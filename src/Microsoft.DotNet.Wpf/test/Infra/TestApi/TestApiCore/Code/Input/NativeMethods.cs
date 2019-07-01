// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Test.Input
{
    internal static class NativeMethods
    {
        #region Const data

        private const string Gdi32Dll = "GDI32.dll";
        private const string User32Dll = "User32.dll";

        public const int INPUT_MOUSE = 0;
        public const int INPUT_KEYBOARD = 1;
        public const int INPUT_HARDWARE = 2;
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;
        public const uint KEYEVENTF_UNICODE = 0x0004;
        public const uint KEYEVENTF_SCANCODE = 0x0008;
        public const uint XBUTTON1 = 0x0001;
        public const uint XBUTTON2 = 0x0002;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_XDOWN = 0x0080;
        public const uint MOUSEEVENTF_XUP = 0x0100;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;
        public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;

        public const int VKeyShiftMask = 0x0100;
        public const int VKeyCharMask = 0x00FF;

        public const int VK_LBUTTON = 0x0001;
        public const int VK_RBUTTON = 0x0002;
        public const int VK_MBUTTON = 0x0004;
        public const int VK_XBUTTON1 = 0x0005;
        public const int VK_XBUTTON2 = 0x0006;

        public const int SMXvirtualscreen = 76;
        public const int SMYvirtualscreen = 77;
        public const int SMCxvirtualscreen = 78;
        public const int SMCyvirtualscreen = 79;

        public const int MouseeventfVirtualdesk = 0x4000;
        public const int WheelDelta = 120;

        #endregion Const data

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        /// <summary>
        /// The INPUT structure is used by SendInput to store information for synthesizing input events such as keystrokes, mouse movement, and mouse clicks. (see: http://msdn.microsoft.com/en-us/library/ms646270(VS.85).aspx)
        /// Declared in Winuser.h, include Windows.h
        /// </summary>
        /// <remarks>
        /// This structure contains information identical to that used in the parameter list of the keybd_event or mouse_event function.
        /// Windows 2000/XP: INPUT_KEYBOARD supports nonkeyboard input methods, such as handwriting recognition or voice recognition, as if it were text input by using the KEYEVENTF_UNICODE flag. For more information, see the remarks section of KEYBDINPUT.
        /// </remarks>
        public struct INPUT
        {
            /// <summary>
            /// Specifies the type of the input event. This member can be one of the following values. 
            /// InputType.MOUSE - The event is a mouse event. Use the mi structure of the union.
            /// InputType.KEYBOARD - The event is a keyboard event. Use the ki structure of the union.
            /// InputType.HARDWARE - Windows 95/98/Me: The event is from input hardware other than a keyboard or mouse. Use the hi structure of the union.
            /// </summary>
            public UInt32 Type;

            /// <summary>
            /// The data structure that contains information about the simulated Mouse, Keyboard or Hardware event.
            /// </summary>
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        /// <summary>
        /// The combined/overlayed structure that includes Mouse, Keyboard and Hardware Input message data (see: http://msdn.microsoft.com/en-us/library/ms646270(VS.85).aspx)
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        public struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;

            [FieldOffset(0)]
            public KEYBDINPUT Keyboard;

            [FieldOffset(0)]
            public HARDWAREINPUT Hardware;
        }

        #endregion Structs

        #region Methods

        [DllImport(User32Dll)]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport(User32Dll, CharSet = CharSet.Auto)]
        public static extern short VkKeyScan(char ch);

        [DllImport(User32Dll, SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport(User32Dll, ExactSpelling = true, EntryPoint = "GetSystemMetrics", CharSet = CharSet.Auto)]
        public static extern int GetSystemMetrics(int nIndex);

        /// <summary>Converts the client-area coordinates of a specified point to screen coordinates.</summary>
        /// <param name="hwndFrom">Handle to the window whose client area is used for the conversion.</param>
        /// <param name="pt">POINT structure that contains the client coordinates to be converted.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        [DllImport("user32.dll", EntryPoint = "ClientToScreen", CharSet = CharSet.Auto)]
        public static extern bool ClientToScreen(IntPtr hwndFrom, [In, Out] ref System.Drawing.Point pt);

        #endregion Methods
    }
}
