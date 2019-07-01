// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Unsafe P/Invokes used by UIAutomation

using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Collections;
using System.IO;
using System.Text;
using System.Security;

namespace Microsoft.Test.Input.Win32
{
    // This class *MUST* be internal for security purposes
    //CASRemoval:[SuppressUnmanagedCodeSecurity]
    internal class UnsafeNativeMethods
    {
        #region Public Members 

        public static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        public const int VK_SHIFT    = 0x10;
        public const int VK_CONTROL  = 0x11;
        public const int VK_MENU     = 0x12;

        public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const int KEYEVENTF_KEYUP       = 0x0002;
        public const int KEYEVENTF_UNICODE     = 0x0004;
        public const int KEYEVENTF_SCANCODE    = 0x0008;

        public const int MOUSEEVENTF_VIRTUALDESK = 0x4000;

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public int    type;
            public INPUTUNION    union;
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct INPUTUNION
        {
            [FieldOffset(0)] public MOUSEINPUT mouseInput;
            [FieldOffset(0)] public KEYBDINPUT keyboardInput;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int    dx;
            public int    dy;
            public int    mouseData;
            public int    dwFlags;
            public int    time;
            public IntPtr dwExtraInfo;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public short  wVk;
            public short  wScan;
            public int    dwFlags;
            public int    time;
            public IntPtr dwExtraInfo;
        };

        public const int INPUT_MOUSE             = 0;
        public const int INPUT_KEYBOARD          = 1;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendInput( int nInputs, ref INPUT mi, int cbSize );

        [DllImport("user32.dll", CharSet=CharSet.Auto)]
        public static extern int MapVirtualKey(int nVirtKey, int nMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetAsyncKeyState(int nVirtKey);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetKeyState(int nVirtKey);


        //
        // Keyboard state
        //
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int GetKeyboardState(byte[] keystate);

        [DllImport("user32.dll", ExactSpelling = true, EntryPoint = "keybd_event", CharSet = CharSet.Auto)]
        internal static extern void Keybd_event(byte vk, byte scan, int flags, int extrainfo);
        
        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        internal static extern int SetKeyboardState(byte[] keystate);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(
            NativeMethods.HWND hWnd, int nMsg, IntPtr wParam, IntPtr lParam);

        // Overload for WM_GETTEXT
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(
            NativeMethods.HWND hWnd, int nMsg, IntPtr wParam, StringBuilder lParam);

        public const int EM_SETSEL               = 0x00B1;
        public const int EM_GETLINECOUNT         = 0x00BA;
        public const int EM_LINEFROMCHAR         = 0x00C9;

        #endregion
    }
}
