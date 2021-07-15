// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

#if RIBBON_IN_FRAMEWORK
namespace System.Windows.Controls.Ribbon
#else
namespace Microsoft.Windows.Controls.Ribbon
#endif
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Windows;
    using MS.Internal;

    internal static class NativeMethods
    {
        public const int MONITOR_DEFAULTTONEAREST = 2;
        internal const int COMBINE_RGN_OR = 2;

        /// <summary>
        /// These are the flags used to create our window.
        /// </summary>
        internal const SWP SWPFlags = SWP.FRAMECHANGED
                            | SWP.NOSIZE
                            | SWP.NOMOVE
                            | SWP.NOZORDER
                            | SWP.NOOWNERZORDER
                            | SWP.NOACTIVATE;

        /// <summary>
        /// SetWindowPos options
        /// </summary>
        [Flags]
        internal enum SWP
        {
            ASYNCWINDOWPOS = 0x4000,
            DEFERERASE = 0x2000,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            HIDEWINDOW = 0x0080,
            NOACTIVATE = 0x0010,
            NOCOPYBITS = 0x0100,
            NOMOVE = 0x0002,
            NOOWNERZORDER = 0x0200,
            NOREDRAW = 0x0008,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            NOSIZE = 0x0001,
            NOZORDER = 0x0004,
            SHOWWINDOW = 0x0040,
        }

        internal enum SC
        {
            SIZE = 0xF000,
            MOVE = 0xF010,
            MINIMIZE = 0xF020,
            MAXIMIZE = 0xF030,
            NEXTWINDOW = 0xF040,
            PREVWINDOW = 0xF050,
            CLOSE = 0xF060,
            VSCROLL = 0xF070,
            HSCROLL = 0xF080,
            MOUSEMENU = 0xF090,
            KEYMENU = 0xF100,
            ARRANGE = 0xF110,
            RESTORE = 0xF120,
            TASKLIST = 0xF130,
            SCREENSAVE = 0xF140,
            HOTKEY = 0xF150,
            DEFAULT = 0xF160,
            MONITORPOWER = 0xF170,
            CONTEXTHELP = 0xF180,
            SEPARATOR = 0xF00F,
            /// <summary>
            /// SCF_ISSECURE
            /// </summary>
            F_ISSECURE = 0x00000001,
            ICON = MINIMIZE,
            ZOOM = MAXIMIZE,
        }
        
        internal enum HT
        {
            ERROR = -2,
            TRANSPARENT = -1,
            NOWHERE = 0,
            CLIENT = 1,
            CAPTION = 2,
            SYSMENU = 3,
            GROWBOX = 4,
            MENU = 5,
            HSCROLL = 6,
            VSCROLL = 7,
            MINBUTTON = 8,
            MAXBUTTON = 9,
            LEFT = 10,
            RIGHT = 11,
            TOP = 12,
            TOPLEFT = 13,
            TOPRIGHT = 14,
            BOTTOM = 15,
            BOTTOMLEFT = 16,
            BOTTOMRIGHT = 17,
            BORDER = 18,
            OBJECT = 19,
            CLOSE = 20,
            HELP = 21
        }

        [Flags]
        internal enum WS : uint
        {
            OVERLAPPED = 0x00000000,
            POPUP = 0x80000000,
            CHILD = 0x40000000,
            MINIMIZE = 0x20000000,
            VISIBLE = 0x10000000,
            DISABLED = 0x08000000,
            CLIPSIBLINGS = 0x04000000,
            CLIPCHILDREN = 0x02000000,
            MAXIMIZE = 0x01000000,
            BORDER = 0x00800000,
            DLGFRAME = 0x00400000,
            VSCROLL = 0x00200000,
            HSCROLL = 0x00100000,
            SYSMENU = 0x00080000,
            THICKFRAME = 0x00040000,
            GROUP = 0x00020000,
            TABSTOP = 0x00010000,

            MINIMIZEBOX = 0x00020000,
            MAXIMIZEBOX = 0x00010000,

            CAPTION = BORDER | DLGFRAME,
            TILED = OVERLAPPED,
            ICONIC = MINIMIZE,
            SIZEBOX = THICKFRAME,
            TILEDWINDOW = OVERLAPPEDWINDOW,

            OVERLAPPEDWINDOW = OVERLAPPED | CAPTION | SYSMENU | THICKFRAME | MINIMIZEBOX | MAXIMIZEBOX,
            POPUPWINDOW = POPUP | BORDER | SYSMENU,
            CHILDWINDOW = CHILD,
        }

        internal enum WM
        {
            NULL = 0x0000,
            CREATE = 0x0001,
            DESTROY = 0x0002,
            MOVE = 0x0003,
            SIZE = 0x0005,
            ACTIVATE = 0x0006,
            SETFOCUS = 0x0007,
            KILLFOCUS = 0x0008,
            ENABLE = 0x000A,
            SETREDRAW = 0x000B,
            SETTEXT = 0x000C,
            GETTEXT = 0x000D,
            GETTEXTLENGTH = 0x000E,
            PAINT = 0x000F,
            CLOSE = 0x0010,
            QUERYENDSESSION = 0x0011,
            QUIT = 0x0012,
            QUERYOPEN = 0x0013,
            ERASEBKGND = 0x0014,
            SYSCOLORCHANGE = 0x0015,

            WINDOWPOSCHANGING = 0x0046,
            WINDOWPOSCHANGED = 0x0047,

            SETICON = 0x0080,
            NCCREATE = 0x0081,
            NCDESTROY = 0x0082,
            NCCALCSIZE = 0x0083,
            NCHITTEST = 0x0084,
            NCPAINT = 0x0085,
            NCACTIVATE = 0x0086,
            GETDLGCODE = 0x0087,
            SYNCPAINT = 0x0088,
            NCMOUSEMOVE = 0x00A0,
            NCLBUTTONDOWN = 0x00A1,
            NCLBUTTONUP = 0x00A2,
            NCLBUTTONDBLCLK = 0x00A3,
            NCRBUTTONDOWN = 0x00A4,
            NCRBUTTONUP = 0x00A5,
            NCRBUTTONDBLCLK = 0x00A6,
            NCMBUTTONDOWN = 0x00A7,
            NCMBUTTONUP = 0x00A8,
            NCMBUTTONDBLCLK = 0x00A9,

            SYSKEYDOWN = 0x0104,
            SYSKEYUP = 0x0105,
            SYSCHAR = 0x0106,
            SYSDEADCHAR = 0x0107,
            SYSCOMMAND = 0x0112,

            DWMCOMPOSITIONCHANGED = 0x031E,
            USER = 0x0400,
            APP = 0x8000,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Message
        {
            public IntPtr handle;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT p;
        }

        [Flags]
        internal enum WS_EX : uint
        {
            None = 0,
            DLGMODALFRAME = 0x00000001,
            NOPARENTNOTIFY = 0x00000004,
            TOPMOST = 0x00000008,
            ACCEPTFILES = 0x00000010,
            TRANSPARENT = 0x00000020,
            MDICHILD = 0x00000040,
            TOOLWINDOW = 0x00000080,
            WINDOWEDGE = 0x00000100,
            CLIENTEDGE = 0x00000200,
            CONTEXTHELP = 0x00000400,
            RIGHT = 0x00001000,
            LEFT = 0x00000000,
            RTLREADING = 0x00002000,
            LTRREADING = 0x00000000,
            LEFTSCROLLBAR = 0x00004000,
            RIGHTSCROLLBAR = 0x00000000,
            CONTROLPARENT = 0x00010000,
            STATICEDGE = 0x00020000,
            APPWINDOW = 0x00040000,
            LAYERED = 0x00080000,
            NOINHERITLAYOUT = 0x00100000, // Disable inheritence of mirroring by children
            LAYOUTRTL = 0x00400000, // Right to left mirroring
            COMPOSITED = 0x02000000,
            NOACTIVATE = 0x08000000,
            OVERLAPPEDWINDOW = (WINDOWEDGE | CLIENTEDGE),
            PALETTEWINDOW = (WINDOWEDGE | TOOLWINDOW | TOPMOST),
        }
        
        internal enum GWL
        {
            WNDPROC = (-4),
            HINSTANCE = (-6),
            HWNDPARENT = (-8),
            STYLE = (-16),
            EXSTYLE = (-20),
            USERDATA = (-21),
            ID = (-12)
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;      // width of left border that retains its size
            public int cxRightWidth;     // width of right border that retains its size
            public int cyTopHeight;      // height of top border that retains its size
            public int cyBottomHeight;   // height of bottom border that retains its size
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        public class MONITORINFOEX
        {
            internal int cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
            internal RECT rcMonitor = new RECT();
            internal RECT rcWork = new RECT();
            internal int dwFlags;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
            internal char[] szDevice = new char[0x20];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public int flags;
        }

        /// <summary>
        /// EnableMenuItem uEnable values, MF_*
        /// </summary>
        [Flags]
        internal enum MF : uint
        {
            ENABLED = 0,
            BYCOMMAND = 0,
            GRAYED = 1,
            DISABLED = 2,
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern IntPtr MonitorFromRect(ref RECT rect, int flags);

        [DllImport("user32.dll", EntryPoint = "GetMonitorInfo", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MONITORINFOEX info);
 
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDoubleClickTime();

        [DllImport("user32.dll")]
        public static extern int MessageBeep(int uType);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetCapture();

        /// <summary>
        /// GetDeviceCaps nIndex values.
        /// </summary>
        internal enum DeviceCap
        {
            /// <summary>Number of bits per pixel
            /// </summary>
            BITSPIXEL = 12,
            /// <summary>
            /// Number of planes
            /// </summary>
            PLANES = 14,
        }
  
        [DllImport("gdi32.dll", SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern int GetDeviceCaps(HandleRef hDC, int nIndex);
      
        [DllImport("user32.dll", SetLastError = true, ExactSpelling = true, EntryPoint = "GetDC", CharSet = CharSet.Auto)]
        private static extern IntPtr IntGetDC(HandleRef hWnd);
        public static IntPtr GetDC(HandleRef hWnd)
        {
            IntPtr hDc = IntGetDC(hWnd);
            if (hDc == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            return HandleCollector.Add(hDc, NativeMethods.CommonHandles.HDC);
        }
       
        [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "ReleaseDC", CharSet = CharSet.Auto)]
        private static extern int IntReleaseDC(HandleRef hWnd, HandleRef hDC);
        public static int ReleaseDC(HandleRef hWnd, HandleRef hDC)
        {
            HandleCollector.Remove((IntPtr)hDC, NativeMethods.CommonHandles.HDC);
            return IntReleaseDC(hWnd, hDC);
        }

        public static class CommonHandles
        {
            static CommonHandles()
            {
            }

            /// <devdoc>
            ///     Handle type for HDC's that count against the Win98 limit of five DC's.  HDC's
            ///     which are not scarce, such as HDC's for bitmaps, are counted as GDIHANDLE's.
            /// </devdoc>
            public static readonly int HDC = HandleCollector.RegisterType("HDC", 100, 2); // wait for 2 dc's before collecting
        }

        #region Helper Functions
        /// <summary>
        ///     Converts a rectangle from an Avalon Rect to a Win32 RECT
        /// </summary>
        /// <remarks>
        ///     Rounds "double" values to the nearest "int"
        /// </remarks>
        /// <param name="rect">
        ///     The rectangle as an Avalon Rect
        /// </param>
        /// <returns>
        ///     The rectangle as a Win32 RECT
        /// </returns>
        internal static NativeMethods.RECT FromRect(Rect rect)
        {
            NativeMethods.RECT rc = new NativeMethods.RECT();

            rc.top = DoubleUtil.DoubleToInt(rect.Y);
            rc.left = DoubleUtil.DoubleToInt(rect.X);
            rc.bottom = DoubleUtil.DoubleToInt(rect.Bottom);
            rc.right = DoubleUtil.DoubleToInt(rect.Right);

            return rc;
        }

        /// <summary>
        ///     Converts a rectangle from a Win32 RECT to an Avalon Rect
        /// </summary>
        /// <param name="rc">
        ///     The rectangle as a Win32 RECT
        /// </param>
        /// <returns>
        ///     The rectangle as an Avalon Rect
        /// </returns>
        internal static Rect ToRect(NativeMethods.RECT rc)
        {
            Rect rect = new Rect();

            rect.X = rc.left;
            rect.Y = rc.top;
            rect.Width = rc.right - rc.left;
            rect.Height = rc.bottom - rc.top;

            return rect;
        }
        #endregion
    }

    #region HandleCollector

    internal static class HandleCollector
    {
        private static HandleType[] handleTypes;
        private static int handleTypeCount = 0;

        private static Object handleMutex = new Object();

        /// <devdoc>
        ///     Adds the given handle to the handle collector.  This keeps the
        ///     handle on a "hot list" of objects that may need to be garbage
        ///     collected.
        /// </devdoc>
        internal static IntPtr Add(IntPtr handle, int type)
        {
            handleTypes[type - 1].Add();
            return handle;
        }

        /// <devdoc>
        ///     Registers a new type of handle with the handle collector.
        /// </devdoc>
        internal static int RegisterType(string typeName, int expense, int initialThreshold)
        {
            lock (handleMutex)
            {
                if (handleTypeCount == 0 || handleTypeCount == handleTypes.Length)
                {
                    HandleType[] newTypes = new HandleType[handleTypeCount + 10];
                    if (handleTypes != null)
                    {
                        Array.Copy(handleTypes, 0, newTypes, 0, handleTypeCount);
                    }
                    handleTypes = newTypes;
                }

                handleTypes[handleTypeCount++] = new HandleType(typeName, expense, initialThreshold);
                return handleTypeCount;
            }
        }

        /// <devdoc>
        ///     Removes the given handle from the handle collector.  Removing a
        ///     handle removes it from our "hot list" of objects that should be
        ///     frequently garbage collected.
        /// </devdoc>
        internal static IntPtr Remove(IntPtr handle, int type)
        {
            handleTypes[type - 1].Remove();
            return handle;
        }

        /// <devdoc>
        ///     Represents a specific type of handle.
        /// </devdoc>
        private class HandleType
        {
            internal readonly string name;

            private int initialThreshHold;
            private int threshHold;
            private int handleCount;
            private readonly int deltaPercent;

            /// <devdoc>
            ///     Creates a new handle type.
            /// </devdoc>
            internal HandleType(string name, int expense, int initialThreshHold)
            {
                this.name = name;
                this.initialThreshHold = initialThreshHold;
                this.threshHold = initialThreshHold;
                this.deltaPercent = 100 - expense;
            }

            /// <devdoc>
            ///     Adds a handle to this handle type for monitoring.
            /// </devdoc>
            internal void Add()
            {
                bool performCollect = false;

                lock (this)
                {
                    handleCount++;
                    performCollect = NeedCollection();

                    if (!performCollect)
                    {
                        return;
                    }
                }

                if (performCollect)
                {
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> Forcing garbage collect");
                    Debug.WriteLine("HC>     name        :" + name);
                    Debug.WriteLine("HC>     threshHold  :" + (threshHold).ToString());
                    Debug.WriteLine("HC>     handleCount :" + (handleCount).ToString());
                    Debug.WriteLine("HC>     deltaPercent:" + (deltaPercent).ToString());
#endif
                    GC.Collect();

                    // We just performed a GC.  If the main thread is in a tight
                    // loop there is a this will cause us to increase handles forever and prevent handle collector
                    // from doing its job.  Yield the thread here.  This won't totally cause
                    // a finalization pass but it will effectively elevate the priority
                    // of the finalizer thread just for an instant.  But how long should
                    // we sleep?  We base it on how expensive the handles are because the
                    // more expensive the handle, the more critical that it be reclaimed.
                    int sleep = (100 - deltaPercent) / 4;
                    System.Threading.Thread.Sleep(sleep);
                }
            }


            /// <devdoc>
            ///     Determines if this handle type needs a garbage collection pass.
            /// </devdoc>
            internal bool NeedCollection()
            {

                if (handleCount > threshHold)
                {
                    threshHold = handleCount + ((handleCount * deltaPercent) / 100);
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> NeedCollection: increase threshHold to " + threshHold);
#endif
                    return true;
                }

                // If handle count < threshHold, we don't
                // need to collect, but if it 10% below the next lowest threshhold we
                // will bump down a rung.  We need to choose a percentage here or else
                // we will oscillate.
                //
                int oldThreshHold = (100 * threshHold) / (100 + deltaPercent);
                if (oldThreshHold >= initialThreshHold && handleCount < (int)(oldThreshHold * .9F))
                {
#if DEBUG_HANDLECOLLECTOR
                    Debug.WriteLine("HC> NeedCollection: throttle threshhold " + threshHold + " down to " + oldThreshHold);
#endif
                    threshHold = oldThreshHold;
                }

                return false;
            }

            /// <devdoc>
            ///     Removes the given handle from our monitor list.
            /// </devdoc>
            internal void Remove()
            {
                lock (this)
                {
                    handleCount--;

                    handleCount = Math.Max(0, handleCount);
                }
            }
        }
    }

    #endregion

}
