// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

using System.Runtime.InteropServices;
using System;
using System.Security.Permissions;
using System.Collections;
using System.IO;
using System.Text;
using System.Security;
using System.Threading;

using Microsoft.Win32;

namespace Microsoft.Test.Win32
{

    /// <summary>
    /// Contains Native Structs
    /// </summary>
    [SuppressUnmanagedCodeSecurity()]
    public static class NativeStructs
    {
        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct DEVMODE
        {
            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeConstants.CCHDEVICENAME)]
            public string dmDeviceName;

            /// <summary>
            ///
            /// </summary>
            public short dmSpecVersion;
            /// <summary>
            ///
            /// </summary>
            public short dmDriverVersion;
            /// <summary>
            ///
            /// </summary>
            public short dmSize;
            /// <summary>
            ///
            /// </summary>
            public short dmDriverExtra;
            /// <summary>
            ///
            /// </summary>
            public int dmFields;
            /// <summary>
            ///
            /// </summary>
            public short dmOrientation;
            /// <summary>
            ///
            /// </summary>
            public short dmPaperSize;
            /// <summary>
            ///
            /// </summary>
            public short dmPaperLength;
            /// <summary>
            ///
            /// </summary>
            public short dmPaperWidth;
            /// <summary>
            ///
            /// </summary>
            public short dmScale;
            /// <summary>
            ///
            /// </summary>
            public short dmCopies;
            /// <summary>
            ///
            /// </summary>
            public short dmDefaultSource;
            /// <summary>
            ///
            /// </summary>
            public short dmPrintQuality;
            /// <summary>
            ///
            /// </summary>
            public short dmColor;
            /// <summary>
            ///
            /// </summary>
            public short dmDuplex;
            /// <summary>
            ///
            /// </summary>
            public short dmYResolution;
            /// <summary>
            ///
            /// </summary>
            public short dmTTOption;
            /// <summary>
            ///
            /// </summary>
            public short dmCollate;
            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeConstants.CCHDEVICENAME)]
            public string dmFormName;
            /// <summary>
            ///
            /// </summary>
            public short dmLogPixels;
            /// <summary>
            ///
            /// </summary>
            public int dmBitsPerPel;
            /// <summary>
            ///
            /// </summary>
            public int dmPelsWidth;
            /// <summary>
            ///
            /// </summary>
            public int dmPelsHeight;
            /// <summary>
            ///
            /// </summary>
            public int dmDisplayFlags;
            /// <summary>
            ///
            /// </summary>
            public int dmDisplayFrequency;
            /// <summary>
            ///
            /// </summary>
            public int dmICMMethod;
            /// <summary>
            ///
            /// </summary>
            public int dmICMIntent;
            /// <summary>
            ///
            /// </summary>
            public int dmMediaType;
            /// <summary>
            ///
            /// </summary>
            public int dmDitherType;
            /// <summary>
            ///
            /// </summary>
            public int dmReserved1;
            /// <summary>
            ///
            /// </summary>
            public int dmReserved2;
            /// <summary>
            ///
            /// </summary>
            public int dmPanningWidth;
            /// <summary>
            ///
            /// </summary>
            public int dmPanningHeight;
        }

        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0)]
        public struct DISPLAY_DEVICE
        {
            /// <summary>
            ///
            /// </summary>
            public int cb;

            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            /// <summary>
            ///
            /// </summary>
            public int StateFlags;
            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            /// <summary>
            ///
            /// </summary>
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)] // For GetMouseMovePointsEx
        public struct MOUSEMOVEPOINT
        {

            /// <summary>
            /// </summary>
            public int x;                       //Specifies the x-coordinate of the mouse

            /// <summary>
            /// </summary>
            public int y;                       //Specifies the x-coordinate of the mouse

            /// <summary>
            /// </summary>
            public int time;                    //Specifies the time stamp of the mouse coordinate

            /// <summary>
            /// </summary>
            public IntPtr dwExtraInfo;              //Specifies extra information associated with this coordinate.
        }


        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class ACCEL
        {
            /// <summary>
            /// </summary>
            public ACCEL() { }

            /// <summary>
            /// </summary>
            public ACCEL(ACCEL accel)
            {
                this.fVirt = accel.fVirt;
                this.key = accel.key;
                this.cmd = accel.cmd;
            }

            /// <summary>
            /// </summary>
            public byte fVirt = 0;
            /// <summary>
            /// </summary>
            public short key = 0;
            /// <summary>
            /// </summary>
            public short cmd = 0;
        }


        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public class COMRECT
        {
            /// <summary>
            /// </summary>
            public int left;

            /// <summary>
            /// </summary>
            public int top;

            /// <summary>
            /// </summary>
            public int right;

            /// <summary>
            /// </summary>
            public int bottom;

            /// <summary>
            /// </summary>
            public COMRECT()
            {
            }

            /// <summary>
            /// </summary>
            public COMRECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            /// <summary>
            /// </summary>
            public static COMRECT FromXYWH(int x, int y, int width, int height)
            {
                return new COMRECT(x, y, x + width, y + height);
            }

            /// <summary>
            /// </summary>
            public override string ToString()
            {
                return "Left = " + left + " Top " + top + " Right = " + right + " Bottom = " + bottom;
            }
        }


        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFOEX
        {
            /// <summary>
            /// </summary>
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));

            /// <summary>
            /// </summary>
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>
            public int dwFlags = 0;

            /// <summary>
            /// </summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public char[] szDevice = new char[32];
        }


        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            /// <summary>
            /// </summary>
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>
            public int dwFlags = 0;
        }


        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MENUITEMINFO_T
        {
            /// <summary>
            /// </summary>
            public int cbSize = Marshal.SizeOf(typeof(MENUITEMINFO_T));
            /// <summary>
            /// </summary>
            public int fMask = 0;
            /// <summary>
            /// </summary>
            public int fType = 0;
            /// <summary>
            /// </summary>
            public int fState = 0;
            /// <summary>
            /// </summary>
            public int wID = 0;
            /// <summary>
            /// </summary>
            public IntPtr hSubMenu = IntPtr.Zero;
            /// <summary>
            /// </summary>
            public IntPtr hbmpChecked = IntPtr.Zero;
            /// <summary>
            /// </summary>
            public IntPtr hbmpUnchecked = IntPtr.Zero;
            /// <summary>
            /// </summary>
            public int dwItemData = 0;
            /// <summary>
            /// </summary>
            public string dwTypeData = null;
            /// <summary>
            /// </summary>
            public int cch = 0;
        }

        /// <summary>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class WNDCLASSEX_D
        {


            /// <summary>
            /// </summary>
            public int cbSize = 0;

            /// <summary>
            /// </summary>
            public int style = 0;

            /// <summary>
            /// </summary>
            public NativeStructs.WndProc lpfnWndProc = null;

            /// <summary>
            /// </summary>
            public int cbClsExtra = 0;

            /// <summary>
            /// </summary>
            public int cbWndExtra = 0;

            /// <summary>
            /// </summary>
            public IntPtr hInstance = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hIcon = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hCursor = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hbrBackground = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public string lpszMenuName = null;

            /// <summary>
            /// </summary>
            public string lpszClassName = null;

            /// <summary>
            /// </summary>
            public IntPtr hIconSm = IntPtr.Zero;
        }

        /// <summary>
        /// WndProc
        /// </summary>
        public delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class WNDCLASS
        {


            /// <summary>
            /// </summary>
            public int style = 0;

            /// <summary>
            /// </summary>
            public IntPtr lpfnWndProc = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public int cbClsExtra = 0;

            /// <summary>
            /// </summary>
            public int cbWndExtra = 0;

            /// <summary>
            /// </summary>
            public IntPtr hInstance = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hIcon = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hCursor = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hbrBackground = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public string lpszMenuName = null;

            /// <summary>
            /// </summary>
            public string lpszClassName = null;
        }

        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class WNDCLASS_I
        {

            /// <summary>
            /// </summary>
            public int style = 0;

            /// <summary>
            /// </summary>
            public IntPtr lpfnWndProc = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public int cbClsExtra = 0;

            /// <summary>
            /// </summary>
            public int cbWndExtra = 0;

            /// <summary>
            /// </summary>
            public IntPtr hInstance = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hIcon = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hCursor = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hbrBackground = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr lpszMenuName = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr lpszClassName = IntPtr.Zero;
        }


        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class WNDCLASSEX_I
        {
            /// <summary>
            /// </summary>
            public int cbSize = 0;

            /// <summary>
            /// </summary>
            public int style = 0;

            /// <summary>
            /// </summary>
            public IntPtr lpfnWndProc = IntPtr.Zero;
            /// <summary>
            /// </summary>
            public int cbClsExtra = 0;

            /// <summary>
            /// </summary>
            public int cbWndExtra = 0;

            /// <summary>
            /// </summary>
            public IntPtr hInstance = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hIcon = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hCursor = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hbrBackground = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr lpszMenuName = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr lpszClassName = IntPtr.Zero;

            /// <summary>
            /// </summary>
            public IntPtr hIconSm = IntPtr.Zero;
        }


        /// <summary>
        /// KEYBDINPUT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            /// <summary>KEYBDINPUT flag value</summary>
            public IntPtr type;
            /// <summary>KEYBDINPUT flag value</summary>
            public short wVk;
            /// <summary>KEYBDINPUT flag value</summary>
            public short wScan;
            /// <summary>KEYBDINPUT flag value</summary>
            public int dwFlags;
            /// <summary>KEYBDINPUT flag value</summary>
            public IntPtr time;
            /// <summary>KEYBDINPUT flag value</summary>
            public IntPtr dwExtraInfo;

            // Padding needed to make this the same size as an overall INPUT
            // struct, which is a union of keyboard, mouse and hardware input structs.

            /// <summary>KEYBDINPUT flag value</summary>
            public int padding1;
            /// <summary>KEYBDINPUT flag value</summary>
            public int padding2;
        }


        /// <summary>
        /// MOUSEINPUT
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            /// <summary>MOUSEINPUT flag value</summary>
            public IntPtr type;
            /// <summary>MOUSEINPUT flag value</summary>
            public int dx;
            /// <summary>MOUSEINPUT flag value</summary>
            public int dy;
            /// <summary>MOUSEINPUT flag value</summary>
            public int mouseData;
            /// <summary>MOUSEINPUT flag value</summary>
            public int dwFlags;
            /// <summary>MOUSEINPUT flag value</summary>
            public IntPtr time;
            /// <summary>MOUSEINPUT flag value</summary>
            public IntPtr dwExtraInfo;
        }

        /// <summary>
        ///
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct NMHDR
        {
            /// <summary>
            ///
            /// </summary>
            public IntPtr hwndFrom;

            /// <summary>
            ///
            /// </summary>
            public int idFrom;

            /// <summary>
            ///
            /// </summary>
            public int code;
        }



        /// <summary>
        /// POINT aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        /// <summary> Win32 </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            /// <summary> Win32 </summary>
            public int left;
            /// <summary> Win32 </summary>
            public int top;
            /// <summary> Win32 </summary>
            public int right;
            /// <summary> Win32 </summary>
            public int bottom;

            /// <summary> Win32 </summary>
            public static readonly RECT Empty = new RECT();

            /// <summary> Win32 </summary>
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            /// <summary> Win32 </summary>
            public int Height
            {
                get { return bottom - top; }
            }

            /// <summary> Win32 </summary>
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }


            /// <summary> Win32 </summary>
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }

            /// <summary> Win32 </summary>
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString()
            {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is RECT)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode()
            {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }


            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2)
            {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2)
            {
                return !(rect1 == rect2);
            }


        }


        /// <summary>
        /// Constat for Mil Windows
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Auto)]
        public struct TVINSERTSTRUCT
        {
            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public IntPtr hParent;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public IntPtr hInsertAfter;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_mask;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_hItem;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_state;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_stateMask;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public string item_pszText;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_cchTextMax;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_iImage;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_iSelectedImage;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_cChildren;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public IntPtr item_lParam;

            /// <summary>
            /// Constat for Mil Windows
            /// </summary>
            public int item_iIntegral;
        }



        ///<summary>
        /// This is for Win32 file dialogs
        ///</summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OPENFILENAME_I
        {
            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int lStructSize = Marshal.SizeOf(typeof(OPENFILENAME_I)); //ndirect.DllLib.sizeOf(this);

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr hwndOwner;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr hInstance;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public string lpstrFilter;   // use embedded nulls to separate filters

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr lpstrCustomFilter;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int nMaxCustFilter;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int nFilterIndex;
            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr lpstrFile;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int nMaxFile = NativeConstants.MAX_PATH;


            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr lpstrFileTitle;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int nMaxFileTitle = NativeConstants.MAX_PATH;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public string lpstrInitialDir;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public string lpstrTitle;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int Flags;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public short nFileOffset;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public short nFileExtension;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public string lpstrDefExt;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr lCustData;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public NativeStructs.WndProc lpfnHook;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public string lpTemplateName;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr pvReserved;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int dwReserved;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int FlagsEx;
        }



        ///<summary>
        /// This is for Win32 file dialogs
        ///</summary>
        [StructLayout(LayoutKind.Sequential)]
        public class OFNOTIFY
        {
            // hdr was a by-value NMHDR structure
            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr hdr_hwndFrom;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr hdr_idFrom;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public int hdr_code;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr lpOFN;

            ///<summary>
            /// This is for Win32 file dialogs
            ///</summary>
            public IntPtr pszFile;
        }

        /// <summary>
        /// Used by NtDll.RtlVerifyVersionInfo
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        [CLSCompliant(false)]
        public struct RTL_OSVERSIONINFOEXW
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;

            [MarshalAsAttribute(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;

            public ushort wServicePackMajor;
            public ushort wServicePackMinor;
            public ushort wSuiteMask;
            public byte wProductType;
            public byte wReserved;

            public RTL_OSVERSIONINFOEXW(int x)
            {
                var size = Marshal.SizeOf(typeof(RTL_OSVERSIONINFOEXW));
                this.dwOSVersionInfoSize = (uint)size;

                this.dwMajorVersion = 0;
                this.dwMinorVersion = 0;
                this.dwBuildNumber = 0;
                this.dwPlatformId = 0;
                this.szCSDVersion = null;
                this.wServicePackMajor = 0;
                this.wServicePackMinor = 0;
                this.wSuiteMask = 0;
                this.wProductType = 0;
                this.wReserved = 0;
            }
        }

    }
}
