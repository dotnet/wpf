// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Test.Input
{
    /// <summary>
    /// Exposes a simple interface to common keyboard operations, allowing the user to simulate keyboard input.
    /// </summary>
    /// <example>
    /// The following code types "Hello, world!" with the specified casing,
    /// and then types "hello, capitalized world!" which will be in all caps because
    /// the left shift key is being held down.
    /// <code>
    /// Keyboard.Type("Hello, world!");
    /// Keyboard.Type(Key.Enter);
    /// Keyboard.Press(Key.LeftShift);
    /// Keyboard.Type("hello, capitalized world!");
    /// Keyboard.Release(Key.LeftShift);
    /// </code>
    /// </example>
    public class Keyboard
    {
        static Keyboard()
        {
            KeyBoardKeys = new Dictionary<Key, KeySpec>();
            KeyBoardKeys.Add(Key.Cancel, new KeySpec((ushort)Key.Cancel, false, "cancel"));
            KeyBoardKeys.Add(Key.Back, new KeySpec((ushort)Key.Back, false, "backspace"));
            KeyBoardKeys.Add(Key.Tab, new KeySpec((ushort)Key.Tab, false, "tab"));
            KeyBoardKeys.Add(Key.Clear, new KeySpec((ushort)Key.Clear, false, "clear"));
            KeyBoardKeys.Add(Key.Return, new KeySpec((ushort)Key.Return, false, "return"));            
            KeyBoardKeys.Add(Key.Shift, new KeySpec((ushort)Key.Shift, false, "shift"));
            KeyBoardKeys.Add(Key.Ctrl, new KeySpec((ushort)Key.Ctrl, false, "ctrl"));
            KeyBoardKeys.Add(Key.Alt, new KeySpec((ushort)Key.Alt, false, "alt"));
            KeyBoardKeys.Add(Key.Pause, new KeySpec((ushort)Key.Pause, false, "pause"));
            KeyBoardKeys.Add(Key.Capital, new KeySpec((ushort)Key.Capital, false, "capital"));
            KeyBoardKeys.Add(Key.KanaMode, new KeySpec((ushort)Key.KanaMode, false, "kanamode"));
            KeyBoardKeys.Add(Key.JunjaMode, new KeySpec((ushort)Key.JunjaMode, false, "junjamode"));
            KeyBoardKeys.Add(Key.FinalMode, new KeySpec((ushort)Key.FinalMode, false, "finalmode"));
            KeyBoardKeys.Add(Key.HanjaMode, new KeySpec((ushort)Key.HanjaMode, false, "hanjamode"));           
            KeyBoardKeys.Add(Key.Escape, new KeySpec((ushort)Key.Escape, false, "esc"));
            KeyBoardKeys.Add(Key.ImeConvert, new KeySpec((ushort)Key.ImeConvert, false, "imeconvert"));
            KeyBoardKeys.Add(Key.ImeNonConvert, new KeySpec((ushort)Key.ImeNonConvert, false, "imenonconvert"));
            KeyBoardKeys.Add(Key.ImeAccept, new KeySpec((ushort)Key.ImeAccept, false, "imeaccept"));
            KeyBoardKeys.Add(Key.ImeModeChange, new KeySpec((ushort)Key.ImeAccept, false, "imemodechange"));
            KeyBoardKeys.Add(Key.Space, new KeySpec((ushort)Key.Space, false, " "));
            KeyBoardKeys.Add(Key.Prior, new KeySpec((ushort)Key.Prior, true, "prior"));            
            KeyBoardKeys.Add(Key.Next, new KeySpec((ushort)Key.Next, true, "next"));
            KeyBoardKeys.Add(Key.End, new KeySpec((ushort)Key.End, true, "end"));
            KeyBoardKeys.Add(Key.Home, new KeySpec((ushort)Key.Home, true, "home"));
            KeyBoardKeys.Add(Key.Left, new KeySpec((ushort)Key.Left, true, "left"));
            KeyBoardKeys.Add(Key.Up, new KeySpec((ushort)Key.Up, true, "up"));
            KeyBoardKeys.Add(Key.Right, new KeySpec((ushort)Key.Right, true, "right"));
            KeyBoardKeys.Add(Key.Down, new KeySpec((ushort)Key.Down, true, "down"));
            KeyBoardKeys.Add(Key.Select, new KeySpec((ushort)Key.Select, false, "select"));
            KeyBoardKeys.Add(Key.Print, new KeySpec((ushort)Key.Print, false, "print"));
            KeyBoardKeys.Add(Key.Execute, new KeySpec((ushort)Key.Execute, false, "execute"));
            KeyBoardKeys.Add(Key.Snapshot, new KeySpec((ushort)Key.Snapshot, true, "snapshot"));           
            KeyBoardKeys.Add(Key.Insert, new KeySpec((ushort)Key.Insert, true, "insert"));
            KeyBoardKeys.Add(Key.Delete, new KeySpec((ushort)Key.Delete, true, "delete"));
            KeyBoardKeys.Add(Key.Help, new KeySpec((ushort)Key.Help, false, "help"));

            KeyBoardKeys.Add(Key.D0, new KeySpec((ushort)Key.D0, false, "0"));
            KeyBoardKeys.Add(Key.D1, new KeySpec((ushort)Key.D1, false, "1"));
            KeyBoardKeys.Add(Key.D2, new KeySpec((ushort)Key.D2, false, "2"));
            KeyBoardKeys.Add(Key.D3, new KeySpec((ushort)Key.D3, false, "3"));
            KeyBoardKeys.Add(Key.D4, new KeySpec((ushort)Key.D4, false, "4"));
            KeyBoardKeys.Add(Key.D5, new KeySpec((ushort)Key.D5, false, "5"));
            KeyBoardKeys.Add(Key.D6, new KeySpec((ushort)Key.D6, false, "6"));
            KeyBoardKeys.Add(Key.D7, new KeySpec((ushort)Key.D7, false, "7"));
            KeyBoardKeys.Add(Key.D8, new KeySpec((ushort)Key.D8, false, "8"));
            KeyBoardKeys.Add(Key.D9, new KeySpec((ushort)Key.D9, false, "9"));

            KeyBoardKeys.Add(Key.A, new KeySpec((ushort)Key.A, false, "a"));
            KeyBoardKeys.Add(Key.B, new KeySpec((ushort)Key.B, false, "b"));
            KeyBoardKeys.Add(Key.C, new KeySpec((ushort)Key.C, false, "c"));
            KeyBoardKeys.Add(Key.D, new KeySpec((ushort)Key.D, false, "d"));
            KeyBoardKeys.Add(Key.E, new KeySpec((ushort)Key.E, false, "e"));
            KeyBoardKeys.Add(Key.F, new KeySpec((ushort)Key.F, false, "f"));
            KeyBoardKeys.Add(Key.G, new KeySpec((ushort)Key.G, false, "g"));
            KeyBoardKeys.Add(Key.H, new KeySpec((ushort)Key.H, false, "h"));
            KeyBoardKeys.Add(Key.I, new KeySpec((ushort)Key.I, false, "i"));
            KeyBoardKeys.Add(Key.J, new KeySpec((ushort)Key.J, false, "j"));
            KeyBoardKeys.Add(Key.K, new KeySpec((ushort)Key.K, false, "k"));
            KeyBoardKeys.Add(Key.L, new KeySpec((ushort)Key.L, false, "l"));
            KeyBoardKeys.Add(Key.M, new KeySpec((ushort)Key.M, false, "m"));
            KeyBoardKeys.Add(Key.N, new KeySpec((ushort)Key.N, false, "n"));
            KeyBoardKeys.Add(Key.O, new KeySpec((ushort)Key.O, false, "o"));
            KeyBoardKeys.Add(Key.P, new KeySpec((ushort)Key.P, false, "p"));
            KeyBoardKeys.Add(Key.Q, new KeySpec((ushort)Key.Q, false, "q"));
            KeyBoardKeys.Add(Key.R, new KeySpec((ushort)Key.R, false, "r"));
            KeyBoardKeys.Add(Key.S, new KeySpec((ushort)Key.S, false, "s"));
            KeyBoardKeys.Add(Key.T, new KeySpec((ushort)Key.T, false, "t"));
            KeyBoardKeys.Add(Key.U, new KeySpec((ushort)Key.U, false, "u"));
            KeyBoardKeys.Add(Key.V, new KeySpec((ushort)Key.V, false, "v"));
            KeyBoardKeys.Add(Key.W, new KeySpec((ushort)Key.W, false, "w"));
            KeyBoardKeys.Add(Key.X, new KeySpec((ushort)Key.X, false, "x"));
            KeyBoardKeys.Add(Key.Y, new KeySpec((ushort)Key.Y, false, "y"));
            KeyBoardKeys.Add(Key.Z, new KeySpec((ushort)Key.Z, false, "z"));

            KeyBoardKeys.Add(Key.LWin, new KeySpec((ushort)Key.LWin, true, "lwin"));
            KeyBoardKeys.Add(Key.RWin, new KeySpec((ushort)Key.RWin, true, "rwin"));
            KeyBoardKeys.Add(Key.Apps, new KeySpec((ushort)Key.Apps, true, "apps"));
            KeyBoardKeys.Add(Key.Sleep, new KeySpec((ushort)Key.Sleep, false, "sleep"));

            KeyBoardKeys.Add(Key.NumPad0, new KeySpec((ushort)Key.NumPad0, false, "n0"));
            KeyBoardKeys.Add(Key.NumPad1, new KeySpec((ushort)Key.NumPad1, false, "n1"));
            KeyBoardKeys.Add(Key.NumPad2, new KeySpec((ushort)Key.NumPad2, false, "n2"));
            KeyBoardKeys.Add(Key.NumPad3, new KeySpec((ushort)Key.NumPad3, false, "n3"));
            KeyBoardKeys.Add(Key.NumPad4, new KeySpec((ushort)Key.NumPad4, false, "n4"));
            KeyBoardKeys.Add(Key.NumPad5, new KeySpec((ushort)Key.NumPad5, false, "n5"));
            KeyBoardKeys.Add(Key.NumPad6, new KeySpec((ushort)Key.NumPad6, false, "n6"));
            KeyBoardKeys.Add(Key.NumPad7, new KeySpec((ushort)Key.NumPad7, false, "n7"));
            KeyBoardKeys.Add(Key.NumPad8, new KeySpec((ushort)Key.NumPad8, false, "n8"));
            KeyBoardKeys.Add(Key.NumPad9, new KeySpec((ushort)Key.NumPad9, false, "n9"));

            KeyBoardKeys.Add(Key.Multiply, new KeySpec((ushort)Key.Multiply, false, "*"));
            KeyBoardKeys.Add(Key.Add, new KeySpec((ushort)Key.Add, false, "+"));
            KeyBoardKeys.Add(Key.Separator, new KeySpec((ushort)Key.Separator, false, "separator"));
            KeyBoardKeys.Add(Key.Subtract, new KeySpec((ushort)Key.Subtract, false, "-"));
            KeyBoardKeys.Add(Key.Decimal, new KeySpec((ushort)Key.Decimal, false, "decimal"));
            KeyBoardKeys.Add(Key.Divide, new KeySpec((ushort)Key.Divide, true, "/"));

            KeyBoardKeys.Add(Key.F1, new KeySpec((ushort)Key.F1, false, "f1"));
            KeyBoardKeys.Add(Key.F2, new KeySpec((ushort)Key.F2, false, "f2"));
            KeyBoardKeys.Add(Key.F3, new KeySpec((ushort)Key.F3, false, "f3"));
            KeyBoardKeys.Add(Key.F4, new KeySpec((ushort)Key.F4, false, "f4"));
            KeyBoardKeys.Add(Key.F5, new KeySpec((ushort)Key.F5, false, "f5"));
            KeyBoardKeys.Add(Key.F6, new KeySpec((ushort)Key.F6, false, "f6"));
            KeyBoardKeys.Add(Key.F7, new KeySpec((ushort)Key.F7, false, "f7"));
            KeyBoardKeys.Add(Key.F8, new KeySpec((ushort)Key.F8, false, "f8"));
            KeyBoardKeys.Add(Key.F9, new KeySpec((ushort)Key.F9, false, "f9"));
            KeyBoardKeys.Add(Key.F10, new KeySpec((ushort)Key.F10, false, "f10"));
            KeyBoardKeys.Add(Key.F11, new KeySpec((ushort)Key.F11, false, "f11"));
            KeyBoardKeys.Add(Key.F12, new KeySpec((ushort)Key.F12, false, "f12"));
            KeyBoardKeys.Add(Key.F13, new KeySpec((ushort)Key.F13, false, "f13"));
            KeyBoardKeys.Add(Key.F14, new KeySpec((ushort)Key.F14, false, "f14"));
            KeyBoardKeys.Add(Key.F15, new KeySpec((ushort)Key.F15, false, "f15"));
            KeyBoardKeys.Add(Key.F16, new KeySpec((ushort)Key.F16, false, "f16"));
            KeyBoardKeys.Add(Key.F17, new KeySpec((ushort)Key.F17, false, "f17"));
            KeyBoardKeys.Add(Key.F18, new KeySpec((ushort)Key.F18, false, "f18"));
            KeyBoardKeys.Add(Key.F19, new KeySpec((ushort)Key.F19, false, "f19"));
            KeyBoardKeys.Add(Key.F20, new KeySpec((ushort)Key.F20, false, "f20"));
            KeyBoardKeys.Add(Key.F21, new KeySpec((ushort)Key.F21, false, "f21"));
            KeyBoardKeys.Add(Key.F22, new KeySpec((ushort)Key.F22, false, "f22"));
            KeyBoardKeys.Add(Key.F23, new KeySpec((ushort)Key.F23, false, "f23"));
            KeyBoardKeys.Add(Key.F24, new KeySpec((ushort)Key.F24, false, "f24"));

            KeyBoardKeys.Add(Key.NumLock, new KeySpec((ushort)Key.NumLock, true, "numlock"));
            KeyBoardKeys.Add(Key.Scroll, new KeySpec((ushort)Key.Scroll, false, "scroll"));
            KeyBoardKeys.Add(Key.LeftShift, new KeySpec((ushort)Key.LeftShift, false, "leftshift"));
            KeyBoardKeys.Add(Key.RightShift, new KeySpec((ushort)Key.RightShift, false, "rightshift"));
            KeyBoardKeys.Add(Key.LeftCtrl, new KeySpec((ushort)Key.LeftCtrl, false, "leftctrl"));
            KeyBoardKeys.Add(Key.RightCtrl, new KeySpec((ushort)Key.RightCtrl, true, "rightctrl"));
            KeyBoardKeys.Add(Key.LeftAlt, new KeySpec((ushort)Key.LeftAlt, false, "leftalt"));
            KeyBoardKeys.Add(Key.RightAlt, new KeySpec((ushort)Key.RightAlt, true, "rightalt"));

            KeyBoardKeys.Add(Key.BrowserBack, new KeySpec((ushort)Key.BrowserBack, false, "browserback"));
            KeyBoardKeys.Add(Key.BrowserForward, new KeySpec((ushort)Key.BrowserForward, false, "browserforward"));
            KeyBoardKeys.Add(Key.BrowserRefresh, new KeySpec((ushort)Key.BrowserRefresh, false, "browserrefresh"));
            KeyBoardKeys.Add(Key.BrowserStop, new KeySpec((ushort)Key.BrowserStop, false, "browserstop"));
            KeyBoardKeys.Add(Key.BrowserSearch, new KeySpec((ushort)Key.BrowserSearch, false, "browsersearch"));
            KeyBoardKeys.Add(Key.BrowserFavorites, new KeySpec((ushort)Key.BrowserFavorites, false, "BrowserFavorites"));
            KeyBoardKeys.Add(Key.BrowserHome, new KeySpec((ushort)Key.BrowserHome, false, "BrowserHome"));

            KeyBoardKeys.Add(Key.VolumeMute, new KeySpec((ushort)Key.VolumeMute, false, "VolumeMute"));
            KeyBoardKeys.Add(Key.VolumeDown, new KeySpec((ushort)Key.VolumeDown, false, "VolumeDown"));
            KeyBoardKeys.Add(Key.VolumeUp, new KeySpec((ushort)Key.VolumeUp, false, "VolumeUp"));
            KeyBoardKeys.Add(Key.MediaNextTrack, new KeySpec((ushort)Key.MediaNextTrack, false, "MediaNextTrack"));
            KeyBoardKeys.Add(Key.MediaPreviousTrack, new KeySpec((ushort)Key.MediaPreviousTrack, false, "MediaPreviousTrack"));
            KeyBoardKeys.Add(Key.MediaStop, new KeySpec((ushort)Key.MediaStop, false, "MediaStop"));
            KeyBoardKeys.Add(Key.MediaPlayPause, new KeySpec((ushort)Key.MediaPlayPause, false, "MediaPlayPause"));
            KeyBoardKeys.Add(Key.LaunchMail, new KeySpec((ushort)Key.LaunchMail, false, "LaunchMail"));
            KeyBoardKeys.Add(Key.SelectMedia, new KeySpec((ushort)Key.SelectMedia, false, "SelectMedia"));
            KeyBoardKeys.Add(Key.LaunchApplication1, new KeySpec((ushort)Key.LaunchApplication1, false, "LaunchApplication1"));
            KeyBoardKeys.Add(Key.LaunchApplication2, new KeySpec((ushort)Key.LaunchApplication2, false, "LaunchApplication2"));
            
            KeyBoardKeys.Add(Key.Oem1, new KeySpec((ushort)Key.Oem1, false, ";"));           
            KeyBoardKeys.Add(Key.OemPlus, new KeySpec((ushort)Key.OemPlus, false, "+"));
            KeyBoardKeys.Add(Key.OemComma, new KeySpec((ushort)Key.OemComma, false, ","));
            KeyBoardKeys.Add(Key.OemMinus, new KeySpec((ushort)Key.OemMinus, false, "-"));
            KeyBoardKeys.Add(Key.OemPeriod, new KeySpec((ushort)Key.OemPeriod, false, "."));
            KeyBoardKeys.Add(Key.Oem2, new KeySpec((ushort)Key.Oem2, false, "?"));
            KeyBoardKeys.Add(Key.Oem3, new KeySpec((ushort)Key.Oem3, false, "~"));
            KeyBoardKeys.Add(Key.AbntC1, new KeySpec((ushort)Key.AbntC1, false, "AbntC1"));
            KeyBoardKeys.Add(Key.AbntC2, new KeySpec((ushort)Key.AbntC2, false, "AbntC2"));
            KeyBoardKeys.Add(Key.Oem4, new KeySpec((ushort)Key.Oem4, false, "["));
            KeyBoardKeys.Add(Key.Oem5, new KeySpec((ushort)Key.Oem5, false, "|"));
            KeyBoardKeys.Add(Key.Oem6, new KeySpec((ushort)Key.Oem6, false, "]"));
            KeyBoardKeys.Add(Key.Oem7, new KeySpec((ushort)Key.Oem7, false, "\""));
            KeyBoardKeys.Add(Key.Oem8, new KeySpec((ushort)Key.Oem8, false, "Oem8"));
            KeyBoardKeys.Add(Key.Oem102, new KeySpec((ushort)Key.Oem102, false, "\\"));
            KeyBoardKeys.Add(Key.ImeProcessed, new KeySpec((ushort)Key.ImeProcessed, false, "ImeProcessed"));
            KeyBoardKeys.Add(Key.OemAttn, new KeySpec((ushort)Key.OemAttn, false, "OemAttn"));
            KeyBoardKeys.Add(Key.OemFinish, new KeySpec((ushort)Key.OemFinish, false, "OemFinish"));
            KeyBoardKeys.Add(Key.OemCopy, new KeySpec((ushort)Key.OemCopy, false, "OemCopy"));
            KeyBoardKeys.Add(Key.OemAuto, new KeySpec((ushort)Key.OemAuto, false, "OemAuto"));
            KeyBoardKeys.Add(Key.OemEnlw, new KeySpec((ushort)Key.OemEnlw, false, "OemEnlw"));          
            KeyBoardKeys.Add(Key.OemBackTab, new KeySpec((ushort)Key.OemBackTab, false, "OemBackTab"));
            KeyBoardKeys.Add(Key.Attn, new KeySpec((ushort)Key.Attn, false, "Attn"));
            KeyBoardKeys.Add(Key.CrSel, new KeySpec((ushort)Key.CrSel, false, "CrSel"));
            KeyBoardKeys.Add(Key.ExSel, new KeySpec((ushort)Key.ExSel, false, "ExSel"));
            KeyBoardKeys.Add(Key.EraseEof, new KeySpec((ushort)Key.EraseEof, false, "EraseEof"));
            KeyBoardKeys.Add(Key.Play, new KeySpec((ushort)Key.Play, false, "Play"));
            KeyBoardKeys.Add(Key.Zoom, new KeySpec((ushort)Key.Zoom, false, "Zoom"));
            KeyBoardKeys.Add(Key.NoName, new KeySpec((ushort)Key.NoName, false, "NoName"));
            KeyBoardKeys.Add(Key.Pa1, new KeySpec((ushort)Key.Pa1, false, "Pa1"));
            KeyBoardKeys.Add(Key.OemClear, new KeySpec((ushort)Key.OemClear, false, "OemClear"));
            KeyBoardKeys.Add(Key.DeadCharProcessed, new KeySpec((ushort)Key.DeadCharProcessed, false, "DeadCharProcessed"));
        }

        #region Public Methods

        /// <summary>
        /// Presses down a key.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public static void Press(Key key)
        {
            var keySpec = GetKeySpecFromKey(key);
            SendKeyboardKey(keySpec.KeyCode, true, keySpec.IsExtended, false);
        }

        /// <summary>
        /// Releases a key.
        /// </summary>
        /// <param name="key">The key to release.</param>
        public static void Release(Key key)
        {
            var keySpec = GetKeySpecFromKey(key);
            SendKeyboardKey(keySpec.KeyCode, false, keySpec.IsExtended, false);
        }

        /// <summary>
        /// Resets the system keyboard to a clean state.
        /// </summary>
        public static void Reset()
        {
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if ((GetKeyState(key) & KeyStates.Down) > 0)
                {
                    Release(key);
                }
            }
        }

        /// <summary>
        /// Performs a press-and-release operation for the specified key, which is effectively equivallent to typing.
        /// </summary>
        /// <param name="key">The key to press.</param>
        public static void Type(Key key)
        {
            Press(key);
            Release(key);
        }

        
        /// <summary>
        /// Types the specified text.
        /// </summary>
        /// <remarks>
        /// Note that a combination of a combination of Key.Shift or Key.Capital and a Unicode point above 0xFE
        /// is not considered valid and will not result in the Unicode point being types without 
        /// applying of the modifier key.
        /// </remarks>
        /// <param name="text">The text to type.</param>
        public static void Type(string text)
        {
            foreach (char c in text)
            {
                // If code point is bigger than 8 bits, we are going for Unicode approach by setting wVk to 0.
                if (c > 0xFE)
                {
                    SendKeyboardKey(c, true, false, true);
                    SendKeyboardKey(c, false, false, true);
                }
                else
                {
                    // We get the vKey value for the character via a Win32 API. We then use bit masks to pull the
                    // upper and lower bytes to get the shift state and key information. We then use WPF KeyInterop
                    // to go from the vKey key info into a System.Windows.Input.Key data structure. This work is
                    // necessary because Key doesn't distinguish between upper and lower case, so we have to wrap
                    // the key type inside a shift press/release if necessary.
                    int vKeyValue = NativeMethods.VkKeyScan(c);
                    bool keyIsShifted = (vKeyValue & NativeMethods.VKeyShiftMask) == NativeMethods.VKeyShiftMask;
                    Key key = (Key)(vKeyValue & NativeMethods.VKeyCharMask);

                    if (keyIsShifted)
                    {
                        Type(key, new Key[] { Key.Shift });
                    }
                    else
                    {
                        Type(key);
                    }
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private static KeySpec GetKeySpecFromKey(Key key)
        {
            KeySpec resultKey;
            if (!KeyBoardKeys.TryGetValue(key, out resultKey))
            {
                resultKey = new KeySpec();
            }

            return resultKey;
        }

        private static void Type(Key key, Key[] modifierKeys)
        {
            foreach (Key modiferKey in modifierKeys)
            {
                Press(modiferKey);
            }

            Type(key);

            foreach (Key modifierKey in modifierKeys.Reverse())
            {
                Release(modifierKey);
            }
        }

        private static void SendKeyboardKey(ushort key, bool isKeyDown, bool isExtended, bool isUnicode)
        {
            var input = new NativeMethods.INPUT();
            input.Type = NativeMethods.INPUT_KEYBOARD;
            if (!isKeyDown)
            {
                input.Data.Keyboard.dwFlags |= NativeMethods.KEYEVENTF_KEYUP;
            }

            if (isUnicode)
            {
                input.Data.Keyboard.dwFlags |= NativeMethods.KEYEVENTF_UNICODE;
                input.Data.Keyboard.wScan = key;
                input.Data.Keyboard.wVk = 0;
            }
            else
            {
                input.Data.Keyboard.wScan = 0;
                input.Data.Keyboard.wVk = key;
            }

            if (isExtended)
            {
                input.Data.Keyboard.dwFlags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;
            }

            input.Data.Keyboard.time = 0;
            input.Data.Keyboard.dwExtraInfo = IntPtr.Zero;

            NativeMethods.SendInput(1, new NativeMethods.INPUT[] { input }, Marshal.SizeOf(input));
            Thread.Sleep(100);
        }

        private static KeyStates GetKeyState(Key key)
        {
            var keyStates = KeyStates.None;
            var nativeKeyState = NativeMethods.GetKeyState((int)key);

            if ((nativeKeyState & 0x00008000) == 0x00008000)
            {
                keyStates |= KeyStates.Down;
            }

            if ((nativeKeyState & 0x00000001) == 0x00000001)
            {
                keyStates |= KeyStates.Toggled;
            }

            return keyStates;
        }

        #endregion Private Methods

        #region Private Data

        private struct KeySpec
        {
            public ushort KeyCode;
            public bool IsExtended;
            public string Name;

            public KeySpec(ushort keyCode, bool isExtended, string name)
            {
                this.KeyCode = keyCode;
                this.IsExtended = isExtended;
                this.Name = name;
            }
        }

        private enum KeyStates
        {
            None = 0,
            Down = 1,
            Toggled = 2
        }

        private static Dictionary<Key, KeySpec> KeyBoardKeys = null;

        #endregion Private Data
    }
}
