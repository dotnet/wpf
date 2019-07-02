// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

namespace Microsoft.Test.RenderingVerification
{
    #region using 
        using System;
        using System.Text;
        using System.Drawing;
        using System.Runtime;
        using System.Security;
        using System.ComponentModel;
        using System.Security.Permissions;
        using System.Runtime.InteropServices;
        using Microsoft.Test.RenderingVerification;
        using Microsoft.Test.RenderingVerification.ResourceFetcher;
    #endregion using 

    /// <summary>
    /// Wraps the User32 dll
    /// </summary>
    [SuppressUnmanagedCodeSecurityAttribute()]
    [BrowsableAttribute(false)]
    internal sealed class User32
    {

        #region Constants

            private const string DLLNAME = "User32.dll";

            /// <summary>
            /// TBM_SETBUDDY message
            /// </summary>
            internal const int TBM_SETBUDDY = 0x432;
            /// <summary>
            /// TBM_GETBUDDY message
            /// </summary>
            internal const int TBM_GETBUDDY = 0x433;
            /// <summary>
            /// SBM_GETSCROLLBARINFO message
            /// </summary>
            internal const int SBM_GETSCROLLBARINFO = 0x00EB;
            /// <summary>
            /// TBM_SETTHUMBRECT message
            /// </summary>
            internal const int TBM_SETTHUMBLENGTH = 0x0427;
            /// <summary>
            /// TBS_FIXEDLENGTH style
            /// </summary>
            internal const int TBS_FIXEDLENGTH = 0x0040;
            /// <summary>
            /// Style windowm, see MSDN
            /// </summary>
            /// <value></value>
            [FlagsAttribute]
            internal enum WindowStyle   // from winuser.h
            {                
                WS_OVERLAPPED           = unchecked((int)0x00000000),
                WS_POPUP                = unchecked((int)0x80000000),
                 WS_CHILD               = unchecked((int)0x40000000),
                 WS_MINIMIZE            = unchecked((int)0x20000000),
                 WS_VISIBLE             = unchecked((int)0x10000000),
                 WS_DISABLED            = unchecked((int)0x08000000),
                 WS_CLIPSIBLINGS        = unchecked((int)0x04000000),
                 WS_CLIPCHILDREN        = unchecked((int) 0x02000000),
                 WS_MAXIMIZE            = unchecked((int) 0x01000000),
                 WS_CAPTION             = unchecked((int) 0x00C00000),     /* WS_BORDER | WS_DLGFRAME  */
                 WS_BORDER              = unchecked((int) 0x00800000),
                 WS_DLGFRAME            = unchecked((int) 0x00400000),
                 WS_VSCROLL             = unchecked((int) 0x00200000),
                 WS_HSCROLL             = unchecked((int) 0x00100000),
                 WS_SYSMENU             = unchecked((int) 0x00080000),
                 WS_THICKFRAME          = unchecked((int) 0x00040000),
                 WS_GROUP               = unchecked((int) 0x00020000),
                 WS_TABSTOP             = unchecked((int) 0x00010000),
                 WS_MINIMIZEBOX         = unchecked((int) 0x00020000),
                 WS_MAXIMIZEBOX         = unchecked((int) 0x00010000),
                 WS_TILED               = WS_OVERLAPPED,
                 WS_ICONIC              = WS_MINIMIZE,
                 WS_SIZEBOX             = WS_THICKFRAME,
                 WS_TILEDWINDOW         = WS_OVERLAPPEDWINDOW,
                 WS_OVERLAPPEDWINDOW    = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX,
                 WS_POPUPWINDOW         = WS_POPUP | WS_BORDER | WS_SYSMENU,
                 WS_CHILDWINDOW         = WS_CHILD
            }

            private const int SW_HIDE = 0;
            private const int SW_SHOWNORMAL = 1;
            private const int SW_NORMAL = 1;
            private const int SW_SHOWMINIMIZED = 2;
            private const int SW_SHOWMAXIMIZED = 3;
            private const int SW_MAXIMIZE = 3;
            private const int SW_SHOWNOACTIVATE = 4;
            private const int SW_SHOW = 5;
            private const int SW_MINIMIZE = 6;
            private const int SW_SHOWMINNOACTIVE = 7;
            private const int SW_SHOWNA = 8;
            private const int SW_RESTORE = 9;
            private const int SW_SHOWDEFAULT = 10;
            private const int SW_FORCEMINIMIZE = 11;
            private const int SW_MAX = 11;

        #endregion Constants

        #region User32 structures & Enum
            /// <summary>
            /// The Win32 POINT counterpart
            /// </summary>
            [StructLayoutAttribute (LayoutKind.Sequential)]
            internal struct POINT 
            {
                public long x;
                public long y; 
            }
             /// <summary>
            /// The Win32 RECT counterpart
            /// </summary>
            [StructLayoutAttribute (LayoutKind.Sequential)]
            public struct RECT
            {
                /// <summary>
                /// Left coordinate
                /// </summary>
                public int Left;
                /// <summary>
                /// Top coordinate
                /// </summary>
                public int Top;
                /// <summary>
                /// Right coordinate
                /// </summary>
                public int Right;
                /// <summary>
                /// Bottom coordinate
                /// </summary>
                public int Bottom;

                /// <summary>
                /// Create a new Rect Struct with the specified params
                /// </summary>
                /// <param name="top">The top coordinate</param>
                /// <param name="left">The left coordinate</param>
                /// <param name="bottom">The bottom coordinate</param>
                /// <param name="right">The right coordinate</param>
                public RECT(int top, int left, int bottom, int right)
                {
                    Top = top;
                    Left = left;
                    Bottom = bottom;
                    Right = right;
                }
            }
            [StructLayoutAttribute (LayoutKind.Sequential)]
            internal struct SCROLLBARINFO 
            {
                internal int cbSize;
                internal RECT rcScrollBar;
                internal int dxyLineButton;
                internal int xyThumbTop;
                internal int xyThumbBottom;
                internal int reserved;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)] internal int[] rgstate; 
            }
            [StructLayoutAttribute (LayoutKind.Sequential)]
            internal struct WINDOWINFO
            {
                internal int cbSize;
                internal RECT rcWindow;
                internal RECT rcClient;
                internal int dwStyle;
                internal int dwExStyle;
                internal int dwWindowStatus;
                internal uint cxWindowBorders;
                internal uint cyWindowBorders;
                internal Int16 atomWindowType;
                internal Int16 wCreatorVersion;
            }
            [StructLayoutAttribute(LayoutKind.Sequential)]
            private struct WINDOWPLACEMENT
            {
                public int length;
                public int flags;
                public int showCmd;
                public Point ptMinPosition;
                public Point ptMaxPosition;
                public Rectangle rcNormalPosition;
            }


/*
            /// <summary>
            /// The Win32 CURSORINFO counterpart
            /// </summary>
            [StructLayoutAttribute (LayoutKind.Sequential)]
            internal struct CURSORINFO 
            {
                public int cbSize;
                public int flags;
                public IntPtr hCursor;
                public POINT ptScreenPos;
            }
            /// <summary>
            /// The Win32 ICONINFO counterpart
            /// </summary>
            [StructLayoutAttribute (LayoutKind.Sequential)]
            internal struct ICONINFO
            {
                public int fIcon;
                public int xHotspot;
                public int yHotspot;
                IntPtr hBitmapMask;
                IntPtr hBitmapColor;
            }
*/
            /// <summary>
            /// The GA_FLAG used by the GetAncestor Method
            /// </summary>
            [FlagsAttribute]
            internal enum GA_FlagEnum
            {
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                GA_PARENT = 0x01,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                GA_ROOT = 0x02,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                GA_ROOTOWNER = 0x03
            }
            internal enum GWL_Index
            {
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                GWL_EXSTYLE = -20  // from winuser.h
            }
            internal enum SetWinPos
            {
                HWND_NOTOPMOST = -2
            }
            internal enum SystemMetricsIndex
            {
                // From winuser.h
                SM_CXSCREEN             = 0,
                SM_CYSCREEN             = 1,
                SM_CXVSCROLL            = 2,
                SM_CYHSCROLL            = 3,
                SM_CYCAPTION            = 4,
                SM_CXBORDER             = 5,
                SM_CYBORDER             = 6,
                SM_CXDLGFRAME           = 7,
                SM_CYDLGFRAME           = 8,
                SM_CYVTHUMB             = 9,
                SM_CXHTHUMB             = 10,
                SM_CXICON               = 11,
                SM_CYICON               = 12,
                SM_CXCURSOR             = 13,
                SM_CYCURSOR             = 14,
                SM_CYMENU               = 15,
                SM_CXFULLSCREEN         = 16,
                SM_CYFULLSCREEN         = 17,
                SM_CYKANJIWINDOW        = 18,
                SM_MOUSEPRESENT         = 19,
                SM_CYVSCROLL            = 20,
                SM_CXHSCROLL            = 21,
                SM_DEBUG                = 22,
                SM_SWAPBUTTON           = 23,
                SM_RESERVED1            = 24,
                SM_RESERVED2            = 25,
                SM_RESERVED3            = 26,
                SM_RESERVED4            = 27,
                SM_CXMIN                = 28,
                SM_CYMIN                = 29,
                SM_CXSIZE               = 30,
                SM_CYSIZE               = 31,
                SM_CXFRAME              = 32,
                SM_CYFRAME              = 33,
                SM_CXMINTRACK           = 34,
                SM_CYMINTRACK           = 35,
                SM_CXDOUBLECLK          = 36,
                SM_CYDOUBLECLK          = 37,
                SM_CXICONSPACING        = 38,
                SM_CYICONSPACING        = 39,
                SM_MENUDROPALIGNMENT    = 40,
                SM_PENWINDOWS           = 41,
                SM_DBCSENABLED          = 42,
                SM_CMOUSEBUTTONS        = 43,
                SM_CXFIXEDFRAME         = SM_CXDLGFRAME,  /* ;win40 name change */
                SM_CYFIXEDFRAME         = SM_CYDLGFRAME,  /* ;win40 name change */
                SM_CXSIZEFRAME          = SM_CXFRAME,     /* ;win40 name change */
                SM_CYSIZEFRAME          = SM_CYFRAME,     /* ;win40 name change */
                SM_SECURE               = 44,
                SM_CXEDGE               = 45,
                SM_CYEDGE               = 46,
                SM_CXMINSPACING         = 47,
                SM_CYMINSPACING         = 48,
                SM_CXSMICON             = 49,
                SM_CYSMICON             = 50,
                SM_CYSMCAPTION          = 51,
                SM_CXSMSIZE             = 52,
                SM_CYSMSIZE             = 53,
                SM_CXMENUSIZE           = 54,
                SM_CYMENUSIZE           = 55,
                SM_ARRANGE              = 56,
                SM_CXMINIMIZED          = 57,
                SM_CYMINIMIZED          = 58,
                SM_CXMAXTRACK           = 59,
                SM_CYMAXTRACK           = 60,
                SM_CXMAXIMIZED          = 61,
                SM_CYMAXIMIZED          = 62,
                SM_NETWORK              = 63,
                SM_CLEANBOOT            = 67,
                SM_CXDRAG               = 68,
                SM_CYDRAG               = 69,
                SM_SHOWSOUNDS           = 70,
                SM_CXMENUCHECK          = 71,   /* Use instead of GetMenuCheckMarkDimensions()! */
                SM_CYMENUCHECK          = 72,
                SM_SLOWMACHINE          = 73,
                SM_MIDEASTENABLED       = 74,
                SM_MOUSEWHEELPRESENT    = 75,
                SM_XVIRTUALSCREEN       = 76,
                SM_YVIRTUALSCREEN       = 77,
                SM_CXVIRTUALSCREEN      = 78,
                SM_CYVIRTUALSCREEN      = 79,
                SM_CMONITORS            = 80,
                SM_SAMEDISPLAYFORMAT    = 81,
                SM_IMMENABLED           = 82,
                SM_CXFOCUSBORDER        = 83,
                SM_CYFOCUSBORDER        = 84,
                SM_TABLETPC             = 86,
                SM_MEDIACENTER          = 87,
                SM_STARTER              = 88,
                SM_REMOTESESSION        = 0x1000,
                SM_SHUTTINGDOWN         = 0x2000,
                SM_REMOTECONTROL        = 0x2001,
                SM_CARETBLINKINGENABLED = 0x2002,
                // value depends on OS (OS < XP, OS == XP, OS > XP )
                SM_CMETRICS_LEGACY      = 76,
                SM_CMETRICS_XP          = 83,
                SM_CMETRICS_ABOVE_XP    = 90,
            }


/*
            /// <summary>
            /// Specify the type of Image to be loaded by the LoadImage API
            /// </summary>
            public enum LoadImageType
            {
                IMAGE_INVALIDTYPE   =  -1,
                IMAGE_BITMAP        =   0,
                IMAGE_ICON          =   1,
                IMAGE_CURSOR        =   2,
                IMAGE_ENHMETAFILE   =   3
            }
            /// <summary>
            /// Specify how the LoadImage API should load the image
            /// </summary>
            [FlagsAttribute]
            public enum LoadImageFlag
            {
                LR_DEFAULTCOLOR     =   0x0000,
                LR_MONOCHROME       =   0x0001,
                LR_COLOR            =   0x0002,
                LR_COPYRETURNORG    =   0x0004,
                LR_COPYDELETEORG    =   0x0008,
                LR_LOADFROMFILE     =   0x0010,
                LR_LOADTRANSPARENT  =   0x0020,
                LR_DEFAULTSIZE      =   0x0040,
                LR_VGACOLOR         =   0x0080,
                LR_LOADMAP3DCOLORS  =   0x1000,
                LR_CREATEDIBSECTION =   0x2000,
                LR_COPYFROMRESOURCE =   0x4000,
                LR_SHARED           =   0x8000
            }
*/
        #endregion User32 structures

        #region Constructors
            private User32() { }    // block instantiation
        #endregion Constructors

        #region DLLImport APIs
            #region public methods
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="caption"></param>
                /// <param name="className"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static public extern IntPtr FindWindow(string className, string caption);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="rect"></param>
                /// <returns></returns>
                static public bool GetClientRect(IntPtr HWND, ref Rectangle rect)
                {
                    return _GetClientRect(HWND, ref rect);
                }
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="pt"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static public extern bool ClientToScreen(IntPtr HWND, ref Point pt);

/*
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hInstance"></param>
                /// <param name="resourceName"></param>
                /// <param name="loadType"></param>
                /// <param name="cxDesired"></param>
                /// <param name="cyDesired"></param>
                /// <param name="loadFlag"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static private extern IntPtr LoadImage(IntPtr hInstance, string resourceName, uint loadType, int cxDesired, int cyDesired, uint loadFlag);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hInstance"></param>
                /// <param name="resourceNumber"></param>
                /// <param name="loadType"></param>
                /// <param name="cxDesired"></param>
                /// <param name="cyDesired"></param>
                /// <param name="loadFlag"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static private extern IntPtr LoadImage(IntPtr hInstance, IntPtr resourceNumber, uint loadType, int cxDesired, int cyDesired, uint loadFlag);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hInstance"></param>
                /// <param name="resourceName"></param>
                /// <param name="loadType"></param>
                /// <param name="cxDesired"></param>
                /// <param name="cyDesired"></param>
                /// <param name="loadFlag"></param>
                /// <returns></returns>
                static internal IntPtr LoadImage(IntPtr hInstance, string resourceName, LoadImageType loadType, int cxDesired, int cyDesired, LoadImageFlag loadFlag)
                {
                    return LoadImage(hInstance, resourceName, (uint)loadType, cxDesired, cyDesired, (uint)loadFlag);
                }
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hInstance"></param>
                /// <param name="resourceNumber"></param>
                /// <param name="loadType"></param>
                /// <param name="cxDesired"></param>
                /// <param name="cyDesired"></param>
                /// <param name="loadFlag"></param>
                /// <returns></returns>
                static internal IntPtr LoadImage(IntPtr hInstance, int resourceNumber, LoadImageType loadType, int cxDesired, int cyDesired, LoadImageFlag loadFlag)
                {
                    return LoadImage(hInstance, new IntPtr(resourceNumber), (uint)loadType, cxDesired, cyDesired, (uint)loadFlag);
                }
                static internal IntPtr LoadImage(IntPtr hInstance, object resourceName, LoadImageType loadType, int cxDesired, int cyDesired, LoadImageFlag loadFlag)
                {
                    if (resourceName is int || resourceName is Int16 || resourceName is Int32 || resourceName is Int64)
                    {
                        return LoadImage(hInstance, (int)resourceName, loadType, cxDesired, cyDesired, loadFlag);
                    }
                    else
                    {
                        if (resourceName is string)
                        {
                            return LoadImage(hInstance, (string)resourceName, (uint)loadType, cxDesired, cyDesired, (uint)loadFlag);
                        }
                        else 
                        {
                            throw new ArgumentException("Param can only be a string of an index (int)", "resourceName");
                        }
                    }
                }
*/
/*
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="cursorInfo"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static internal extern int GetCursorInfo(ref CURSORINFO cursorInfo);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hIcon"></param>
                /// <param name="piconinfo"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static internal extern int GetIconInfo(IntPtr hIcon, ref ICONINFO piconinfo);
*/
                [DllImportAttribute("User32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
                static private extern IntPtr CreateIconFromResource(IntPtr pBits, int size, int bCreateIcon, int Version);
                static internal IntPtr CreateIconFromResource(byte[] bits, bool createCursor)
                {
                    unsafe
                    {
                        fixed (void* useless = bits)
                        {
                            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(bits, 0);
                            return CreateIconFromResource(ptr, bits.Length, ((createCursor) ? Win32StdConst.FALSE : Win32StdConst.TRUE), (int)(0x00030000));
                        }
                    }
                }

                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hCursor"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static internal extern int DestroyCursor(IntPtr hCursor);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hIcon"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static internal extern int DestroyIcon(IntPtr hIcon);

                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="nIndex"></param>
                /// <returns></returns>
                [DllImportAttribute("User32.dll", SetLastError = true)]
                static private extern int GetSystemMetrics(int nIndex);

            #endregion public methods
            #region internal methods
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="Msg"></param>
                /// <param name="wParam"></param>
                /// <param name="lParam"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME)]
                static internal extern IntPtr SendMessage(IntPtr HWND, int Msg, Int32 wParam, IntPtr lParam);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME)]
                static internal extern IntPtr GetDC(IntPtr HWND);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="HDC"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME)]
                static internal extern int ReleaseDC(IntPtr HWND, IntPtr HDC);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="GA_Flag"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern IntPtr GetAncestor(IntPtr HWND, int GA_Flag);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="flag"></param>
                /// <returns></returns>
                static internal IntPtr GetAncestor(IntPtr HWND, GA_FlagEnum flag)
                {
                    return GetAncestor(HWND, (int)flag);
                }
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWNDparent"></param>
                /// <param name="HWNDafterChild"></param>
                /// <param name="className"></param>
                /// <param name="windowName"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME)]
                static internal extern IntPtr FindWindowEx(IntPtr HWNDparent, IntPtr HWNDafterChild, string className, string windowName);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="rect"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern bool GetWindowRect(IntPtr HWND, ref Rectangle rect);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <param name="index"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern long GetWindowLongA(IntPtr HWND, int index);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern bool IsWindow(IntPtr HWND);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="HWND"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern bool IsWindowVisible(IntPtr HWND);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="point"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern IntPtr WindowFromPoint(Point point);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern IntPtr GetDesktopWindow();
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="DeviceMode"></param>
                /// <param name="flag"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern long ChangeDisplaySettings(IntPtr DeviceMode, int flag);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hWnd"></param>
                /// <param name="hWndInsertAfter"></param>
                /// <param name="X"></param>
                /// <param name="Y"></param>
                /// <param name="cx"></param>
                /// <param name="cy"></param>
                /// <param name="Flags"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern int SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, int Flags);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hWnd"></param>
                /// <param name="X"></param>
                /// <param name="Y"></param>
                /// <param name="nWidth"></param>
                /// <param name="nHeight"></param>
                /// <param name="bRepaint"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                /// <param name="hwnd"></param>
                /// <param name="pwi"></param>
                /// <returns></returns>
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static internal extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

            #endregion internal methods
            #region private methods
                [DllImportAttribute(DLLNAME, EntryPoint="GetClientRect", SetLastError=true)]
                static private extern bool _GetClientRect(IntPtr HWND, ref Rectangle rect);
                [DllImport(DLLNAME, SetLastError = true)]
                private static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT windowPlacement);
                [DllImportAttribute(DLLNAME, SetLastError = true)]
                static private extern bool GetCursorPos(ref Point pt);
                [DllImportAttribute(DLLNAME, SetLastError = true, EntryPoint = "SetCursorPos")]
                static private extern bool _SetCursorPos(int x, int y);
                [DllImportAttribute(DLLNAME, SetLastError = true, EntryPoint = "SetProcessDPIAware")]
                static private extern bool _SetProcessDPIAware();
                [DllImport(DLLNAME, EntryPoint = "GetCaretBlinkTime", SetLastError = true)]
                static private extern int _GetCaretBlinkTime();
                [DllImport(DLLNAME, EntryPoint = "SetCaretBlinkTime", SetLastError = true)]
                static private extern bool _SetCaretBlinkTime(int ms);
                [DllImport(DLLNAME, EntryPoint = "ClientToScreen", SetLastError = true)]
                static private extern bool _ClientToScreen(IntPtr hwnd, ref System.Drawing.Point pt);
                //[DllImport(DLLNAME, SetLastError = true)]
                //static private extern bool GetClientRect(IntPtr hwnd, ref Rectangle rect);
             #endregion private methods
        #endregion DLLImport APIs

        #region Helper Methods
            /// <summary>
            /// Returns the screen resolution (in pixels per inches)
            /// </summary>
            /// <returns></returns>
            static internal Size GetScreenResolution()
            {
                Size retVal = Size.Empty;
                IntPtr screenHDC = IntPtr.Zero;
                try
                {
                    screenHDC = User32.GetDC(IntPtr.Zero);
                    if (screenHDC == IntPtr.Zero) { throw new ExternalException("Native call to 'GetDC' failed (error # " + Marshal.GetLastWin32Error() + ")"); }
                    int x = GDI32.GetDeviceCaps(screenHDC, (int)GDI32.GetDeviceCapsIndex.LOGPIXELSX);
                    if (x == 0) { throw new ExternalException("Native call to 'GetDeviceCaps(x)' failed (error # " + Marshal.GetLastWin32Error() + ")"); }
                    int y = GDI32.GetDeviceCaps(screenHDC, (int)GDI32.GetDeviceCapsIndex.LOGPIXELSY);
                    if (y == 0) { throw new ExternalException("Native call to 'GetDeviceCaps(y)' failed (error # " + Marshal.GetLastWin32Error() + ")"); }
                    retVal = new Size(x, y);
                }
                finally
                {
                    if (screenHDC != IntPtr.Zero) { User32.ReleaseDC(IntPtr.Zero, screenHDC); screenHDC = IntPtr.Zero; }
                }
                return retVal;
            }
            /// <summary>
            /// Returns the requested system metric
            /// </summary>
            /// <param name="systemMetricsIndex"></param>
            /// <returns></returns>
            static internal int GetSystemMetrics(SystemMetricsIndex systemMetricsIndex)
            {
                return GetSystemMetrics((int)systemMetricsIndex);
            }
            static internal Point GetClientOffset(IntPtr hwnd)
            {
                WINDOWINFO windowInfo = new WINDOWINFO();
                windowInfo.cbSize = Marshal.SizeOf(typeof(WINDOWINFO));
                GetWindowInfo(hwnd, ref windowInfo);
                return new Point(windowInfo.rcClient.Left, windowInfo.rcClient.Top);
            }
            static internal bool IsWindowMinimized(IntPtr hwnd)
            {
                // Assuming hwnd is a valid handle to a visible window.
                WINDOWPLACEMENT wp = new WINDOWPLACEMENT();
                bool result = GetWindowPlacement(hwnd, ref wp);
                if (!result) { throw new System.Runtime.InteropServices.ExternalException("Call to native API 'GetWindowPlacement' failed"); }
                return (wp.showCmd == SW_MINIMIZE || wp.showCmd == SW_HIDE || wp.showCmd == SW_SHOWMINIMIZED);
            }
            static internal Point GetCursorPos()
            {
                Point retVal = new Point();
                bool success = GetCursorPos(ref retVal);
                if (!success)
                {
                    throw new ExternalException("Call to Win32 Api 'GetCursorPos' failed (error #" + System.Runtime.InteropServices.Marshal.GetLastWin32Error() + ")");
                }
                return retVal;
            }
            static internal void SetCursorPos(int x, int y)
            {
                bool success = _SetCursorPos(x, y);
                if (!success)
                {
                    throw new ExternalException("Call to Win32 Api 'GetCursorPos' failed (error #" + System.Runtime.InteropServices.Marshal.GetLastWin32Error() + ")");
                }

            }
            static internal void SetProcessDPIAware()
            {
                // Do only if OS >= vista
                if (System.Environment.OSVersion.Version.Major > 5)
                {
                    if (!_SetProcessDPIAware())
                    {
                        throw new ExternalException("Call to Native API 'SetProcessDPIAware' failed");
                    }
                }
            }
            static internal int GetCaretBlinkTime()
            {
                int retVal = _GetCaretBlinkTime();
                if (retVal == 0) { throw new ExternalException("Native call to API 'GetCaretBlinkTime' failed"); }
                return retVal;
            }
            static internal void SetCaretBlinkTime(int ms)
            {
                bool success = _SetCaretBlinkTime(ms);
                if (!success) { throw new ExternalException("Native call to API 'SetCaretBlinkTime' failed"); }
            }
/*
            static internal void ClientToScreen(IntPtr hwnd, ref System.Drawing.Point point)
            {
                bool success = _ClientToScreen(hwnd, ref point);
                if (!success) { throw new ExternalException("Native call to API 'ClientToScreen' failed"); }
            }
*/
            static internal System.Drawing.Rectangle GetClientRect(IntPtr hwnd)
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle();
                bool success = GetClientRect(hwnd, ref rect);
                if (!success) { throw new ExternalException("Native call to API 'GetClientRect' failed"); }
                return rect;
            }
        #endregion Helper Methods
    }

    /// <summary>
    /// Standard Win32 definition and constants
    /// </summary>
    [BrowsableAttribute(false)]
    internal sealed class Win32StdConst
    {
        #region constants
            /// <summary>
            /// The standard Win32 definition of MAX_PATH
            /// </summary>
            internal const int MAX_PATH = 260;
            /// <summary>
            /// The C definition of FALSE (as BOOL)
            /// </summary>
            internal const int FALSE = 0;
            /// <summary>
            /// The C definition of TRUE (as BOOL)
            /// </summary>
            internal const int TRUE = 1;
        #endregion constants

        #region Constructors
            private Win32StdConst() { }    // block instantiation
        #endregion Constructors
    }

    /// <summary>
    /// Wraps the GDI32.dll
    /// </summary>
    [SuppressUnmanagedCodeSecurityAttribute()]
    [BrowsableAttribute(false)]
    internal sealed class GDI32
    {
        #region Enum, consts and structs (from wingdi.h)
            private const string DLLNAME = "Gdi32.dll";
            internal const int CBM_INIT         =   0x04;  /* initialize bitmap */
            internal const int DIB_RGB_COLORS   =   0;      /* color table in RGBs */

            /// <summary>
            /// The RasterOperation used by BitBlt and StretchBlt
            /// </summary>
            [FlagsAttribute]
            internal enum RasterOperationCodeEnum
            {
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                SRCCOPY         = 0x00CC0020,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                SRCPAINT        = 0x00EE0086,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                SRCAND          = 0x008800C6,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                SRCINVERT = 0x00660046,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                SRCERASE = 0x00440328,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                NOTSRCCOPY = 0x00330008,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                NOTSRCERASE = 0x001100A6,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                MERGECOPY = 0x00C000CA,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                MERGEPAINT = 0x00BB0226,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                PATCOPY = 0x00F00021,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                PATPAINT = 0x00FB0A09,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                PATINVERT = 0x005A0049,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                DSTINVERT = 0x00550009,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                BLACKNESS = 0x00000042,
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                WHITENESS = 0x00FF0062,
/*
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                NOMIRRORBITMAP  = (int)0x80000000,
*/
                /// <summary>
                /// See MSDN Documentation
                /// </summary>
                CAPTUREBLT = 0x40000000
            }
            /// <summary>
            /// The Index used by GetDeviceCaps API
            /// </summary>
            internal enum GetDeviceCapsIndex
            {
                DRIVERVERSION = 0,     /* Device driver version                    */
                TECHNOLOGY    = 2,     /* Device classification                    */
                HORZSIZE      = 4,     /* Horizontal size in millimeters           */
                VERTSIZE      = 6,     /* Vertical size in millimeters             */
                HORZRES       = 8,     /* Horizontal width in pixels               */
                VERTRES       = 10,    /* Vertical height in pixels                */
                BITSPIXEL     = 12,    /* Number of bits per pixel                 */
                PLANES        = 14,    /* Number of planes                         */
                NUMBRUSHES    = 16,    /* Number of brushes the device has         */
                NUMPENS       = 18,    /* Number of pens the device has            */
                NUMMARKERS    = 20,    /* Number of markers the device has         */
                NUMFONTS      = 22,    /* Number of fonts the device has           */
                NUMCOLORS     = 24,    /* Number of colors the device supports     */
                PDEVICESIZE   = 26,    /* Size required for device descriptor      */
                CURVECAPS     = 28,    /* Curve capabilities                       */
                LINECAPS      = 30,    /* Line capabilities                        */
                POLYGONALCAPS = 32,    /* Polygonal capabilities                   */
                TEXTCAPS      = 34,    /* Text capabilities                        */
                CLIPCAPS      = 36,    /* Clipping capabilities                    */
                RASTERCAPS    = 38,    /* Bitblt capabilities                      */
                ASPECTX       = 40,    /* Length of the X leg                      */
                ASPECTY       = 42,    /* Length of the Y leg                      */
                ASPECTXY      = 44,    /* Length of the hypotenuse                 */

                LOGPIXELSX    = 88,    /* Logical pixels/inch in X                 */
                LOGPIXELSY    = 90,    /* Logical pixels/inch in Y                 */

                SIZEPALETTE  = 104,    /* Number of entries in physical palette    */
                NUMRESERVED  = 106,    /* Number of reserved entries in palette    */
                COLORRES     = 108,    /* Actual color resolution                  */

                // Printing related DeviceCaps. These replace the appropriate Escapes
                PHYSICALWIDTH   = 110, /* Physical Width in device units           */
                PHYSICALHEIGHT  = 111, /* Physical Height in device units          */
                PHYSICALOFFSETX = 112, /* Physical Printable Area x margin         */
                PHYSICALOFFSETY = 113, /* Physical Printable Area y margin         */
                SCALINGFACTORX  = 114, /* Scaling factor x                         */
                SCALINGFACTORY  = 115, /* Scaling factor y                         */

                // Display driver specific
                VREFRESH        = 116,  /* Current vertical refresh rate of the display device (for displays only) in Hz */
                DESKTOPVERTRES  = 117,  /* Horizontal width of entire desktop in pixels */
                DESKTOPHORZRES  = 118,  /* Vertical height of entire desktop in pixels */
                BLTALIGNMENT    = 119,  /* Preferred blt alignment                 */

                SHADEBLENDCAPS  = 120,  /* Shading and blending caps               */
                COLORMGMTCAPS   = 121,  /* Color Management caps                   */

                /* Device Technologies */
                DT_PLOTTER          = 0,   /* Vector plotter                   */
                DT_RASDISPLAY       = 1,   /* Raster display                   */
                DT_RASPRINTER       = 2,   /* Raster printer                   */
                DT_RASCAMERA        = 3,   /* Raster camera                    */
                DT_CHARSTREAM       = 4,   /* Character-stream, PLP            */
                DT_METAFILE         = 5,   /* Metafile, VDM                    */
                DT_DISPFILE         = 6,   /* Display-file                     */

                /* Curve Capabilities */
                CC_NONE             = 0,   /* Curves not supported             */
                CC_CIRCLES          = 1,   /* Can do circles                   */
                CC_PIE              = 2,   /* Can do pie wedges                */
                CC_CHORD            = 4,   /* Can do chord arcs                */
                CC_ELLIPSES         = 8,   /* Can do ellipese                  */
                CC_WIDE             = 16,  /* Can do wide lines                */
                CC_STYLED           = 32,  /* Can do styled lines              */
                CC_WIDESTYLED       = 64,  /* Can do wide styled lines         */
                CC_INTERIORS        = 128, /* Can do interiors                 */
                CC_ROUNDRECT        = 256, /*                                  */

                /* Line Capabilities */
                LC_NONE             = 0,   /* Lines not supported              */
                LC_POLYLINE         = 2,   /* Can do polylines                 */
                LC_MARKER           = 4,   /* Can do markers                   */
                LC_POLYMARKER       = 8,   /* Can do polymarkers               */
                LC_WIDE             = 16,  /* Can do wide lines                */
                LC_STYLED           = 32,  /* Can do styled lines              */
                LC_WIDESTYLED       = 64,  /* Can do wide styled lines         */
                LC_INTERIORS        = 128, /* Can do interiors                 */

                /* Polygonal Capabilities */
                PC_NONE             = 0,   /* Polygonals not supported         */
                PC_POLYGON          = 1,   /* Can do polygons                  */
                PC_RECTANGLE        = 2,   /* Can do rectangles                */
                PC_WINDPOLYGON      = 4,   /* Can do winding polygons          */
                PC_TRAPEZOID        = 4,   /* Can do trapezoids                */
                PC_SCANLINE         = 8,   /* Can do scanlines                 */
                PC_WIDE             = 16,  /* Can do wide borders              */
                PC_STYLED           = 32,  /* Can do styled borders            */
                PC_WIDESTYLED       = 64,  /* Can do wide styled borders       */
                PC_INTERIORS        = 128, /* Can do interiors                 */
                PC_POLYPOLYGON      = 256, /* Can do polypolygons              */
                PC_PATHS            = 512, /* Can do paths                     */

                /* Clipping Capabilities */
                CP_NONE             = 0,   /* No clipping of output            */
                CP_RECTANGLE        = 1,   /* Output clipped to rects          */
                CP_REGION           = 2,   /* obsolete                         */

                /* Text Capabilities */
                TC_OP_CHARACTER     = 0x00000001,  /* Can do OutputPrecision   CHARACTER      */
                TC_OP_STROKE        = 0x00000002,  /* Can do OutputPrecision   STROKE         */
                TC_CP_STROKE        = 0x00000004,  /* Can do ClipPrecision     STROKE         */
                TC_CR_90            = 0x00000008,  /* Can do CharRotAbility    90             */
                TC_CR_ANY           = 0x00000010,  /* Can do CharRotAbility    ANY            */
                TC_SF_X_YINDEP      = 0x00000020,  /* Can do ScaleFreedom      X_YINDEPENDENT */
                TC_SA_DOUBLE        = 0x00000040,  /* Can do ScaleAbility      DOUBLE         */
                TC_SA_INTEGER       = 0x00000080,  /* Can do ScaleAbility      INTEGER        */
                TC_SA_CONTIN        = 0x00000100,  /* Can do ScaleAbility      CONTINUOUS     */
                TC_EA_DOUBLE        = 0x00000200,  /* Can do EmboldenAbility   DOUBLE         */
                TC_IA_ABLE          = 0x00000400,  /* Can do ItalisizeAbility  ABLE           */
                TC_UA_ABLE          = 0x00000800,  /* Can do UnderlineAbility  ABLE           */
                TC_SO_ABLE          = 0x00001000,  /* Can do StrikeOutAbility  ABLE           */
                TC_RA_ABLE          = 0x00002000,  /* Can do RasterFontAble    ABLE           */
                TC_VA_ABLE          = 0x00004000,  /* Can do VectorFontAble    ABLE           */
                TC_RESERVED         = 0x00008000,
                TC_SCROLLBLT        = 0x00010000,  /* Don't do text scroll with blt           */

                /* Raster Capabilities */
                RC_NONE             = 0,
                RC_BITBLT           = 1,       /* Can do standard BLT.             */
                RC_BANDING          = 2,       /* Device requires banding support  */
                RC_SCALING          = 4,       /* Device requires scaling support  */
                RC_BITMAP64         = 8,       /* Device can support >64K bitmap   */
                RC_GDI20_OUTPUT     = 0x0010,      /* has 2.0 output calls         */
                RC_GDI20_STATE      = 0x0020,
                RC_SAVEBITMAP       = 0x0040,
                RC_DI_BITMAP        = 0x0080,      /* supports DIB to memory       */
                RC_PALETTE          = 0x0100,      /* supports a palette           */
                RC_DIBTODEV         = 0x0200,      /* supports DIBitsToDevice      */
                RC_BIGFONT          = 0x0400,      /* supports >64K fonts          */
                RC_STRETCHBLT       = 0x0800,      /* supports StretchBlt          */
                RC_FLOODFILL        = 0x1000,      /* supports FloodFill           */
                RC_STRETCHDIB       = 0x2000,      /* supports StretchDIBits       */
                RC_OP_DX_OUTPUT     = 0x4000,
                RC_DEVBITS          = 0x8000,

                /* Shading and blending caps */
                SB_NONE             = 0x00000000,
                SB_CONST_ALPHA      = 0x00000001,
                SB_PIXEL_ALPHA      = 0x00000002,
                SB_PREMULT_ALPHA    = 0x00000004,
                SB_GRAD_RECT       = 0x00000010,
                SB_GRAD_TRI         = 0x00000020,

                /* Color Management caps */
                CM_NONE             = 0x00000000,
                CM_DEVICE_ICM       = 0x00000001,
                CM_GAMMA_RAMP       = 0x00000002,
                CM_CMYK_COLOR       = 0x00000004,
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct BITMAPINFOHEADER
            {
                public int biSize;
                public int biWidth;
                public int biHeight;
                public short biPlanes;
                public short biBitCount;
                public int biCompression;
                public int biSizeImage;
                public int biXPelsPerMeter;
                public int biYPelsPerMeter;
                public int biClrUsed;
                public int biClrImportant;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct LOGPALETTE 
            {
                public short palVersion;
                public short palNumEntries;
                public PALETTEENTRY palPalEntry;
            }
            [StructLayout(LayoutKind.Sequential)]
            internal struct PALETTEENTRY
            {
                public byte peRed;
                public byte peGreen;
                public byte peBlue;
                public byte peFlags;
            }
 
        #endregion Enum, consts and structs (from wingdi.h)

        #region Constructors
            private GDI32() { }    // block instantiation
        #endregion Constructors

        #region DLLImport APIs
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="hdcDest"></param>
            /// <param name="nXDest"></param>
            /// <param name="nYDest"></param>
            /// <param name="nWidth"></param>
            /// <param name="nHeight"></param>
            /// <param name="hdcSrc"></param>
            /// <param name="nXSrc"></param>
            /// <param name="nYSrc"></param>
            /// <param name="RasterOpCode"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, Int32 RasterOpCode);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="hdcSrc"></param>
            /// <param name="width"></param>
            /// <param name="height"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr CreateCompatibleBitmap(IntPtr hdcSrc, int width, int height);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDCSource"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr CreateCompatibleDC(IntPtr HDCSource);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="driverName"></param>
            /// <param name="deviceName"></param>
            /// <param name="reserved"></param>
            /// <param name="initData"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr CreateDC(string driverName, string deviceName, string reserved, IntPtr initData);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern bool DeleteDC(IntPtr HDC);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="hGdiObject"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern bool DeleteObject(IntPtr hGdiObject);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="hBMP"></param>
            /// <param name="bufferSize"></param>
            /// <param name="buffer"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern long GetBitmapBits(IntPtr hBMP, long bufferSize, ref byte[] buffer);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <param name="flagIndex"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int GetDeviceCaps(IntPtr HDC, int flagIndex);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int GetPixel(IntPtr HDC, int x, int y);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <param name="hgdiobj"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr SelectObject(IntPtr HDC, IntPtr hgdiobj);
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="color"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int SetPixel(IntPtr HDC, int x, int y, int color);

            [DllImportAttribute(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
            static internal extern IntPtr CreateBitmapIndirect(IntPtr lpbm);
            [DllImportAttribute(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
            static internal extern IntPtr CreateDIBitmap(IntPtr HDC, IntPtr bitmapInfoHeader, int fdwInit, IntPtr bits, IntPtr bitmapInfo, int usage);

            [DllImportAttribute(DLLNAME, CharSet = CharSet.Unicode, SetLastError = true)]
            static internal extern IntPtr CreatePalette(IntPtr logPalettePtr);

        #endregion DLLImport APIs

        #region Win32 API wrapper (helper)
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="hdcDest"></param>
            /// <param name="nXDest"></param>
            /// <param name="nYDest"></param>
            /// <param name="nWidth"></param>
            /// <param name="nHeight"></param>
            /// <param name="hdcSrc"></param>
            /// <param name="nXSrc"></param>
            /// <param name="nYSrc"></param>
            /// <param name="RasterOpCode"></param>
            /// <returns></returns>
            static internal bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, RasterOperationCodeEnum RasterOpCode)
            {
                return BitBlt(hdcDest, nXDest, nYDest, nWidth, nHeight, hdcSrc, nXSrc, nYSrc, (Int32)RasterOpCode);
            }
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            static internal Color GetPixelColor(IntPtr HDC, int x, int y)
            {
                return Color.FromArgb(GetPixel(HDC, x, y));
            }
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="HDC"></param>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <param name="color"></param>
            /// <returns></returns>
            static internal Color SetPixelColor(IntPtr HDC, int x, int y, Color color)
            {
                return Color.FromArgb(SetPixel(HDC, x, y, color.ToArgb()));
            }
        #endregion
    }

    /// <summary>
    /// Wraps the Kernel32 dll
    /// </summary>
    [SuppressUnmanagedCodeSecurityAttribute()]
    [BrowsableAttribute(false)]
    internal sealed class Kernel32
    {
        #region Constants

            private const string DLLNAME = "Kernel32.dll";

            /// <summary>
            /// LOCAL_USER_DEFAULT value ( natively in "winnt.h" )
            /// </summary>
            internal const int LOCALE_USER_DEFAULT = 0x00;  // LCID  --  SORT_DEFAULT = 0x00 & LANG_USER_DEFAULT = 0x00 (because LANG_NEUTRAL = 0x00 & SUBLANG_NEUTRAL = 0x00)
            /// <summary>
            /// LOCAL_SYSTEM_DEFAULT value ( natively in "winnt.h" )
            /// BUGBUG : CHECK THE VALUE
            /// </summary>
            internal const int LOCALE_SYSTEM_DEFAULT = 0x0020;  // LCID  --  SORT_DEFAULT = 0x00 & LANG_SYSTEM_DEFAULT = 0x02 (because LANG_NEUTRAL = 0x00 & SUBLANG_SYS_NEUTRAL = 0x02)
            /// <summary>
            /// LOCALE_IDEFAULTANSICODEPAGE value ( natively in "winnls.h" )
            /// </summary>
            internal const int LOCALE_IDEFAULTANSICODEPAGE = 0x00001004;    // LCTYPE  --  ANSI Code Page
            /// <summary>
            /// FILE_MAP_WRITE value
            /// </summary>
            internal const int FILE_MAP_WRITE = 0x0002; // from winbase.h & winnt.h
            /// <summary>
            /// FILE_MAP_READ value
            /// </summary>
            internal const int FILE_MAP_READ = 0x0004;  // from winbase.h & winnt.h
            /// <summary>
            /// The value for the Code page ANSI latin (english, french, german, spanish, ...)
            /// </summary>
            internal const int CODEPAGE_ANSI_LATIN = 1252;
        #endregion Constants

        #region Constructors
            private Kernel32() { }    // block instantiation
        #endregion Constructors

        #region Callback delegates
            internal delegate int EnumResourceTypeProc(IntPtr hModule, IntPtr type, IntPtr lParam);
            internal delegate int EnumResourceNamesProc(IntPtr hModule, IntPtr type, IntPtr name, IntPtr lParam);
            internal delegate int EnumResourceLanguagesProc(IntPtr hModule, IntPtr type, IntPtr name, Int16 idDLanguage, IntPtr lParam);
        #endregion Callback delegates

        #region DllImport APIs
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern Int32 GetLastError();
            /// <summary>
            /// See MSDN Documentation
            /// </summary>
            /// <param name="LCID"></param>
            /// <param name="LCTYPE"></param>
            /// <param name="info"></param>
            /// <param name="infoSize"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int GetLocaleInfo(int LCID, int LCTYPE, StringBuilder info, int infoSize);
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr LoadLibrary(string fileName);
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int FreeLibrary(IntPtr hModule);
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern IntPtr MapViewOfFile(IntPtr hFileMappingObject, int dwDesiredAccess, int dwFileOffsetHigh, int dwFileOffsetLow, long dwNumberOfBytesToMap);
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int UnmapViewOfFile(IntPtr lpBaseAddress);

            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern int EnumResourceTypes(IntPtr hModule, [MarshalAs(UnmanagedType.FunctionPtr)]EnumResourceTypeProc lpEnumFunc,IntPtr lParam);
            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern int EnumResourceNames(IntPtr hModule, IntPtr resourceType, [MarshalAs(UnmanagedType.FunctionPtr)]EnumResourceNamesProc EnumFunc, IntPtr lParam);
            static internal int EnumResourceNames(IntPtr hModule, ResourceType resourceType, EnumResourceNamesProc EnumFunc, IntPtr lParam)
            {
                return EnumResourceNames(hModule, new IntPtr((int)resourceType), EnumFunc, lParam);
            }
            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern int EnumResourceLanguages(IntPtr hModule, IntPtr type, IntPtr name, [MarshalAs(UnmanagedType.FunctionPtr)]EnumResourceLanguagesProc EnumFunc, IntPtr lParam);

            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern IntPtr FindResourceEx(IntPtr hModule, IntPtr type, IntPtr name, Int16 wLanguage);
            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern int SizeofResource(IntPtr hModule, IntPtr hResInfo);
            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);
            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern IntPtr LockResource(IntPtr hGlobal);
            [DllImportAttribute(DLLNAME, CharSet=CharSet.Unicode, SetLastError = true)]
            static internal extern int FreeResource(IntPtr hLoadResourceHandle);

        #endregion DllImport APIs
    }

    /// <summary>
    /// Wraps some of the uxTheme.dll APIs
    /// </summary>
    [SuppressUnmanagedCodeSecurityAttribute()]
    [BrowsableAttribute(false)]
    public sealed class UxTheme
    {
        #region Constants
            private const string DLLNAME = "UxTheme.dll";
        #endregion

        #region Constructors
        private UxTheme() { }    // block instantiation
        #endregion Constructors

        #region DLLImport APIs
            /// <summary>
            /// See MSDN Documentation
            /// Note : Will throw a DLLNotFoundException on OS below XP
            /// </summary>
            /// <param name="themeFileName"></param>
            /// <param name="maxThemeFileNameLenght"></param>
            /// <param name="colorSchemeName"></param>
            /// <param name="maxColorSchemeNameLength"></param>
            /// <param name="sizeName"></param>
            /// <param name="maxSizeNameLength"></param>
            /// <returns></returns>
            [DllImportAttribute(DLLNAME, SetLastError = true)]
            static internal extern int GetCurrentThemeName([MarshalAs(UnmanagedType.LPWStr)]StringBuilder themeFileName, int maxThemeFileNameLenght, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder colorSchemeName, int maxColorSchemeNameLength, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder sizeName, int maxSizeNameLength);
        #endregion DLLImport APIs

        #region Win32 API wrapper (helper)
            /// <summary>
            /// Wrapper to Retrieve the theme name without calling directly into the Win32 API
            /// NOTE : Calling this API on a OS below WinXP will always return an empty string since this is not defined before then.
            /// </summary>
            /// <returns>The name of the file name containing the theme, an empty string if no theme</returns>
            [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
            static public string GetCurrentThemeName()
            {
                int maxPath = Win32StdConst.MAX_PATH;
                StringBuilder themeFileName = new StringBuilder(maxPath);
                StringBuilder colorSchemeName = new StringBuilder(maxPath);
                StringBuilder sizeName = new StringBuilder(maxPath);
                int errorNum = 0;
                try
                {
                    errorNum = UxTheme.GetCurrentThemeName(themeFileName, maxPath, colorSchemeName, maxPath, sizeName, maxPath);
                }
                catch (DllNotFoundException)
                {
                    // OS < xp
                    return string.Empty;
                }
                if (errorNum == unchecked((int)0x80070490)) // 0x80070490 = > "Element not found"
                {
                    // Theming is turned off (windows classic)
                    return string.Empty;
                }
                if (errorNum != 0)
                {
                    throw new ExternalException("Call to Win32 API 'GetCurrentThemeName' failed, error #" + errorNum.ToString("h"));
                }
                return System.IO.Path.GetFileNameWithoutExtension(themeFileName.ToString());
            }
        #endregion Win32 API wrapper (helper)
    }

    /// <summary>
    /// Wraps some COM macro and value
    /// </summary>
    [SuppressUnmanagedCodeSecurityAttribute()]
    [BrowsableAttribute(false)]
    internal sealed class COM
    {
        #region Constants
            public const int S_OK = 0;
            public const int S_FALSE = 1;
        #endregion Constants

        #region Constructors
            private COM() { } // block instantiation
        #endregion Constructors

        #region Methods
            public static bool Succeeded(int hresult)
            {
                return (hresult == S_OK || hresult == S_FALSE) ? true : false;
            }
            public static bool Failed(int hresult)
            {
                return ! Succeeded(hresult);
            }
        #endregion Methods

    }
}
