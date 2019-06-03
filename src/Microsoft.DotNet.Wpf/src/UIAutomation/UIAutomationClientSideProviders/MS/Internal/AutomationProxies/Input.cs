// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provides mouse and keyboard input functionality

using System.Windows.Input;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MS.Win32;

using System;

namespace MS.Internal.AutomationProxies
{
    // Flags for Input.SendMouseInput, indicate whether movent took place,
    // or whether buttons were pressed or released.
    [Flags]
    internal enum SendMouseInputFlags
    {
        // Specifies that the pointer moved.
        Move       = 0x0001,
        // Specifies that the left button was pressed.
        LeftDown   = 0x0002,
        // Specifies that the left button was released.
        LeftUp     = 0x0004,
        // Specifies that the right button was pressed.
        RightDown  = 0x0008,
        // Specifies that the right button was released.
        RightUp    = 0x0010,
        // Specifies that the middle button was pressed.
        MiddleDown = 0x0020,
        // Specifies that the middle button was released.
        MiddleUp   = 0x0040,
        // Specifies that the x button was pressed.
        XDown      = 0x0080,
        // Specifies that the x button was released. 
        XUp        = 0x0100,
        // Specifies that the wheel was moved
        Wheel      = 0x0800,
        // Specifies that x, y are absolute, not relative
        Absolute   = 0x8000,
    };


    // Provides methods for sending mouse and keyboard input
    internal sealed class Input
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------
 
        #region Constructors

        // Static class - Private to prevent creation
        Input()
        {
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Inject pointer input into the system
        // x, y are in pixels. If Absolute flag used, are relative to desktop origin.
        internal static void SendMouseInput( double x, double y, int data, SendMouseInputFlags flags )
        {
            int intflags = (int) flags;

            if( ( intflags & (int)SendMouseInputFlags.Absolute ) != 0 )
            {
                int vscreenWidth = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CXVIRTUALSCREEN);
                int vscreenHeight = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_CYVIRTUALSCREEN);
                int vscreenLeft = UnsafeNativeMethods.GetSystemMetrics(NativeMethods.SM_XVIRTUALSCREEN);
                int vscreenTop = UnsafeNativeMethods.GetSystemMetrics( NativeMethods.SM_YVIRTUALSCREEN );

                // Absolute input requires that input is in 'normalized' coords - with the entire
                // desktop being (0,0)...(65535,65536). Need to convert input x,y coords to this
                // first.
                //
                // In this normalized world, any pixel on the screen corresponds to a block of values
                // of normalized coords - eg. on a 1024x768 screen,
                // y pixel 0 corresponds to range 0 to 85.333,
                // y pixel 1 corresponds to range 85.333 to 170.666,
                // y pixel 2 correpsonds to range 170.666 to 256 - and so on.
                // Doing basic scaling math - (x-top)*65536/Width - gets us the start of the range.
                // However, because int math is used, this can end up being rounded into the wrong
                // pixel. For example, if we wanted pixel 1, we'd get 85.333, but that comes out as
                // 85 as an int, which falls into pixel 0's range - and that's where the pointer goes.
                // To avoid this, we add on half-a-"screen pixel"'s worth of normalized coords - to
                // push us into the middle of any given pixel's range - that's the 65536/(Width*2)
                // part of the formula. So now pixel 1 maps to 85+42 = 127 - which is comfortably
                // in the middle of that pixel's block.
                // The key ting here is that unlike points in coordinate geometry, pixels take up
                // space, so are often better treated like rectangles - and if you want to target
                // a particular pixel, target its rectangle's midpoint, not its edge.
                x = ( ( x - vscreenLeft ) * 65536 ) / vscreenWidth + 65536 / ( vscreenWidth * 2 );
                y = ( ( y - vscreenTop ) * 65536 ) / vscreenHeight + 65536 / ( vscreenHeight * 2 );

                intflags |= NativeMethods.MOUSEEVENTF_VIRTUALDESK;
            }

            NativeMethods.INPUT mi = new NativeMethods.INPUT();
            mi.type = NativeMethods.INPUT_MOUSE;
            mi.union.mouseInput.dx = (int) x;
            mi.union.mouseInput.dy = (int)y;
            mi.union.mouseInput.mouseData = data;
            mi.union.mouseInput.dwFlags = intflags;
            mi.union.mouseInput.time = 0;
            mi.union.mouseInput.dwExtraInfo = new IntPtr( 0 );
            if( UnsafeNativeMethods.SendInput( 1, ref mi, Marshal.SizeOf(mi) ) == 0 )
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Inject keyboard input into the system
        internal static void SendKeyboardInput( Key key, bool press )
        {
            NativeMethods.INPUT ki = new NativeMethods.INPUT();
            ki.type = NativeMethods.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = (short) KeyInterop.VirtualKeyFromKey( key );
            ki.union.keyboardInput.wScan = (short)SafeNativeMethods.MapVirtualKey(ki.union.keyboardInput.wVk, 0);
            int dwFlags = 0;
            if( ki.union.keyboardInput.wScan > 0 )
                dwFlags |= NativeMethods.KEYEVENTF_SCANCODE;
            if( !press )
                dwFlags |= NativeMethods.KEYEVENTF_KEYUP;
            ki.union.keyboardInput.dwFlags = dwFlags;
            if (IsExtendedKey(ki.union.keyboardInput.wVk))
            {
                ki.union.keyboardInput.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }
            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr( 0 );
            if( UnsafeNativeMethods.SendInput( 1, ref ki, Marshal.SizeOf(ki) ) == 0 )
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        // Used internally by the HWND SetFocus code - it sends a hotkey to
        // itself - because it uses a VK that's not on the keyboard, it needs
        // to send the VK directly, not the scan code, which regular
        // SendKeyboardInput does.
        internal static void SendKeyboardInputVK(short vk, bool press)
        {
            NativeMethods.INPUT ki = new NativeMethods.INPUT();

            ki.type = NativeMethods.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = vk;
            ki.union.keyboardInput.wScan = 0;
            ki.union.keyboardInput.dwFlags = press ? 0 : NativeMethods.KEYEVENTF_KEYUP;
            if (IsExtendedKey(vk))
            {
                ki.union.keyboardInput.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }
            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr( 0 );

            Misc.SendInput(1, ref ki, Marshal.SizeOf(ki));
        }

        #endregion Internal Methods


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------
 
        #region Private Methods

        private static bool IsExtendedKey(short vk)
        {
            // From the SDK:
            // The extended-key flag indicates whether the keystroke message originated from one of
            // the additional keys on the enhanced keyboard. The extended keys consist of the ALT and
            // CTRL keys on the right-hand side of the keyboard; the INS, DEL, HOME, END, PAGE UP,
            // PAGE DOWN, and arrow keys in the clusters to the left of the numeric keypad; the NUM LOCK
            // key; the BREAK (CTRL+PAUSE) key; the PRINT SCRN key; and the divide (/) and ENTER keys in
            // the numeric keypad. The extended-key flag is set if the key is an extended key.
            //
            // - docs appear to be incorrect. Use of Spy++ indicates that break is not an extended key.
            // Also, menu key and windows keys also appear to be extended.
            return vk == unchecked((short)UnsafeNativeMethods.VK_RMENU) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_RCONTROL) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_NUMLOCK) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_INSERT) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_DELETE) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_HOME) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_END) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_PRIOR) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_NEXT) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_UP) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_DOWN) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_LEFT) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_RIGHT) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_APPS) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_RWIN) ||
                   vk == unchecked((short)UnsafeNativeMethods.VK_LWIN);
            // Note that there are no distinct values for the following keys:
            // numpad divide
            // numpad enter
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------
 
        #region Private Fields

        // Stateless object, has no private fields

        #endregion Private Fields
    }
}
