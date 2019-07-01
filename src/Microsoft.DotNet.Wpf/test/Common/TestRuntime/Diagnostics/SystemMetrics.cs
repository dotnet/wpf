// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Microsoft.Test.Win32;

namespace Microsoft.Test.Diagnostics
{
    /// <summary>
    /// Provides a light-weight wrapper around the user32 GetSystemMetrics API call.
    /// </summary>
    /// <remarks>
    /// This class provides similar capabilties of as the SystemInformation class in the 
    /// Windows forms namespace without the dependency.
    /// </remarks>
    internal static class SystemMetrics
    {
        /// <summary>
        /// Retrieves a <see cref="SystemMetric"/>.
        /// </summary>
        /// <param name="index">The <see cref="SystemMetric"/> to retrieve.</param>
        /// <returns>The metric value; otherwise, zero if the metric could not be retreived.</returns>
        public static int GetSystemMetric(SystemMetric index)
        {
            int metric = User32.GetSystemMetrics((int)index);
            int hResult = Marshal.GetLastWin32Error();

            return metric;
        }
    }

    /// <summary>
    /// Defines the values pass to <see cref="SystemMetrics.GetSystemMetric"/>.
    /// </summary>
    internal enum SystemMetric
    {
        /// <summary>
        /// The width, in pixels, of the screen of the primary display monitor.
        /// </summary>
        ScreenWidth = 0,

        /// <summary>
        /// The height, in pixels, of the screen of the primary display monitor.
        /// </summary>
        ScreenHeight = 1,

        /// <summary>
        /// The width, in pixels, of the vertical scroll bar.
        /// </summary>
        VerticalScrollbarWidth = 2,

        /// <summary>
        /// The height, in pixels, of a horizontal scroll bar.
        /// </summary>
        HorizontalScrollbarHeight = 3,

        /// <summary>
        /// The height, in pixels, if the caption area.
        /// </summary>
        CaptionHeight = 4,

        /// <summary>
        /// The width of a window border, in pixels. This is equivalent to the <see cref="Border3DWidth"/> value for windows with the 3-D look.
        /// </summary>
        BorderWidth = 5,

        /// <summary>
        /// The height of a window border, in pixels.
        /// </summary>
        /// <remarks>
        /// This is equivalent to the <see cref="Border3DHeight"/> value for windows with the 3-D look.
        /// </remarks>
        BorderHeight = 6,

        /// <summary>
        /// The width, in pixels, of the left and right edges of the focus rectangle drawn by DrawFocusRect.
        /// </summary>
        FocusBorderWidth = 7,

        /// <summary>
        /// The height, in pixels, of the left and right edges of the focus rectangle drawn by DrawFocusRect.
        /// </summary>
        FocusBorderHeight = 84,

        /// <summary>
        /// The height of the thumb box in a horizontal scroll bar, in pixels.
        /// </summary>
        ScrollThumbHeight = 9,

        /// <summary>
        /// The width of the thumb box in a horizontal scroll bar, in pixels.
        /// </summary>
        ScrollThumbWidth = 10,

        /// <summary>
        /// The default width of an icon, in pixels.
        /// </summary>
        /// <remarks>
        /// The LoadIcon function can load only icons with the dimensions specified by <see cref="IconWidth"/> and <see cref="IconHeight"/>.
        /// </remarks>
        IconWidth = 11,

        /// <summary>
        /// The default height of an icon, in pixels.
        /// </summary>
        /// <remarks>
        /// The LoadIcon function can load only icons with the dimensions specified by <see cref="IconWidth"/> and <see cref="IconHeight"/>.
        /// </remarks>
        IconHeight = 12,

        /// <summary>
        /// The width of a grid cell for items in large icon view, in pixels.
        /// </summary>
        /// <remarks>
        /// item fits into a rectangle of size <see cref="IconSpacingWidth"/> by <see cref="IconSpacingHeight"/>
        /// when arranged. This value is always greater than or equal to <see cref="IconWidth"/>.
        /// </remarks>
        IconSpacingWidth = 38,

        /// <summary>
        /// The height of a grid cell for items in large icon view, in pixels.
        /// </summary>
        /// <remarks>
        /// item fits into a rectangle of size <see cref="IconSpacingWidth"/> by <see cref="IconSpacingHeight"/>
        /// when arranged. This value is always greater than or equal to <see cref="IconHeight"/>.
        /// </remarks>
        IconSpacingHeight = 39,

        /// <summary>
        /// Width of a cursor, in pixels. The system cannot create cursors of other sizes.
        /// </summary>
        CursorWidth = 13,

        /// <summary>
        /// The height, in pixels, of a cursor.
        /// </summary>
        /// <remarks>
        /// The system cannot create cursors of other sizes.
        /// </remarks>
        CursorHeight = 14,

        /// <summary>
        /// The height, in pixels, of a single line menu bar.
        /// </summary>
        MenuHeight = 15,

        /// <summary>
        /// The width, in pixels, of the client area for a full-screen window on the primary display monitor.
        /// </summary>
        FullScreenWidth = 16,

        /// <summary>
        /// The height, in pixels, of the client area for a full-screen window on the primary display monitor.
        /// </summary>
        FullScreenHeight = 17,

        /// <summary>
        /// The height of the Kanji window at the bottom of the screen, in pixels.
        /// </summary>
        /// <remarks>
        /// This metric is for double byte character set versions of the system.
        /// </remarks>
        CYKanjiWindow = 18,

        /// <summary>
        /// Non-zero if a mouse is present; otherwise, zero.
        /// </summary>
        MousePresent = 19,

        /// <summary>
        /// The height of the arrow bitmap on a horizontal scroll bar, in pixels.
        /// </summary>
        ScrollBitmapHeight = 20,

        /// <summary>
        /// The width of the arrow bitmap on a horizontal scroll bar, in pixels.
        /// </summary>
        ScrollBitmapWidth = 21,

        /// <summary>
        /// Nonzero if the debug version of User.exe is installed; otherwise, zero.
        /// </summary>
        Debug = 22,

        /// <summary>
        /// Nonzero if the meanings of the left and right mouse buttons are swapped; zero otherwise.
        /// </summary>
        SwapButton = 23,

        /// <summary>
        /// The minimum width of a window, in pixels.
        /// </summary>
        MinimumWindowWidth = 28,

        /// <summary>
        /// The minimum height of a window, in pixels.
        /// </summary>
        MinimumWindowHeight = 29,


        /// <summary>
        /// The width, in pixels, of a button in a window's caption or title bar.
        /// </summary>
        CaptionButtonWidth = 30,

        /// <summary>
        /// The height, in pixels, of a button in a window's caption or title bar.
        /// </summary>
        CaptionButtonHeight = 31,

        /// <summary>
        /// The horizontal thickness, in pixels, of the sizing border around the perimeter of a window that can be resized, in pixels.
        /// </summary>
        SizableFrameWidth = 32,

        /// <summary>
        /// The vertical thickness, in pixels, of the sizing border around the perimeter of a window that can be resized, in pixels.
        /// </summary>
        SizableFrameHeight = 33,

        /// <summary>
        /// The minimum tracking width of a window, in pixels.
        /// </summary>
        /// <remarks>
        /// The user cannot drag the window frame to a size smaller than these dimensions.
        /// A window can override this value by processing the WM_GETMINMAXINFO message.
        /// </remarks>
        MinimumTrackWidth = 34,

        /// <summary>
        /// The minimum tracking height of a window, in pixels.
        /// </summary>
        /// <remarks>
        /// The user cannot drag the window frame to a size smaller than these dimensions.
        /// A window can override this value by processing the WM_GETMINMAXINFO message.
        /// </remarks>
        MinimumTrackHeight = 35,

        /// <summary>
        /// The width of the rectangle around the location of a first click in a double-click sequence, in pixels.
        /// <remarks>
        /// The second click must occur within the rectangle defined by <see cref="DoubleClickWidth"/> and <see cref="DoubleClickHeight"/>
        /// for the system to consider the two clicks a double-click.
        /// <para>
        /// The two clicks must also occur within a specified time.
        /// </para>
        /// </remarks>
        /// </summary>
        DoubleClickWidth = 36,

        /// <summary>
        /// The height of the rectangle around the location of a first click in a double-click sequence, in pixels.
        /// <remarks>
        /// The second click must occur within the rectangle defined by <see cref="DoubleClickWidth"/> and <see cref="DoubleClickHeight"/>
        /// for the system to consider the two clicks a double-click.
        /// <para>
        /// The two clicks must also occur within a specified time.
        /// </para>
        /// </remarks>
        /// </summary>
        DoubleClickHeight = 37,

        /// <summary>
        /// Nonzero if drop-down menus are right-aligned with the corresponding menu-bar item; zero if the menus are left-aligned.
        /// </summary>
        MenuDropAlignment = 40,

        /// <summary>
        /// Nonzero if the Microsoft Windows for Pen computing extensions are installed; otherwise, zero.
        /// </summary>
        PenWindows = 41,

        /// <summary>
        /// Nonzero if User32.dll supports DBCS; otherwise, zero.
        /// </summary>
        /// <remarks>
        /// Windows Me/98/95:  Nonzero if the double-byte character-set (DBCS) version of User.exe is installed; otherwise, zero.
        /// </remarks>
        DBCSEnabled = 42,

        /// <summary>
        /// Number of buttons on mouse, or zero if no mouse is installed.
        /// </summary>
        MouseButtons = 43,

        /// <summary>
        /// The thickness, in pixels, of the horizontal frame border around the perimeter of a window that has a caption but is not sizable.
        /// </summary>
        FixedFrameWidth = 7,

        /// <summary>
        /// The thickness, in pixels, of the vertical frame border around the perimeter of a window that has a caption but is not sizable.
        /// </summary>
        FixedFrameHeight = 8,

        /// <summary>
        /// Nonzero if security is present; otherwise, zero.
        /// </summary>
        Secure = 44,

        /// <summary>
        /// The width of a 3-D border, in pixels.
        /// </summary>
        /// <remarks>
        ///  This is the 3-D counterpart of <see cref="BorderWidth"/>.
        /// </remarks>
        Border3DWidth = 45,

        /// <summary>
        /// The height of a 3-D border, in pixels.
        /// </summary>
        /// <remarks>
        ///  This is the 3-D counterpart of <see cref="BorderHeight"/>.
        /// </remarks>
        Border3DHeight = 46,

        /// <summary>
        /// The width of a grid cell for a minimized window, in pixels.
        /// </summary>
        /// <remarks>
        /// Each minimized window fits into a rectangle this size when arranged.
        /// <para>
        /// This value is always greater than or equal to <see cref="MinimizedWindowWidth"/>.
        /// </para>
        /// </remarks>
        MinimizedWindowSpacingWidth = 47,

        /// <summary>
        /// The height of a grid cell for a minimized window, in pixels.
        /// </summary>
        /// <remarks>
        /// Each minimized window fits into a rectangle this size when arranged.
        /// <para>
        /// This value is always greater than or equal to <see cref="MinimizedWindowWidth"/>.
        /// </para>
        /// </remarks>
        MaximizedWindowSpacingHeight = 48,

        /// <summary>
        /// The recommended width, in pixels, of a small icon.
        /// </summary>
        /// <remarks>
        /// Small icons typically appear in window captions and in small icon view.
        /// </remarks>
        SmallIconWidth = 49,

        /// <summary>
        /// The recommended height, in pixels, of a small icon.
        /// </summary>
        /// <remarks>
        /// Small icons typically appear in window captions and in small icon view.
        /// </remarks>
        SmallIconHeight = 50,

        /// <summary>
        /// The height of a small caption, in pixels.
        /// </summary>
        CYSmCaption = 51,

        /// <summary>
        /// The width, in pixels, of small caption buttons.
        /// </summary>
        SmallCaptionButtonWidth = 52,

        /// <summary>
        /// The height, in pixels, of small caption buttons.
        /// </summary>
        SmallCaptionButtonHeight = 53,

        /// <summary>
        /// The width, in pixels, of menu bar buttons, such as the child window close button used in the multiple document interface.
        /// </summary>
        MenuButtonWidth = 54,

        /// <summary>
        /// The height, in pixels, of menu bar buttons, such as the child window close button used in the multiple document interface.
        /// </summary>
        MenuButtonHeight = 55,

        /// <summary>
        ///  Specifies how the system arranged minimized windows.
        /// </summary>
        Arrange = 56,

        /// <summary>
        /// The width of a minimized window, in pixels.
        /// </summary>
        MinimizedWindowWidth = 57,

        /// <summary>
        /// The height of a minimized window, in pixels.
        /// </summary>
        MinimizedWindowHeight = 58,

        /// <summary>
        /// The default maximum width of a window that has a caption and sizing borders, in pixels.
        /// </summary>
        /// <remarks>
        /// This metric refers to the entire desktop. The user cannot drag the window frame to a
        /// size larger than these dimensions. A window can override this value by processing the
        /// WM_GETMINMAXINFO message.
        /// </remarks>
        MaximimumTrackWidth = 59,

        /// <summary>
        /// The default maximum height of a window that has a caption and sizing borders, in pixels.
        /// </summary>
        /// <remarks>
        /// This metric refers to the entire desktop. The user cannot drag the window frame to a
        /// size larger than these dimensions. A window can override this value by processing the
        /// WM_GETMINMAXINFO message.
        /// </remarks>
        MaximimumTrackHeight = 60,

        /// <summary>
        /// The default width, in pixels, of a maximized top-level window on the primary display monitor.
        /// </summary>
        MaximizedWidth = 61,

        /// <summary>
        /// The default height, in pixels, of a maximized top-level window on the primary display monitor.
        /// </summary>
        MaximizedHeight = 62,

        /// <summary>
        /// The least significant bit is set if a network is present; otherwise, it is cleared.
        /// </summary>
        /// <remarks>
        /// The other bits are reserved for future use.
        /// </remarks>
        Network = 63,

        /// <summary>
        /// Indicatges how the system was started:
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///     <listheader>
        ///         <term>Value</term>
        ///         <description>Description</description>
        ///     </listheader>
        ///     <item>
        ///         <term>zero</term>
        ///         <description>Normal boot.</description>
        ///     </item>
        ///     <item>
        ///         <term>Zero</term>
        ///         <description>Fail-safe boot.</description>
        ///     </item>
        ///     <item>
        ///         <term>Greater than zero</term>
        ///         <description>Fail-safe with network boot.</description>
        ///     </item>
        /// </list>
        /// <para>
        /// Fail-safe boot (also called SafeBoot, Safe Mode, or Clean Boot) bypasses the
        /// user's startup files.
        /// </para>
        /// </remarks>
        Cleanboot = 67,

        /// <summary>
        /// The width of a rectangle centered on a drag point to allow for limited movement
        /// of the mouse pointer before a drag operation begins, in pixels.
        /// </summary>
        /// <remarks>
        /// This allows the user to click and release the mouse button easily without
        /// unintentionally starting a drag operation.
        /// </remarks>
        DragWidth = 68,

        /// <summary>
        /// The height of a rectangle centered on a drag point to allow for limited movement
        /// of the mouse pointer before a drag operation begins, in pixels.
        /// </summary>
        /// <remarks>
        /// This allows the user to click and release the mouse button easily without
        /// unintentionally starting a drag operation.
        /// </remarks>
        DragHeight = 69,

        /// <summary>
        /// Nonzero if the user requires an application to present information visually
        /// in situations where it would otherwise present the information only in
        /// audible form; otherwise, zero.
        /// </summary>
        ShowSounds = 70,

        /// <summary>
        /// The width of the default menu check-mark bitmap, in pixels.
        /// </summary>
        MenuCheckWidth = 71,

        /// <summary>
        /// The height of the default menu check-mark bitmap, in pixels.
        /// </summary>
        MenuCheckHeight = 72,

        /// <summary>
        /// Nonzero if the computer has a low-end (slow) processor; zero otherwise.
        /// </summary>
        SlowMachine = 73,

        /// <summary>
        /// Nonzero if the system is enabled for Hebrew and Arabic languages; otherwise, zero.
        /// </summary>
        MiddleEastEnabled = 74,

        /// <summary>
        /// Nonzero if a mouse with a wheel is installed; otherwise, zero.
        /// </summary>
        /// <remarks>
        /// Windows NT 3.51 and earlier and Windows 95:  This value is not supported.
        /// </remarks>
        MouseWheelPresent = 75,

        /// <summary>
        /// The Coordinates for the left side of the virtual screen.
        /// </summary>
        /// <remarks>
        /// The virtual screen is the bounding rectangle of all display monitors.
        /// The <see cref="VirtualScreenWidth"/> metric is the width of the virtual screen.
        /// </remarks>
        VirtualScreenX = 76,

        /// <summary>
        /// The Coordinates for the top of the virtual screen.
        /// </summary>
        /// <remarks>
        /// The virtual screen is the bounding rectangle of all display monitors.
        /// The <see cref="VirtualScreenHeight"/> metric is the height of the virtual screen.
        /// </remarks>
        VirtualScreenY = 77,

        /// <summary>
        /// The width, in pixels, of the virtual screen.
        /// </summary>
        VirtualScreenWidth = 78,

        /// <summary>
        /// The height, in pixels, of the virtual screen.
        /// </summary>
        VirtualScreenHeight = 79,

        /// <summary>
        /// Number of display monitors on the desktop. See Remarks for more information.
        /// </summary>
        Monitors = 80,

        /// <summary>
        /// Nonzero if all the display monitors have the same color format; otherwise, zero.
        /// </summary>
        /// <remarks>
        /// Note that two displays can have the same bit depth, but different color formats.
        /// <para>
        /// For example, the red, green, and blue pixels can be encoded with different
        /// numbers of bits, or those bits can be located in different places in a pixel's color value.
        /// </para>
        /// <para>
        /// Windows NT and Windows 95:  This value is not supported.
        /// </para>
        /// </remarks>
        SameDisplayFormat = 81,

        /// <summary>
        /// Nonzero if Input Method Manager/Input Method Editor features are enabled; otherwise, zero.
        /// </summary>
        /// <remarks>
        /// Windows NT and Windows Me/98/95:  This value is not supported.
        /// <para>
        /// <see cref="IMMEnabled"/> indicates whether the system is ready to use a Unicode-based IME
        /// on a Unicode application. To ensure that a language-dependent IME works,
        /// check <see cref="DBCSEnabled"/> and the system ANSI code page. Otherwise the
        /// ANSI-to-Unicode conversion may not be performed correctly, or some components
        /// like fonts or registry setting may not be present.
        /// </para>
        /// </remarks>
        IMMEnabled = 82,

        /// <summary>
        /// Nonzero if the current operating system is the Windows XP Tablet PC edition; otherwise, zero.
        /// </summary>
        TabletPC = 86,

        /// <summary>
        /// Nonzero if the current operating system is the Windows XP, Media Center Edition; otherwise, zero.
        /// </summary>
        MediaCenter = 87,

        /// <summary>
        /// Windows XP Starter Edition.
        /// </summary>
        StarterEdition = 88,

        /// <summary>
        /// Windows Server 2003 R2.
        /// </summary>
        ServerR2 = 89,

        /// <summary>
        /// Nonzero if the calling process is associated with a Terminal Services client session; otherwise,
        /// zero if the calling process is associated with the Terminal Server console session.
        /// </summary>
        /// <remarks>
        /// The console session is not necessarily the physical console - see WTSGetActiveConsoleSessionId for more information. f
        /// <para>
        /// Windows NT 4.0 SP3 and earlier and Windows Me/98/95:  This value is not supported.
        /// </para>
        /// </remarks>
        RemoteSession = 0x1000,

        /// <summary>
        /// Nonzero if the current session is shutting down; otherwise, zero.
        /// </summary>
        /// <remarks>
        /// Windows 2000/NT and Windows Me/98/95:  This value is not supported.
        /// </remarks>
        ShuttingDown = 0x2000,

        /// <summary>
        /// Nonzero if the current session is remotely controlled; otherwise, zero.
        /// </summary>
        /// <remarks>
        /// This system metric is used in a Terminal Services environment.
        /// </remarks>
        RemoteControl = 0x2001,

        /// <summary>
        /// TBD
        /// </summary>
        CaretBlinkingEnabled = 0x2002
    }
}
