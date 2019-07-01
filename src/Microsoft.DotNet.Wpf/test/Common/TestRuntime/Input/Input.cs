// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Provides mouse and keyboard input functionality

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;
using System.Security.Permissions;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Test.Input.Win32;
using Microsoft.Test.RenderingVerification;

namespace Microsoft.Test.Input
{
    /// <summary>
    /// Flags for Input.SendMouseInput, indicate whether movent took place,
    /// or whether buttons were pressed or released.
    /// </summary>
    [Flags]
    [Serializable]
    public enum SendMouseInputFlags
    {
        /// <summary>Specifies that the pointer moved.</summary>
        Move = 0x0001,
        /// <summary>Specifies that the left button was pressed.</summary>
        LeftDown = 0x0002,
        /// <summary>Specifies that the left button was released.</summary>
        LeftUp = 0x0004,
        /// <summary>Specifies that the right button was pressed.</summary>
        RightDown = 0x0008,
        /// <summary>Specifies that the right button was released.</summary>
        RightUp = 0x0010,
        /// <summary>Specifies that the middle button was pressed.</summary>
        MiddleDown = 0x0020,
        /// <summary>Specifies that the middle button was released.</summary>
        MiddleUp = 0x0040,
        /// <summary>Specifies that the x button was pressed.</summary>
        XDown = 0x0080,
        /// <summary>Specifies that the x button was released. </summary>
        XUp = 0x0100,
        /// <summary>Specifies that the wheel was moved</summary>
        Wheel = 0x0800,
        /// <summary>Specifies that x, y are absolute, not relative</summary>
        Absolute = 0x8000,
    };

    /// <summary>
    /// POINT aka POINTAPI
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    struct POINT
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

    /// <summary>
    /// Provides methods for sending mouse and keyboard input
    /// The assert is delibrate, as numeorus tests (in AppSec in particular)
    /// require this functionality in a partial trust environment.
    /// </summary>
    [PermissionSet(SecurityAction.Assert, Name = "FullTrust")]
    public sealed class Input
    {

        #region Constructors

        // Static class - Private to prevent creation
        Input()
        {
        }

        #endregion Constructors

        #region Public Constants and Readonly Fields

        /// <summary>The first X mouse button</summary>
        public const int XButton1 = 0x01;

        /// <summary>The second X mouse button</summary>
        public const int XButton2 = 0x02;

        #endregion Public Constants and Readonly Fields

        #region Public Methods

        /// <summary>
        /// Inject pointer input into the system
        /// </summary>
        /// <param name="x">x coordinate of pointer, if Move flag specified</param>
        /// <param name="y">y coordinate of pointer, if Move flag specified</param>
        /// <param name="data">wheel movement, or mouse X button, depending on flags</param>
        /// <param name="flags">flags to indicate which type of input occurred - move, button press/release, wheel move, etc.</param>
        /// <remarks>x, y are in pixels. If Absolute flag used, are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void SendMouseInput(double x, double y, int data, SendMouseInputFlags flags)
        {
            SendMouseInputInternal(x, y, data, flags);
        }

        /// <summary>Converts the client-area coordinates of a specified point to screen coordinates.</summary>
        /// <param name="hwndFrom">Handle to the window whose client area is used for the conversion.</param>
        /// <param name="pt">POINT structure that contains the client coordinates to be converted.</param>
        /// <returns>true if the function succeeds, false otherwise.</returns>
        [DllImport("user32.dll", EntryPoint = "ClientToScreen", CharSet = CharSet.Auto)]
        private static extern bool ClientToScreen(IntPtr hwndFrom, [In, Out] ref Microsoft.Test.Input.POINT pt);

        /// <summary>
        /// Inject pointer input into the system
        /// </summary>
        /// <param name="x">x coordinate of pointer, if Move flag specified</param>
        /// <param name="y">y coordinate of pointer, if Move flag specified</param>
        /// <param name="data">wheel movement, or mouse X button, depending on flags</param>
        /// <param name="flags">flags to indicate which type of input occurred - move, button press/release, wheel move, etc.</param>
        /// <remarks>x, y are in pixels. If Absolute flag used, are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        internal static void SendMouseInputInternal(double x, double y, int data, SendMouseInputFlags flags)
        {
            //CASRemoval:AutomationPermission.Demand( AutomationPermissionFlag.Input );

            int intflags = (int)flags;

            if ((intflags & (int)SendMouseInputFlags.Absolute) != 0)
            {
                // this method of moving the cursor appears to be more reliable than the previous code,
                // at least in the case on Vista where the DPI was changed but we haven't rebooted yet.
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)(x + 0.5), (int)(y + 0.5));

                //now clear the absolute flag
                intflags &= ~(int)SendMouseInputFlags.Absolute;

                //and set our x and y to zero so we don't move
                x = y = 0;

                intflags |= UnsafeNativeMethods.MOUSEEVENTF_VIRTUALDESK;
            }

            UnsafeNativeMethods.INPUT mi = new UnsafeNativeMethods.INPUT();
            mi.type = UnsafeNativeMethods.INPUT_MOUSE;
            mi.union.mouseInput.dx = (int)x;
            mi.union.mouseInput.dy = (int)y;
            mi.union.mouseInput.mouseData = data;
            mi.union.mouseInput.dwFlags = intflags;
            mi.union.mouseInput.time = 0;
            mi.union.mouseInput.dwExtraInfo = new IntPtr(0);
            //Console.WriteLine("Sending");
            if (UnsafeNativeMethods.SendInput(1, ref mi, Marshal.SizeOf(mi)) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }


        /// <summary>
        /// Inject keyboard input into the system
        /// </summary>
        /// <param name="key">indicates the key pressed or released. Can be one of the constants defined in the Key enum</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void SendKeyboardInput(System.Windows.Input.Key key, bool press)
        {
            SendKeyboardInputInternal(key, press);
        }

        /// <summary>
        /// Inject keyboard input into the system
        /// </summary>
        /// <param name="key">indicates the key pressed or released. Can be one of the constants defined in the Key enum</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        internal static void SendKeyboardInputInternal(System.Windows.Input.Key key, bool press)
        {
            //CASRemoval:AutomationPermission.Demand( AutomationPermissionFlag.Input );

            UnsafeNativeMethods.INPUT ki = new UnsafeNativeMethods.INPUT();
            ki.type = UnsafeNativeMethods.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = (short)KeyInterop.VirtualKeyFromKey(key);
            ki.union.keyboardInput.wScan = (short)UnsafeNativeMethods.MapVirtualKey(ki.union.keyboardInput.wVk, 0);
            int dwFlags = 0;
            if (ki.union.keyboardInput.wScan > 0)
                dwFlags |= UnsafeNativeMethods.KEYEVENTF_SCANCODE;
            if (!press)
                dwFlags |= UnsafeNativeMethods.KEYEVENTF_KEYUP;
            ki.union.keyboardInput.dwFlags = dwFlags;
            if (IsExtendedKey(key))
            {
                ki.union.keyboardInput.dwFlags |= UnsafeNativeMethods.KEYEVENTF_EXTENDEDKEY;
            }
            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);
            if (UnsafeNativeMethods.SendInput(1, ref ki, Marshal.SizeOf(ki)) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Injects a unicode character as keyboard input into the system
        /// </summary>
        /// <param name="key">indicates the key to be pressed or released. Can be any unicode character</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void SendUnicodeKeyboardInput(char key, bool press)
        {
            SendUnicodeKeyboardInputInternal(key, press);
        }

        /// <summary>
        /// Injects a unicode character as keyboard input into the system
        /// </summary>
        /// <param name="key">indicates the key to be pressed or released. Can be any unicode character</param>
        /// <param name="press">true to inject a key press, false to inject a key release</param>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        internal static void SendUnicodeKeyboardInputInternal(char key, bool press)
        {
            //CASRemoval:AutomationPermission.Demand(AutomationPermissionFlag.Input);

            UnsafeNativeMethods.INPUT ki = new UnsafeNativeMethods.INPUT();

            ki.type = UnsafeNativeMethods.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = (short)0;
            ki.union.keyboardInput.wScan = (short)key;
            ki.union.keyboardInput.dwFlags = UnsafeNativeMethods.KEYEVENTF_UNICODE | (press ? 0 : UnsafeNativeMethods.KEYEVENTF_KEYUP);
            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);
            if (UnsafeNativeMethods.SendInput(1, ref ki, Marshal.SizeOf(ki)) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        /// <summary>
        /// Injects a string of Unicode characters using simulated keyboard input
        /// It should be noted that this overload just sends the whole string
        /// with no pauses, depending on the recieving applications input processing
        /// it may not be able to keep up with the speed, resulting in corruption or
        /// loss of the input data.
        /// </summary>
        /// <param name="data">The unicode string to be sent</param>
        public static void SendUnicodeString(string data)
        {
            SendUnicodeStringInternal(data);
        }

        /// <summary>
        /// Injects a string of Unicode characters using simulated keyboard input
        /// It should be noted that this overload just sends the whole string
        /// with no pauses, depending on the recieving applications input processing
        /// it may not be able to keep up with the speed, resulting in corruption or
        /// loss of the input data.
        /// </summary>
        /// <param name="data">The unicode string to be sent</param>
        internal static void SendUnicodeStringInternal(string data)
        {
            InternalSendUnicodeString(data, -1, 0);
        }

        /// <summary>
        /// Injects a string of Unicode characters using simulated keyboard input
        /// with user defined timing.
        /// </summary>
        /// <param name="data">The unicode string to be sent</param>
        /// <param name="sleepFrequency">How many characters to send between sleep calls</param>
        /// <param name="sleepLength">How long, in milliseconds, to sleep for at each sleep call</param>
        public static void SendUnicodeString(string data, int sleepFrequency, int sleepLength)
        {
            SendUnicodeStringInternal(data, sleepFrequency, sleepLength);
        }

        /// <summary>
        /// Injects a string of Unicode characters using simulated keyboard input
        /// with user defined timing.
        /// </summary>
        /// <param name="data">The unicode string to be sent</param>
        /// <param name="sleepFrequency">How many characters to send between sleep calls</param>
        /// <param name="sleepLength">How long, in milliseconds, to sleep for at each sleep call</param>
        internal static void SendUnicodeStringInternal(string data, int sleepFrequency, int sleepLength)
        {
            if (sleepFrequency < 1)
                throw new ArgumentOutOfRangeException("sleepFrequency");
            if (sleepLength < 0)
                throw new ArgumentOutOfRangeException("sleepLength");
            InternalSendUnicodeString(data, sleepFrequency, sleepLength);
        }

        /// <summary>
        /// Checks whether the specified key is currently up or down
        /// </summary>
        /// <param name="key">The Key to check</param>
        /// <returns>true if the specified key is currently down (being pressed), false if it is up</returns>
        public static bool GetAsyncKeyState(System.Windows.Input.Key key)
        {
            return GetAsyncKeyStateInternal(key);
        }


        /// <summary>
        /// Checks whether the specified key is currently up or down
        /// </summary>
        /// <param name="key">The Key to check</param>
        /// <returns>true if the specified key is currently down (being pressed), false if it is up</returns>
        internal static bool GetAsyncKeyStateInternal(System.Windows.Input.Key key)
        {
            int vKey = KeyInterop.VirtualKeyFromKey(key);
            int resp = UnsafeNativeMethods.GetAsyncKeyState(vKey);

            if (resp == 0)
                throw new InvalidOperationException("GetAsyncKeyStateFailed");

            return resp < 0;
        }

        /// <summary>
        /// Move the mouse to an element. 
        ///
        /// IMPORTANT!
        /// 
        /// Do not call MoveToAndClick (actually, do not make any calls to UIAutomationClient) 
        /// from the UI thread if your test is in the same process as the UI being tested.  
        /// UIAutomation calls back into Avalon core for UI information (e.g. ClickablePoint) 
        /// and must be on the UI thread to get this information.  If your test is making calls 
        /// from the UI thread you are going to deadlock...
        /// 
        /// </summary>
        /// <param name="target">The element that the mouse will move to</param>
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// </outside_see>
        public static void MoveTo(UIElement target)
        {
            if (target == null)
            {
                throw new ArgumentNullException("target");
            }

            // Get bounding rectangle for UIElement
            System.Drawing.Rectangle boundRect = ImageUtility.GetScreenBoundingRectangle(target);

            // Calculate center point of UIElement
            System.Windows.Point moveToPoint = new System.Windows.Point();

            moveToPoint.X = (boundRect.Right + boundRect.Left) / 2;
            moveToPoint.Y = (boundRect.Bottom + boundRect.Top) / 2;

            // Move mouse to the point and click
            MoveTo(moveToPoint);
        }

        /// <summary>
        /// Move the mouse to an element. 
        ///
        /// IMPORTANT!
        /// 
        /// Do not call MoveToAndClick (actually, do not make any calls to UIAutomationClient) 
        /// from the UI thread if your test is in the same process as the UI being tested.  
        /// UIAutomation calls back into Avalon core for UI information (e.g. ClickablePoint) 
        /// and must be on the UI thread to get this information.  If your test is making calls 
        /// from the UI thread you are going to deadlock...
        /// 
        /// </summary>
        /// <param name="el">The element that the mouse will move to</param>
        /// <exception cref="NoClickablePointException">If there is not clickable point for the element</exception>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveTo(AutomationElement el)
        {
            if (el == null)
            {
                throw new ArgumentNullException("el");
            }
            MoveTo(el.GetClickablePoint());
        }

        /// <summary>
        /// Move the mouse to an element and click on it.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="target">The UI element to click on</param>
        public static void MoveToAndClick(UIElement target)
        {
            // Get bounding rectangle for UIElement
            System.Drawing.Rectangle boundRect = ImageUtility.GetScreenBoundingRectangle(target);

            // Calculate center point of UIElement
            System.Windows.Point moveToPoint = new System.Windows.Point();

            moveToPoint.X = (boundRect.Right + boundRect.Left) / 2;
            moveToPoint.Y = (boundRect.Bottom + boundRect.Top) / 2;

            // Move mouse to the point and click
            MoveToAndClick(moveToPoint);
        }


        /// <summary>
        /// Move the mouse to a point. 
        /// </summary>
        /// <param name="pt">The point that the mouse will move to.</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveTo(Point pt)
        {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);
        }

        /// <summary>
        /// Move the mouse to an element and click on it.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        ///
        /// IMPORTANT!
        /// 
        /// Do not call MoveToAndClick (actually, do not make any calls to UIAutomationClient) 
        /// from the UI thread if your test is in the same process as the UI being tested.  
        /// UIAutomation calls back into Avalon core for UI information (e.g. ClickablePoint) 
        /// and must be on the UI thread to get this information.  If your test is making calls 
        /// from the UI thread you are going to deadlock...
        /// 
        /// </summary>
        /// <param name="el">The element to click on</param>
        /// <exception cref="NoClickablePointException">If there is not clickable point for the element</exception>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndClick(AutomationElement el)
        {
            if (el == null)
            {
                throw new ArgumentNullException("el");
            }
            MoveToAndClick(el.GetClickablePoint());
        }

        /// <summary>
        /// Move the mouse to a point and click.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The point to click at</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does not work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndClick(Point pt)
        {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);

            // send SendMouseInput works in term of the phisical mouse buttons, therefore we need
            // to check to see if the mouse buttons are swapped because this method need to use the primary
            // mouse button.
            if (SafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_SWAPBUTTON) == 0)
            {
                // the mouse buttons are not swaped the primary is the left
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.LeftDown | SendMouseInputFlags.Absolute);
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.LeftUp | SendMouseInputFlags.Absolute);
            }
            else
            {
                // the mouse buttons are swaped so click the right button which as actually the primary
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.RightDown | SendMouseInputFlags.Absolute);
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.RightUp | SendMouseInputFlags.Absolute);
            }
        }


        /// <summary>
        /// Move the mouse to a point and click.  The primary mouse button will be used
        /// this is usually the left button except if the mouse buttons are swaped.
        /// </summary>
        /// <param name="pt">The point to click at</param>
        /// <remarks>pt are in pixels that are relative to desktop origin.</remarks>
        /// 
        /// <outside_see conditional="false">
        /// This API does work inside the secure execution environment.
        /// <exception cref="System.Security.Permissions.SecurityPermission"/>
        /// </outside_see>
        public static void MoveToAndClickSafe(Point pt)
        {
            Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.Move | SendMouseInputFlags.Absolute);

            // send SendMouseInput works in term of the phisical mouse buttons, therefore we need
            // to check to see if the mouse buttons are swapped because this method need to use the primary
            // mouse button.
            if (SafeNativeMethods.GetSystemMetrics(SafeNativeMethods.SM_SWAPBUTTON) == 0)
            {
                // the mouse buttons are not swaped the primary is the left
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.LeftDown | SendMouseInputFlags.Absolute);
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.LeftUp | SendMouseInputFlags.Absolute);
            }
            else
            {
                // the mouse buttons are swaped so click the right button which as actually the primary
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.RightDown | SendMouseInputFlags.Absolute);
                Input.SendMouseInput(pt.X, pt.Y, 0, SendMouseInputFlags.RightUp | SendMouseInputFlags.Absolute);
            }
        }

        #endregion Public Methods

        #region Internal Methods


        // Used internally by the HWND SetFocus code - it sends a hotkey to
        // itself - because it uses a VK that's not on the keyboard, it needs
        // to send the VK directly, not the scan code, which regular
        // SendKeyboardInput does.
        // Note that this method is public, but this class is private, so
        // this is not externally visible.
        internal static void SendKeyboardInputVK(byte vk, bool press)
        {
            UnsafeNativeMethods.INPUT ki = new UnsafeNativeMethods.INPUT();
            ki.type = UnsafeNativeMethods.INPUT_KEYBOARD;
            ki.union.keyboardInput.wVk = vk;
            ki.union.keyboardInput.wScan = 0;
            ki.union.keyboardInput.dwFlags = press ? 0 : UnsafeNativeMethods.KEYEVENTF_KEYUP;
            ki.union.keyboardInput.time = 0;
            ki.union.keyboardInput.dwExtraInfo = new IntPtr(0);
            if (UnsafeNativeMethods.SendInput(1, ref ki, Marshal.SizeOf(ki)) == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }


        internal static bool IsExtendedKey(System.Windows.Input.Key key)
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
            return key == System.Windows.Input.Key.RightAlt
                || key == System.Windows.Input.Key.RightCtrl
                || key == System.Windows.Input.Key.NumLock
                || key == System.Windows.Input.Key.Insert
                || key == System.Windows.Input.Key.Delete
                || key == System.Windows.Input.Key.Home
                || key == System.Windows.Input.Key.End
                || key == System.Windows.Input.Key.Prior
                || key == System.Windows.Input.Key.Next
                || key == System.Windows.Input.Key.Up
                || key == System.Windows.Input.Key.Down
                || key == System.Windows.Input.Key.Left
                || key == System.Windows.Input.Key.Right
                || key == System.Windows.Input.Key.Apps
                || key == System.Windows.Input.Key.RWin
                || key == System.Windows.Input.Key.LWin;

            // Note that there are no distinct values for the following keys:
            // numpad divide
            // numpad enter
        }

        #endregion Internal Methods

        #region Private Methods

        // Injects a string of Unicode characters using simulated keyboard input
        // with user defined timing
        // <param name="data">The unicode string to be sent</param>
        // <param name="sleepFrequency">How many characters to send between sleep calls
        // A sleepFrequency of -1 means to never sleep</param>
        // <param name="sleepLength">How long, in milliseconds, to sleep for at each sleep call</param>
        private static void InternalSendUnicodeString(string data, int sleepFrequency, int sleepLength)
        {
            char[] chardata = data.ToCharArray();
            int counter = -1;

            foreach (char c in chardata)
            {
                // Every sleepFrequency characters, sleep for sleepLength ms to avoid overflowing the input buffer.
                if (++counter == sleepFrequency)
                {
                    counter = 0;
                    System.Threading.Thread.Sleep(sleepLength);
                }

                SendUnicodeKeyboardInput(c, true);
                SendUnicodeKeyboardInput(c, false);
            }
        }

        #endregion Private Methods

    }
}
