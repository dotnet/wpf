// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


 using MS.Win32;

using System;

namespace System.Windows.Input 
{
    /// <summary>
    ///     Provides static methods to convert between Win32 VirtualKeys
    ///     and our Key enum.
    /// </summary>
    public static class KeyInterop
    {
        /// <summary>
        ///     Convert a Win32 VirtualKey into our Key enum.
        /// </summary>
        public static Key KeyFromVirtualKey(int virtualKey)
        {
            Key key = Key.None;
            
            switch(virtualKey)
            {
                case NativeMethods.VK_CANCEL:
                    key = Key.Cancel;
                    break;
                    
                case NativeMethods.VK_BACK:
                    key = Key.Back;
                    break;
                    
                case NativeMethods.VK_TAB:
                    key = Key.Tab;
                    break;
                    
                case NativeMethods.VK_CLEAR:
                    key = Key.Clear;
                    break;
                    
                case NativeMethods.VK_RETURN:
                    key = Key.Return;
                    break;
                    
                case NativeMethods.VK_PAUSE:
                    key = Key.Pause;
                    break;
                    
                case NativeMethods.VK_CAPITAL:
                    key = Key.Capital;
                    break;
                    
                case NativeMethods.VK_KANA:
                    key = Key.KanaMode;
                    break;
                    
                case NativeMethods.VK_JUNJA:
                    key = Key.JunjaMode;
                    break;
                    
                case NativeMethods.VK_FINAL:
                    key = Key.FinalMode;
                    break;
                    
                case NativeMethods.VK_KANJI:
                    key = Key.KanjiMode;
                    break;
                    
                case NativeMethods.VK_ESCAPE:
                    key = Key.Escape;
                    break;
                    
                case NativeMethods.VK_CONVERT:
                    key = Key.ImeConvert;
                    break;
                    
                case NativeMethods.VK_NONCONVERT:
                    key = Key.ImeNonConvert;
                    break;
                    
                case NativeMethods.VK_ACCEPT:
                    key = Key.ImeAccept;
                    break;
                    
                case NativeMethods.VK_MODECHANGE:
                    key = Key.ImeModeChange;
                    break;
                    
                case NativeMethods.VK_SPACE:
                    key = Key.Space;
                    break;
                    
                case NativeMethods.VK_PRIOR:
                    key = Key.Prior;
                    break;
                    
                case NativeMethods.VK_NEXT:
                    key = Key.Next;
                    break;
                    
                case NativeMethods.VK_END:
                    key = Key.End;
                    break;
                    
                case NativeMethods.VK_HOME:
                    key = Key.Home;
                    break;
                    
                case NativeMethods.VK_LEFT:
                    key = Key.Left;
                    break;
                    
                case NativeMethods.VK_UP:
                    key = Key.Up;
                    break;
                    
                case NativeMethods.VK_RIGHT:
                    key = Key.Right;
                    break;
                    
                case NativeMethods.VK_DOWN:
                    key = Key.Down;
                    break;
                    
                case NativeMethods.VK_SELECT:
                    key = Key.Select;
                    break;
                    
                case NativeMethods.VK_PRINT:
                    key = Key.Print;
                    break;
                    
                case NativeMethods.VK_EXECUTE:
                    key = Key.Execute;
                    break;
                    
                case NativeMethods.VK_SNAPSHOT:
                    key = Key.Snapshot;
                    break;
                    
                case NativeMethods.VK_INSERT:
                    key = Key.Insert;
                    break;
                    
                case NativeMethods.VK_DELETE:
                    key = Key.Delete;
                    break;
                    
                case NativeMethods.VK_HELP:
                    key = Key.Help;
                    break;
                    
                case NativeMethods.VK_0:
                    key = Key.D0;
                    break;
                    
                case NativeMethods.VK_1:
                    key = Key.D1;
                    break;
                    
                case NativeMethods.VK_2:
                    key = Key.D2;
                    break;
                    
                case NativeMethods.VK_3:
                    key = Key.D3;
                    break;
                    
                case NativeMethods.VK_4:
                    key = Key.D4;
                    break;
                    
                case NativeMethods.VK_5:
                    key = Key.D5;
                    break;
                    
                case NativeMethods.VK_6:
                    key = Key.D6;
                    break;
                    
                case NativeMethods.VK_7:
                    key = Key.D7;
                    break;
                    
                case NativeMethods.VK_8:
                    key = Key.D8;
                    break;
                    
                case NativeMethods.VK_9:
                    key = Key.D9;
                    break;
                    
                case NativeMethods.VK_A:
                    key = Key.A;
                    break;
                    
                case NativeMethods.VK_B:
                    key = Key.B;
                    break;
                    
                case NativeMethods.VK_C:
                    key = Key.C;
                    break;
                    
                case NativeMethods.VK_D:
                    key = Key.D;
                    break;
                    
                case NativeMethods.VK_E:
                    key = Key.E;
                    break;
                    
                case NativeMethods.VK_F:
                    key = Key.F;
                    break;
                    
                case NativeMethods.VK_G:
                    key = Key.G;
                    break;
                    
                case NativeMethods.VK_H:
                    key = Key.H;
                    break;
                    
                case NativeMethods.VK_I:
                    key = Key.I;
                    break;
                    
                case NativeMethods.VK_J:
                    key = Key.J;
                    break;
                    
                case NativeMethods.VK_K:
                    key = Key.K;
                    break;
                    
                case NativeMethods.VK_L:
                    key = Key.L;
                    break;
                    
                case NativeMethods.VK_M:
                    key = Key.M;
                    break;
                    
                case NativeMethods.VK_N:
                    key = Key.N;
                    break;
                    
                case NativeMethods.VK_O:
                    key = Key.O;
                    break;
                    
                case NativeMethods.VK_P:
                    key = Key.P;
                    break;
                    
                case NativeMethods.VK_Q:
                    key = Key.Q;
                    break;
                    
                case NativeMethods.VK_R:
                    key = Key.R;
                    break;
                    
                case NativeMethods.VK_S:
                    key = Key.S;
                    break;
                    
                case NativeMethods.VK_T:
                    key = Key.T;
                    break;
                    
                case NativeMethods.VK_U:
                    key = Key.U;
                    break;
                    
                case NativeMethods.VK_V:
                    key = Key.V;
                    break;
                    
                case NativeMethods.VK_W:
                    key = Key.W;
                    break;
                    
                case NativeMethods.VK_X:
                    key = Key.X;
                    break;
                    
                case NativeMethods.VK_Y:
                    key = Key.Y;
                    break;
                    
                case NativeMethods.VK_Z:
                    key = Key.Z;
                    break;
                    
                case NativeMethods.VK_LWIN:
                    key = Key.LWin;
                    break;
                    
                case NativeMethods.VK_RWIN:
                    key = Key.RWin;
                    break;
                    
                case NativeMethods.VK_APPS:
                    key = Key.Apps;
                    break;
                    
                case NativeMethods.VK_SLEEP:
                    key = Key.Sleep;
                    break;
                    
                case NativeMethods.VK_NUMPAD0:
                    key = Key.NumPad0;
                    break;
                    
                case NativeMethods.VK_NUMPAD1:
                    key = Key.NumPad1;
                    break;
                    
                case NativeMethods.VK_NUMPAD2:
                    key = Key.NumPad2;
                    break;
                    
                case NativeMethods.VK_NUMPAD3:
                    key = Key.NumPad3;
                    break;
                    
                case NativeMethods.VK_NUMPAD4:
                    key = Key.NumPad4;
                    break;
                    
                case NativeMethods.VK_NUMPAD5:
                    key = Key.NumPad5;
                    break;
                    
                case NativeMethods.VK_NUMPAD6:
                    key = Key.NumPad6;
                    break;
                    
                case NativeMethods.VK_NUMPAD7:
                    key = Key.NumPad7;
                    break;
                    
                case NativeMethods.VK_NUMPAD8:
                    key = Key.NumPad8;
                    break;
                    
                case NativeMethods.VK_NUMPAD9:
                    key = Key.NumPad9;
                    break;
                    
                case NativeMethods.VK_MULTIPLY:
                    key = Key.Multiply;
                    break;
                    
                case NativeMethods.VK_ADD:
                    key = Key.Add;
                    break;
                    
                case NativeMethods.VK_SEPARATOR:
                    key = Key.Separator;
                    break;
                    
                case NativeMethods.VK_SUBTRACT:
                    key = Key.Subtract;
                    break;
                    
                case NativeMethods.VK_DECIMAL:
                    key = Key.Decimal;
                    break;
                    
                case NativeMethods.VK_DIVIDE:
                    key = Key.Divide;
                    break;
                    
                case NativeMethods.VK_F1:
                    key = Key.F1;
                    break;
                    
                case NativeMethods.VK_F2:
                    key = Key.F2;
                    break;
                    
                case NativeMethods.VK_F3:
                    key = Key.F3;
                    break;
                    
                case NativeMethods.VK_F4:
                    key = Key.F4;
                    break;
                    
                case NativeMethods.VK_F5:
                    key = Key.F5;
                    break;
                    
                case NativeMethods.VK_F6:
                    key = Key.F6;
                    break;
                    
                case NativeMethods.VK_F7:
                    key = Key.F7;
                    break;
                    
                case NativeMethods.VK_F8:
                    key = Key.F8;
                    break;
                    
                case NativeMethods.VK_F9:
                    key = Key.F9;
                    break;
                    
                case NativeMethods.VK_F10:
                    key = Key.F10;
                    break;
                    
                case NativeMethods.VK_F11:
                    key = Key.F11;
                    break;
                    
                case NativeMethods.VK_F12:
                    key = Key.F12;
                    break;
                    
                case NativeMethods.VK_F13:
                    key = Key.F13;
                    break;
                    
                case NativeMethods.VK_F14:
                    key = Key.F14;
                    break;
                    
                case NativeMethods.VK_F15:
                    key = Key.F15;
                    break;
                    
                case NativeMethods.VK_F16:
                    key = Key.F16;
                    break;
                    
                case NativeMethods.VK_F17:
                    key = Key.F17;
                    break;
                    
                case NativeMethods.VK_F18:
                    key = Key.F18;
                    break;
                    
                case NativeMethods.VK_F19:
                    key = Key.F19;
                    break;
                    
                case NativeMethods.VK_F20:
                    key = Key.F20;
                    break;
                    
                case NativeMethods.VK_F21:
                    key = Key.F21;
                    break;
                    
                case NativeMethods.VK_F22:
                    key = Key.F22;
                    break;
                    
                case NativeMethods.VK_F23:
                    key = Key.F23;
                    break;
                    
                case NativeMethods.VK_F24:
                    key = Key.F24;
                    break;
                    
                case NativeMethods.VK_NUMLOCK:
                    key = Key.NumLock;
                    break;
                    
                case NativeMethods.VK_SCROLL:
                    key = Key.Scroll;
                    break;
                    
                case NativeMethods.VK_SHIFT:
                case NativeMethods.VK_LSHIFT:
                    key = Key.LeftShift;
                    break;
                    
                case NativeMethods.VK_RSHIFT:
                    key = Key.RightShift;
                    break;
                    
                case NativeMethods.VK_CONTROL:
                case NativeMethods.VK_LCONTROL:
                    key = Key.LeftCtrl;
                    break;
                    
                case NativeMethods.VK_RCONTROL:
                    key = Key.RightCtrl;
                    break;
                    
                case NativeMethods.VK_MENU:
                case NativeMethods.VK_LMENU:
                    key = Key.LeftAlt;
                    break;
                    
                case NativeMethods.VK_RMENU:
                    key = Key.RightAlt;
                    break;
                    
                case NativeMethods.VK_BROWSER_BACK:
                    key = Key.BrowserBack;
                    break;
                    
                case NativeMethods.VK_BROWSER_FORWARD:
                    key = Key.BrowserForward;
                    break;
                    
                case NativeMethods.VK_BROWSER_REFRESH:
                    key = Key.BrowserRefresh;
                    break;
                    
                case NativeMethods.VK_BROWSER_STOP:
                    key = Key.BrowserStop;
                    break;
                    
                case NativeMethods.VK_BROWSER_SEARCH:
                    key = Key.BrowserSearch;
                    break;
                    
                case NativeMethods.VK_BROWSER_FAVORITES:
                    key = Key.BrowserFavorites;
                    break;
                    
                case NativeMethods.VK_BROWSER_HOME:
                    key = Key.BrowserHome;
                    break;
                    
                case NativeMethods.VK_VOLUME_MUTE:
                    key = Key.VolumeMute;
                    break;
                    
                case NativeMethods.VK_VOLUME_DOWN:
                    key = Key.VolumeDown;
                    break;
                    
                case NativeMethods.VK_VOLUME_UP:
                    key = Key.VolumeUp;
                    break;
                    
                case NativeMethods.VK_MEDIA_NEXT_TRACK:
                    key = Key.MediaNextTrack;
                    break;
                    
                case NativeMethods.VK_MEDIA_PREV_TRACK:
                    key = Key.MediaPreviousTrack;
                    break;
                    
                case NativeMethods.VK_MEDIA_STOP:
                    key = Key.MediaStop;
                    break;
                    
                case NativeMethods.VK_MEDIA_PLAY_PAUSE:
                    key = Key.MediaPlayPause;
                    break;
                    
                case NativeMethods.VK_LAUNCH_MAIL:
                    key = Key.LaunchMail;
                    break;
                    
                case NativeMethods.VK_LAUNCH_MEDIA_SELECT:
                    key = Key.SelectMedia;
                    break;
                    
                case NativeMethods.VK_LAUNCH_APP1:
                    key = Key.LaunchApplication1;
                    break;
                    
                case NativeMethods.VK_LAUNCH_APP2:
                    key = Key.LaunchApplication2;
                    break;
                    
                case NativeMethods.VK_OEM_1:
                    key = Key.OemSemicolon;
                    break;
                    
                case NativeMethods.VK_OEM_PLUS:
                    key = Key.OemPlus;
                    break;
                    
                case NativeMethods.VK_OEM_COMMA:
                    key = Key.OemComma;
                    break;
                    
                case NativeMethods.VK_OEM_MINUS:
                    key = Key.OemMinus;
                    break;
                    
                case NativeMethods.VK_OEM_PERIOD:
                    key = Key.OemPeriod;
                    break;
                    
                case NativeMethods.VK_OEM_2:
                    key = Key.OemQuestion;
                    break;
                    
                case NativeMethods.VK_OEM_3:
                    key = Key.OemTilde;
                    break;

                case NativeMethods.VK_C1:
                    key = Key.AbntC1;
                    break;
                    
                case NativeMethods.VK_C2:
                    key = Key.AbntC2;
                    break;
                    
                case NativeMethods.VK_OEM_4:
                    key = Key.OemOpenBrackets;
                    break;
                    
                case NativeMethods.VK_OEM_5:
                    key = Key.OemPipe;
                    break;
                    
                case NativeMethods.VK_OEM_6:
                    key = Key.OemCloseBrackets;
                    break;
                    
                case NativeMethods.VK_OEM_7:
                    key = Key.OemQuotes;
                    break;
                    
                case NativeMethods.VK_OEM_8:
                    key = Key.Oem8;
                    break;
                    
                case NativeMethods.VK_OEM_102:
                    key = Key.OemBackslash;
                    break;
                    
                case NativeMethods.VK_PROCESSKEY:
                    key = Key.ImeProcessed;
                    break;

                case NativeMethods.VK_OEM_ATTN: // VK_DBE_ALPHANUMERIC
                    key = Key.OemAttn;          // DbeAlphanumeric
                    break;

                case NativeMethods.VK_OEM_FINISH: // VK_DBE_KATAKANA
                    key = Key.OemFinish;          // DbeKatakana
                    break;

                case NativeMethods.VK_OEM_COPY: // VK_DBE_HIRAGANA
                    key = Key.OemCopy;          // DbeHiragana
                    break;

                case NativeMethods.VK_OEM_AUTO: // VK_DBE_SBCSCHAR
                    key = Key.OemAuto;          // DbeSbcsChar
                    break;

                case NativeMethods.VK_OEM_ENLW: // VK_DBE_DBCSCHAR
                    key = Key.OemEnlw;          // DbeDbcsChar
                    break;

                case NativeMethods.VK_OEM_BACKTAB: // VK_DBE_ROMAN
                    key = Key.OemBackTab;          // DbeRoman
                    break;

                case NativeMethods.VK_ATTN: // VK_DBE_NOROMAN
                    key = Key.Attn;         // DbeNoRoman
                    break;
                    
                case NativeMethods.VK_CRSEL: // VK_DBE_ENTERWORDREGISTERMODE
                    key = Key.CrSel;         // DbeEnterWordRegisterMode
                    break;
                    
                case NativeMethods.VK_EXSEL: // VK_DBE_ENTERIMECONFIGMODE
                    key = Key.ExSel;         // DbeEnterImeConfigMode
                    break;
                    
                case NativeMethods.VK_EREOF: // VK_DBE_FLUSHSTRING
                    key = Key.EraseEof;      // DbeFlushString
                    break;
                    
                case NativeMethods.VK_PLAY: // VK_DBE_CODEINPUT
                    key = Key.Play;         // DbeCodeInput
                    break;
                    
                case NativeMethods.VK_ZOOM: // VK_DBE_NOCODEINPUT
                    key = Key.Zoom;         // DbeNoCodeInput
                    break;
                    
                case NativeMethods.VK_NONAME: // VK_DBE_DETERMINESTRING
                    key = Key.NoName;         // DbeDetermineString
                    break;
                    
                case NativeMethods.VK_PA1: // VK_DBE_ENTERDLGCONVERSIONMODE
                    key = Key.Pa1;         // DbeEnterDlgConversionMode
                    break;
                    
                case NativeMethods.VK_OEM_CLEAR:
                    key = Key.OemClear;
                    break;

                default:
                    key = Key.None;
                    break;
            }

            return key;
        }

        /// <summary>
        ///     Convert our Key enum into a Win32 VirtualKey.
        /// </summary>
        public static int VirtualKeyFromKey(Key key)
        {
            int virtualKey = 0;
            
            switch(key)
            {
                case Key.Cancel:
                    virtualKey = NativeMethods.VK_CANCEL;
                    break;
                    
                case Key.Back:
                    virtualKey = NativeMethods.VK_BACK;
                    break;
                    
                case Key.Tab:
                    virtualKey = NativeMethods.VK_TAB;
                    break;
                    
                case Key.Clear:
                    virtualKey = NativeMethods.VK_CLEAR;
                    break;
                    
                case Key.Return:
                    virtualKey = NativeMethods.VK_RETURN;
                    break;
                    
                case Key.Pause:
                    virtualKey = NativeMethods.VK_PAUSE;
                    break;
                    
                case Key.Capital:
                    virtualKey = NativeMethods.VK_CAPITAL;
                    break;
                    
                case Key.KanaMode:
                    virtualKey = NativeMethods.VK_KANA;
                    break;
                    
                case Key.JunjaMode:
                    virtualKey = NativeMethods.VK_JUNJA;
                    break;
                    
                case Key.FinalMode:
                    virtualKey = NativeMethods.VK_FINAL;
                    break;
                    
                case Key.KanjiMode:
                    virtualKey = NativeMethods.VK_KANJI;
                    break;
                    
                case Key.Escape:
                    virtualKey = NativeMethods.VK_ESCAPE;
                    break;
                    
                case Key.ImeConvert:
                    virtualKey = NativeMethods.VK_CONVERT;
                    break;
                    
                case Key.ImeNonConvert:
                    virtualKey = NativeMethods.VK_NONCONVERT;
                    break;
                    
                case Key.ImeAccept:
                    virtualKey = NativeMethods.VK_ACCEPT;
                    break;
                    
                case Key.ImeModeChange:
                    virtualKey = NativeMethods.VK_MODECHANGE;
                    break;
                    
                case Key.Space:
                    virtualKey = NativeMethods.VK_SPACE;
                    break;
                    
                case Key.Prior:
                    virtualKey = NativeMethods.VK_PRIOR;
                    break;
                    
                case Key.Next:
                    virtualKey = NativeMethods.VK_NEXT;
                    break;
                    
                case Key.End:
                    virtualKey = NativeMethods.VK_END;
                    break;
                    
                case Key.Home:
                    virtualKey = NativeMethods.VK_HOME;
                    break;
                    
                case Key.Left:
                    virtualKey = NativeMethods.VK_LEFT;
                    break;
                    
                case Key.Up:
                    virtualKey = NativeMethods.VK_UP;
                    break;
                    
                case Key.Right:
                    virtualKey = NativeMethods.VK_RIGHT;
                    break;
                    
                case Key.Down:
                    virtualKey = NativeMethods.VK_DOWN;
                    break;
                    
                case Key.Select:
                    virtualKey = NativeMethods.VK_SELECT;
                    break;
                    
                case Key.Print:
                    virtualKey = NativeMethods.VK_PRINT;
                    break;
                    
                case Key.Execute:
                    virtualKey = NativeMethods.VK_EXECUTE;
                    break;
                    
                case Key.Snapshot:
                    virtualKey = NativeMethods.VK_SNAPSHOT;
                    break;
                    
                case Key.Insert:
                    virtualKey = NativeMethods.VK_INSERT;
                    break;
                    
                case Key.Delete:
                    virtualKey = NativeMethods.VK_DELETE;
                    break;
                    
                case Key.Help:
                    virtualKey = NativeMethods.VK_HELP;
                    break;
                    
                case Key.D0:
                    virtualKey = NativeMethods.VK_0;
                    break;
                    
                case Key.D1:
                    virtualKey = NativeMethods.VK_1;
                    break;
                    
                case Key.D2:
                    virtualKey = NativeMethods.VK_2;
                    break;
                    
                case Key.D3:
                    virtualKey = NativeMethods.VK_3;
                    break;
                    
                case Key.D4:
                    virtualKey = NativeMethods.VK_4;
                    break;
                    
                case Key.D5:
                    virtualKey = NativeMethods.VK_5;
                    break;
                    
                case Key.D6:
                    virtualKey = NativeMethods.VK_6;
                    break;
                    
                case Key.D7:
                    virtualKey = NativeMethods.VK_7;
                    break;
                    
                case Key.D8:
                    virtualKey = NativeMethods.VK_8;
                    break;
                    
                case Key.D9:
                    virtualKey = NativeMethods.VK_9;
                    break;
                    
                case Key.A:
                    virtualKey = NativeMethods.VK_A;
                    break;
                    
                case Key.B:
                    virtualKey = NativeMethods.VK_B;
                    break;
                    
                case Key.C:
                    virtualKey = NativeMethods.VK_C;
                    break;
                    
                case Key.D:
                    virtualKey = NativeMethods.VK_D;
                    break;
                    
                case Key.E:
                    virtualKey = NativeMethods.VK_E;
                    break;
                    
                case Key.F:
                    virtualKey = NativeMethods.VK_F;
                    break;
                    
                case Key.G:
                    virtualKey = NativeMethods.VK_G;
                    break;
                    
                case Key.H:
                    virtualKey = NativeMethods.VK_H;
                    break;
                    
                case Key.I:
                    virtualKey = NativeMethods.VK_I;
                    break;
                    
                case Key.J:
                    virtualKey = NativeMethods.VK_J;
                    break;
                    
                case Key.K:
                    virtualKey = NativeMethods.VK_K;
                    break;
                    
                case Key.L:
                    virtualKey = NativeMethods.VK_L;
                    break;
                    
                case Key.M:
                    virtualKey = NativeMethods.VK_M;
                    break;
                    
                case Key.N:
                    virtualKey = NativeMethods.VK_N;
                    break;
                    
                case Key.O:
                    virtualKey = NativeMethods.VK_O;
                    break;
                    
                case Key.P:
                    virtualKey = NativeMethods.VK_P;
                    break;
                    
                case Key.Q:
                    virtualKey = NativeMethods.VK_Q;
                    break;
                    
                case Key.R:
                    virtualKey = NativeMethods.VK_R;
                    break;
                    
                case Key.S:
                    virtualKey = NativeMethods.VK_S;
                    break;
                    
                case Key.T:
                    virtualKey = NativeMethods.VK_T;
                    break;
                    
                case Key.U:
                    virtualKey = NativeMethods.VK_U;
                    break;
                    
                case Key.V:
                    virtualKey = NativeMethods.VK_V;
                    break;
                    
                case Key.W:
                    virtualKey = NativeMethods.VK_W;
                    break;
                    
                case Key.X:
                    virtualKey = NativeMethods.VK_X;
                    break;
                    
                case Key.Y:
                    virtualKey = NativeMethods.VK_Y;
                    break;
                    
                case Key.Z:
                    virtualKey = NativeMethods.VK_Z;
                    break;
                    
                case Key.LWin:
                    virtualKey = NativeMethods.VK_LWIN;
                    break;
                    
                case Key.RWin:
                    virtualKey = NativeMethods.VK_RWIN;
                    break;
                    
                case Key.Apps:
                    virtualKey = NativeMethods.VK_APPS;
                    break;
                    
                case Key.Sleep:
                    virtualKey = NativeMethods.VK_SLEEP;
                    break;
                    
                case Key.NumPad0:
                    virtualKey = NativeMethods.VK_NUMPAD0;
                    break;
                    
                case Key.NumPad1:
                    virtualKey = NativeMethods.VK_NUMPAD1;
                    break;
                    
                case Key.NumPad2:
                    virtualKey = NativeMethods.VK_NUMPAD2;
                    break;
                    
                case Key.NumPad3:
                    virtualKey = NativeMethods.VK_NUMPAD3;
                    break;
                    
                case Key.NumPad4:
                    virtualKey = NativeMethods.VK_NUMPAD4;
                    break;
                    
                case Key.NumPad5:
                    virtualKey = NativeMethods.VK_NUMPAD5;
                    break;
                    
                case Key.NumPad6:
                    virtualKey = NativeMethods.VK_NUMPAD6;
                    break;
                    
                case Key.NumPad7:
                    virtualKey = NativeMethods.VK_NUMPAD7;
                    break;
                    
                case Key.NumPad8:
                    virtualKey = NativeMethods.VK_NUMPAD8;
                    break;
                    
                case Key.NumPad9:
                    virtualKey = NativeMethods.VK_NUMPAD9;
                    break;
                    
                case Key.Multiply:
                    virtualKey = NativeMethods.VK_MULTIPLY;
                    break;
                    
                case Key.Add:
                    virtualKey = NativeMethods.VK_ADD;
                    break;
                    
                case Key.Separator:
                    virtualKey = NativeMethods.VK_SEPARATOR;
                    break;
                    
                case Key.Subtract:
                    virtualKey = NativeMethods.VK_SUBTRACT;
                    break;
                    
                case Key.Decimal:
                    virtualKey = NativeMethods.VK_DECIMAL;
                    break;
                    
                case Key.Divide:
                    virtualKey = NativeMethods.VK_DIVIDE;
                    break;
                    
                case Key.F1:
                    virtualKey = NativeMethods.VK_F1;
                    break;
                    
                case Key.F2:
                    virtualKey = NativeMethods.VK_F2;
                    break;
                    
                case Key.F3:
                    virtualKey = NativeMethods.VK_F3;
                    break;
                    
                case Key.F4:
                    virtualKey = NativeMethods.VK_F4;
                    break;
                    
                case Key.F5:
                    virtualKey = NativeMethods.VK_F5;
                    break;
                    
                case Key.F6:
                    virtualKey = NativeMethods.VK_F6;
                    break;
                    
                case Key.F7:
                    virtualKey = NativeMethods.VK_F7;
                    break;
                    
                case Key.F8:
                    virtualKey = NativeMethods.VK_F8;
                    break;
                    
                case Key.F9:
                    virtualKey = NativeMethods.VK_F9;
                    break;
                    
                case Key.F10:
                    virtualKey = NativeMethods.VK_F10;
                    break;
                    
                case Key.F11:
                    virtualKey = NativeMethods.VK_F11;
                    break;
                    
                case Key.F12:
                    virtualKey = NativeMethods.VK_F12;
                    break;
                    
                case Key.F13:
                    virtualKey = NativeMethods.VK_F13;
                    break;
                    
                case Key.F14:
                    virtualKey = NativeMethods.VK_F14;
                    break;
                    
                case Key.F15:
                    virtualKey = NativeMethods.VK_F15;
                    break;
                    
                case Key.F16:
                    virtualKey = NativeMethods.VK_F16;
                    break;
                    
                case Key.F17:
                    virtualKey = NativeMethods.VK_F17;
                    break;
                    
                case Key.F18:
                    virtualKey = NativeMethods.VK_F18;
                    break;
                    
                case Key.F19:
                    virtualKey = NativeMethods.VK_F19;
                    break;
                    
                case Key.F20:
                    virtualKey = NativeMethods.VK_F20;
                    break;
                    
                case Key.F21:
                    virtualKey = NativeMethods.VK_F21;
                    break;
                    
                case Key.F22:
                    virtualKey = NativeMethods.VK_F22;
                    break;
                    
                case Key.F23:
                    virtualKey = NativeMethods.VK_F23;
                    break;
                    
                case Key.F24:
                    virtualKey = NativeMethods.VK_F24;
                    break;
                    
                case Key.NumLock:
                    virtualKey = NativeMethods.VK_NUMLOCK;
                    break;
                    
                case Key.Scroll:
                    virtualKey = NativeMethods.VK_SCROLL;
                    break;
                    
                case Key.LeftShift:
                    virtualKey = NativeMethods.VK_LSHIFT;
                    break;
                    
                case Key.RightShift:
                    virtualKey = NativeMethods.VK_RSHIFT;
                    break;
                    
                case Key.LeftCtrl:
                    virtualKey = NativeMethods.VK_LCONTROL;
                    break;
                    
                case Key.RightCtrl:
                    virtualKey = NativeMethods.VK_RCONTROL;
                    break;
                    
                case Key.LeftAlt:
                    virtualKey = NativeMethods.VK_LMENU;
                    break;
                    
                case Key.RightAlt:
                    virtualKey = NativeMethods.VK_RMENU;
                    break;
                    
                case Key.BrowserBack:
                    virtualKey = NativeMethods.VK_BROWSER_BACK;
                    break;
                    
                case Key.BrowserForward:
                    virtualKey = NativeMethods.VK_BROWSER_FORWARD;
                    break;
                    
                case Key.BrowserRefresh:
                    virtualKey = NativeMethods.VK_BROWSER_REFRESH;
                    break;
                    
                case Key.BrowserStop:
                    virtualKey = NativeMethods.VK_BROWSER_STOP;
                    break;
                    
                case Key.BrowserSearch:
                    virtualKey = NativeMethods.VK_BROWSER_SEARCH;
                    break;
                    
                case Key.BrowserFavorites:
                    virtualKey = NativeMethods.VK_BROWSER_FAVORITES;
                    break;
                    
                case Key.BrowserHome:
                    virtualKey = NativeMethods.VK_BROWSER_HOME;
                    break;
                    
                case Key.VolumeMute:
                    virtualKey = NativeMethods.VK_VOLUME_MUTE;
                    break;
                    
                case Key.VolumeDown:
                    virtualKey = NativeMethods.VK_VOLUME_DOWN;
                    break;
                    
                case Key.VolumeUp:
                    virtualKey = NativeMethods.VK_VOLUME_UP;
                    break;
                    
                case Key.MediaNextTrack:
                    virtualKey = NativeMethods.VK_MEDIA_NEXT_TRACK;
                    break;
                    
                case Key.MediaPreviousTrack:
                    virtualKey = NativeMethods.VK_MEDIA_PREV_TRACK;
                    break;
                    
                case Key.MediaStop:
                    virtualKey = NativeMethods.VK_MEDIA_STOP;
                    break;
                    
                case Key.MediaPlayPause:
                    virtualKey = NativeMethods.VK_MEDIA_PLAY_PAUSE;
                    break;
                    
                case Key.LaunchMail:
                    virtualKey = NativeMethods.VK_LAUNCH_MAIL;
                    break;
                    
                case Key.SelectMedia:
                    virtualKey = NativeMethods.VK_LAUNCH_MEDIA_SELECT;
                    break;
                    
                case Key.LaunchApplication1:
                    virtualKey = NativeMethods.VK_LAUNCH_APP1;
                    break;
                    
                case Key.LaunchApplication2:
                    virtualKey = NativeMethods.VK_LAUNCH_APP2;
                    break;
                    
                case Key.OemSemicolon:
                    virtualKey = NativeMethods.VK_OEM_1;
                    break;
                    
                case Key.OemPlus:
                    virtualKey = NativeMethods.VK_OEM_PLUS;
                    break;
                    
                case Key.OemComma:
                    virtualKey = NativeMethods.VK_OEM_COMMA;
                    break;
                    
                case Key.OemMinus:
                    virtualKey = NativeMethods.VK_OEM_MINUS;
                    break;
                    
                case Key.OemPeriod:
                    virtualKey = NativeMethods.VK_OEM_PERIOD;
                    break;
                    
                case Key.OemQuestion:
                    virtualKey = NativeMethods.VK_OEM_2;
                    break;
                    
                case Key.OemTilde:
                    virtualKey = NativeMethods.VK_OEM_3;
                    break;
                    
                case Key.AbntC1:
                    virtualKey = NativeMethods.VK_C1;
                    break;
                    
                case Key.AbntC2:
                    virtualKey = NativeMethods.VK_C2;
                    break;
                    
                case Key.OemOpenBrackets:
                    virtualKey = NativeMethods.VK_OEM_4;
                    break;
                    
                case Key.OemPipe:
                    virtualKey = NativeMethods.VK_OEM_5;
                    break;
                    
                case Key.OemCloseBrackets:
                    virtualKey = NativeMethods.VK_OEM_6;
                    break;
                    
                case Key.OemQuotes:
                    virtualKey = NativeMethods.VK_OEM_7;
                    break;
                    
                case Key.Oem8:
                    virtualKey = NativeMethods.VK_OEM_8;
                    break;
                    
                case Key.OemBackslash:
                    virtualKey = NativeMethods.VK_OEM_102;
                    break;
                    
                case Key.ImeProcessed:
                    virtualKey = NativeMethods.VK_PROCESSKEY;
                    break;

                case Key.OemAttn:                           // DbeAlphanumeric
                    virtualKey = NativeMethods.VK_OEM_ATTN; // VK_DBE_ALPHANUMERIC
                    break;

                case Key.OemFinish:                           // DbeKatakana
                    virtualKey = NativeMethods.VK_OEM_FINISH; // VK_DBE_KATAKANA
                    break;

                case Key.OemCopy:                           // DbeHiragana
                    virtualKey = NativeMethods.VK_OEM_COPY; // VK_DBE_HIRAGANA
                    break;

                case Key.OemAuto:                           // DbeSbcsChar
                    virtualKey = NativeMethods.VK_OEM_AUTO; // VK_DBE_SBCSCHAR
                    break;

                case Key.OemEnlw:                           // DbeDbcsChar
                    virtualKey = NativeMethods.VK_OEM_ENLW; // VK_DBE_DBCSCHAR
                    break;

                case Key.OemBackTab:                           // DbeRoman
                    virtualKey = NativeMethods.VK_OEM_BACKTAB; // VK_DBE_ROMAN
                    break;
                    
                case Key.Attn:                          // DbeNoRoman
                    virtualKey = NativeMethods.VK_ATTN; // VK_DBE_NOROMAN
                    break;
                    
                case Key.CrSel:                          // DbeEnterWordRegisterMode
                    virtualKey = NativeMethods.VK_CRSEL; // VK_DBE_ENTERWORDREGISTERMODE
                    break;
                    
                case Key.ExSel:                          // EnterImeConfigureMode
                    virtualKey = NativeMethods.VK_EXSEL; // VK_DBE_ENTERIMECONFIGMODE
                    break;
                    
                case Key.EraseEof:                       // DbeFlushString
                    virtualKey = NativeMethods.VK_EREOF; // VK_DBE_FLUSHSTRING
                    break;
                    
                case Key.Play:                           // DbeCodeInput
                    virtualKey = NativeMethods.VK_PLAY;  // VK_DBE_CODEINPUT
                    break;
                    
                case Key.Zoom:                           // DbeNoCodeInput
                    virtualKey = NativeMethods.VK_ZOOM;  // VK_DBE_NOCODEINPUT
                    break;
                    
                case Key.NoName:                          // DbeDetermineString
                    virtualKey = NativeMethods.VK_NONAME; // VK_DBE_DETERMINESTRING
                    break;
                    
                case Key.Pa1:                          // DbeEnterDlgConversionMode
                    virtualKey = NativeMethods.VK_PA1; // VK_ENTERDLGCONVERSIONMODE
                    break;
                    
                case Key.OemClear:
                    virtualKey = NativeMethods.VK_OEM_CLEAR;
                    break;

                case Key.DeadCharProcessed:             //This is usused.  It's just here for completeness.
                    virtualKey = 0;                     //There is no Win32 VKey for this.
                    break;
        
                default:
                    virtualKey = 0;
                    break;
            }
        
            return virtualKey;
        }
    }
}

