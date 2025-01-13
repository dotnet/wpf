// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Windows.Input.Tests;

public class KeyInteropTests
{
    [Theory]
    [InlineData(VK_LBUTTON, Key.None)]
    [InlineData(VK_RBUTTON, Key.None)]
    [InlineData(VK_CANCEL, Key.Cancel)]
    [InlineData(VK_MBUTTON, Key.None)]
    [InlineData(VK_XBUTTON1, Key.None)]
    [InlineData(VK_XBUTTON2, Key.None)]
    [InlineData(0x07, Key.None)]
    [InlineData(VK_BACK, Key.Back)]
    [InlineData(VK_TAB, Key.Tab)]
    [InlineData(0x0A, Key.None)]
    [InlineData(0x0B, Key.None)]
    [InlineData(VK_CLEAR, Key.Clear)]
    [InlineData(VK_RETURN, Key.Return)]
    [InlineData(0x0E, Key.None)]
    [InlineData(0x0F, Key.None)]
    [InlineData(VK_SHIFT, Key.LeftShift)]
    [InlineData(VK_CONTROL, Key.LeftCtrl)]
    [InlineData(VK_MENU, Key.LeftAlt)]
    [InlineData(VK_PAUSE, Key.Pause)]
    [InlineData(VK_CAPITAL, Key.Capital)]
    [InlineData(VK_KANA, Key.KanaMode)]
    [InlineData(0x16, Key.None)]
    [InlineData(VK_JUNJA, Key.JunjaMode)]
    [InlineData(VK_FINAL, Key.FinalMode)]
    [InlineData(VK_KANJI, Key.KanjiMode)]
    [InlineData(0x1A, Key.None)]
    [InlineData(VK_ESCAPE, Key.Escape)]
    [InlineData(VK_CONVERT, Key.ImeConvert)]
    [InlineData(VK_NONCONVERT, Key.ImeNonConvert)]
    [InlineData(VK_ACCEPT, Key.ImeAccept)]
    [InlineData(VK_MODECHANGE, Key.ImeModeChange)]
    [InlineData(VK_SPACE, Key.Space)]
    [InlineData(VK_PRIOR, Key.Prior)]
    [InlineData(VK_NEXT, Key.Next)]
    [InlineData(VK_END, Key.End)]
    [InlineData(VK_HOME, Key.Home)]
    [InlineData(VK_LEFT, Key.Left)]
    [InlineData(VK_UP, Key.Up)]
    [InlineData(VK_RIGHT, Key.Right)]
    [InlineData(VK_DOWN, Key.Down)]
    [InlineData(VK_SELECT, Key.Select)]
    [InlineData(VK_PRINT, Key.Print)]
    [InlineData(VK_EXECUTE, Key.Execute)]
    [InlineData(VK_SNAPSHOT, Key.Snapshot)]
    [InlineData(VK_INSERT, Key.Insert)]
    [InlineData(VK_DELETE, Key.Delete)]
    [InlineData(VK_HELP, Key.Help)]
    [InlineData(VK_0, Key.D0)]
    [InlineData(VK_1, Key.D1)]
    [InlineData(VK_2, Key.D2)]
    [InlineData(VK_3, Key.D3)]
    [InlineData(VK_4, Key.D4)]
    [InlineData(VK_5, Key.D5)]
    [InlineData(VK_6, Key.D6)]
    [InlineData(VK_7, Key.D7)]
    [InlineData(VK_8, Key.D8)]
    [InlineData(VK_9, Key.D9)]
    [InlineData(0x3A, Key.None)]
    [InlineData(0x3B, Key.None)]
    [InlineData(0x3C, Key.None)]
    [InlineData(0x3D, Key.None)]
    [InlineData(0x3E, Key.None)]
    [InlineData(0x3F, Key.None)]
    [InlineData(VK_A, Key.A)]
    [InlineData(VK_B, Key.B)]
    [InlineData(VK_C, Key.C)]
    [InlineData(VK_D, Key.D)]
    [InlineData(VK_E, Key.E)]
    [InlineData(VK_F, Key.F)]
    [InlineData(VK_G, Key.G)]
    [InlineData(VK_H, Key.H)]
    [InlineData(VK_I, Key.I)]
    [InlineData(VK_J, Key.J)]
    [InlineData(VK_K, Key.K)]
    [InlineData(VK_L, Key.L)]
    [InlineData(VK_M, Key.M)]
    [InlineData(VK_N, Key.N)]
    [InlineData(VK_O, Key.O)]
    [InlineData(VK_P, Key.P)]
    [InlineData(VK_Q, Key.Q)]
    [InlineData(VK_R, Key.R)]
    [InlineData(VK_S, Key.S)]
    [InlineData(VK_T, Key.T)]
    [InlineData(VK_U, Key.U)]
    [InlineData(VK_V, Key.V)]
    [InlineData(VK_W, Key.W)]
    [InlineData(VK_X, Key.X)]
    [InlineData(VK_Y, Key.Y)]
    [InlineData(VK_Z, Key.Z)]
    [InlineData(VK_LWIN, Key.LWin)]
    [InlineData(VK_RWIN, Key.RWin)]
    [InlineData(VK_APPS, Key.Apps)]
    [InlineData(0x5E, Key.None)]
    [InlineData(VK_SLEEP, Key.Sleep)]
    [InlineData(VK_NUMPAD0, Key.NumPad0)]
    [InlineData(VK_NUMPAD1, Key.NumPad1)]
    [InlineData(VK_NUMPAD2, Key.NumPad2)]
    [InlineData(VK_NUMPAD3, Key.NumPad3)]
    [InlineData(VK_NUMPAD4, Key.NumPad4)]
    [InlineData(VK_NUMPAD5, Key.NumPad5)]
    [InlineData(VK_NUMPAD6, Key.NumPad6)]
    [InlineData(VK_NUMPAD7, Key.NumPad7)]
    [InlineData(VK_NUMPAD8, Key.NumPad8)]
    [InlineData(VK_NUMPAD9, Key.NumPad9)]
    [InlineData(VK_MULTIPLY, Key.Multiply)]
    [InlineData(VK_ADD, Key.Add)]
    [InlineData(VK_SEPARATOR, Key.Separator)]
    [InlineData(VK_SUBTRACT, Key.Subtract)]
    [InlineData(VK_DECIMAL, Key.Decimal)]
    [InlineData(VK_DIVIDE, Key.Divide)]
    [InlineData(VK_F1, Key.F1)]
    [InlineData(VK_F2, Key.F2)]
    [InlineData(VK_F3, Key.F3)]
    [InlineData(VK_F4, Key.F4)]
    [InlineData(VK_F5, Key.F5)]
    [InlineData(VK_F6, Key.F6)]
    [InlineData(VK_F7, Key.F7)]
    [InlineData(VK_F8, Key.F8)]
    [InlineData(VK_F9, Key.F9)]
    [InlineData(VK_F10, Key.F10)]
    [InlineData(VK_F11, Key.F11)]
    [InlineData(VK_F12, Key.F12)]
    [InlineData(VK_F13, Key.F13)]
    [InlineData(VK_F14, Key.F14)]
    [InlineData(VK_F15, Key.F15)]
    [InlineData(VK_F16, Key.F16)]
    [InlineData(VK_F17, Key.F17)]
    [InlineData(VK_F18, Key.F18)]
    [InlineData(VK_F19, Key.F19)]
    [InlineData(VK_F20, Key.F20)]
    [InlineData(VK_F21, Key.F21)]
    [InlineData(VK_F22, Key.F22)]
    [InlineData(VK_F23, Key.F23)]
    [InlineData(VK_F24, Key.F24)]
    [InlineData(VK_NUMLOCK, Key.NumLock)]
    [InlineData(VK_SCROLL, Key.Scroll)]
    [InlineData(VK_OEM_NEC_EQUAL, Key.None)]
    //[InlineData(VK_OEM_FJ_JISHO, Key.None)]
    [InlineData(VK_OEM_FJ_MASSHOU, Key.None)]
    [InlineData(VK_OEM_FJ_TOUROKU, Key.None)]
    [InlineData(VK_OEM_FJ_LOYA, Key.None)]
    [InlineData(VK_OEM_FJ_ROYA, Key.None)]
    [InlineData(VK_LSHIFT, Key.LeftShift)]
    [InlineData(VK_RSHIFT, Key.RightShift)]
    [InlineData(VK_LCONTROL, Key.LeftCtrl)]
    [InlineData(VK_RCONTROL, Key.RightCtrl)]
    [InlineData(VK_LMENU, Key.LeftAlt)]
    [InlineData(VK_RMENU, Key.RightAlt)]
    [InlineData(VK_BROWSER_BACK, Key.BrowserBack)]
    [InlineData(VK_BROWSER_FORWARD, Key.BrowserForward)]
    [InlineData(VK_BROWSER_REFRESH, Key.BrowserRefresh)]
    [InlineData(VK_BROWSER_STOP, Key.BrowserStop)]
    [InlineData(VK_BROWSER_SEARCH, Key.BrowserSearch)]
    [InlineData(VK_BROWSER_FAVORITES, Key.BrowserFavorites)]
    [InlineData(VK_BROWSER_HOME, Key.BrowserHome)]
    [InlineData(VK_VOLUME_MUTE, Key.VolumeMute)]
    [InlineData(VK_VOLUME_DOWN, Key.VolumeDown)]
    [InlineData(VK_VOLUME_UP, Key.VolumeUp)]
    [InlineData(VK_MEDIA_NEXT_TRACK, Key.MediaNextTrack)]
    [InlineData(VK_MEDIA_PREV_TRACK, Key.MediaPreviousTrack)]
    [InlineData(VK_MEDIA_STOP, Key.MediaStop)]
    [InlineData(VK_MEDIA_PLAY_PAUSE, Key.MediaPlayPause)]
    [InlineData(VK_LAUNCH_MAIL, Key.LaunchMail)]
    [InlineData(VK_LAUNCH_MEDIA_SELECT, Key.SelectMedia)]
    [InlineData(VK_LAUNCH_APP1, Key.LaunchApplication1)]
    [InlineData(VK_LAUNCH_APP2, Key.LaunchApplication2)]
    [InlineData(0xB8, Key.None)]
    [InlineData(0xB9, Key.None)]
    [InlineData(VK_OEM_1, Key.OemSemicolon)]
    [InlineData(VK_OEM_PLUS, Key.OemPlus)]
    [InlineData(VK_OEM_COMMA, Key.OemComma)]
    [InlineData(VK_OEM_MINUS, Key.OemMinus)]
    [InlineData(VK_OEM_PERIOD, Key.OemPeriod)]
    [InlineData(VK_OEM_2, Key.OemQuestion)]
    [InlineData(VK_OEM_3, Key.OemTilde)]
    [InlineData(VK_C1, Key.AbntC1)]
    [InlineData(VK_C2, Key.AbntC2)]
    [InlineData(VK_GAMEPAD_A, Key.None)]
    [InlineData(VK_GAMEPAD_B, Key.None)]
    [InlineData(VK_GAMEPAD_X, Key.None)]
    [InlineData(VK_GAMEPAD_Y, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_SHOULDER, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_SHOULDER, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_TRIGGER, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_TRIGGER, Key.None)]
    [InlineData(VK_GAMEPAD_DPAD_UP, Key.None)]
    [InlineData(VK_GAMEPAD_DPAD_DOWN, Key.None)]
    [InlineData(VK_GAMEPAD_DPAD_LEFT, Key.None)]
    [InlineData(VK_GAMEPAD_DPAD_RIGHT, Key.None)]
    [InlineData(VK_GAMEPAD_MENU, Key.None)]
    [InlineData(VK_GAMEPAD_VIEW, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_THUMBSTICK_UP, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_THUMBSTICK_DOWN, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT, Key.None)]
    [InlineData(VK_GAMEPAD_LEFT_THUMBSTICK_LEFT, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_THUMBSTICK_UP, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT, Key.None)]
    [InlineData(VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT, Key.None)]
    [InlineData(VK_OEM_4, Key.OemOpenBrackets)]
    [InlineData(VK_OEM_5, Key.OemPipe)]
    [InlineData(VK_OEM_6, Key.OemCloseBrackets)]
    [InlineData(VK_OEM_7, Key.OemQuotes)]
    [InlineData(VK_OEM_8, Key.Oem8)]
    [InlineData(0xE0, Key.None)]
    [InlineData(VK_OEM_AX, Key.None)]
    [InlineData(VK_OEM_102, Key.OemBackslash)]
    [InlineData(VK_ICO_HELP, Key.None)]
    [InlineData(VK_ICO_00, Key.None)]
    [InlineData(VK_PROCESSKEY, Key.ImeProcessed)]
    [InlineData(VK_ICO_CLEAR, Key.None)]
    [InlineData(VK_PACKET, Key.None)]
    [InlineData(0xE8, Key.None)]
    [InlineData(VK_OEM_RESET, Key.None)]
    [InlineData(VK_OEM_JUMP, Key.None)]
    [InlineData(VK_OEM_PA1, Key.None)]
    [InlineData(VK_OEM_PA2, Key.None)]
    [InlineData(VK_OEM_PA3, Key.None)]
    [InlineData(VK_OEM_WSCTRL, Key.None)]
    [InlineData(VK_OEM_CUSEL, Key.None)]
    [InlineData(VK_OEM_ATTN, Key.OemAttn)]
    [InlineData(VK_OEM_FINISH, Key.OemFinish)]
    [InlineData(VK_OEM_COPY, Key.OemCopy)]
    [InlineData(VK_OEM_AUTO, Key.OemAuto)]
    [InlineData(VK_OEM_ENLW, Key.OemEnlw)]
    [InlineData(VK_OEM_BACKTAB, Key.OemBackTab)]
    [InlineData(VK_ATTN, Key.Attn)]
    [InlineData(VK_CRSEL, Key.CrSel)]
    [InlineData(VK_EXSEL, Key.ExSel)]
    [InlineData(VK_EREOF, Key.EraseEof)]
    [InlineData(VK_PLAY, Key.Play)]
    [InlineData(VK_ZOOM, Key.Zoom)]
    [InlineData(VK_NONAME, Key.NoName)]
    [InlineData(VK_PA1, Key.Pa1)]
    [InlineData(VK_OEM_CLEAR, Key.OemClear)]
    [InlineData(0xFF, Key.None)]
    [InlineData(0x100, Key.None)]
    [InlineData(int.MinValue, Key.None)]
    [InlineData(-1, Key.None)]
    [InlineData(int.MaxValue, Key.None)]
    public void KeyFromVirtualKey_Invoke_ReturnsExpected(int virtualKey, Key expected)
    {
        Assert.Equal(expected, KeyInterop.KeyFromVirtualKey(virtualKey));
    }

    [Theory]
    [InlineData(Key.None, 0)]
    [InlineData(Key.Cancel, VK_CANCEL)]
    [InlineData(Key.Back, VK_BACK)]
    [InlineData(Key.Tab, VK_TAB)]
    [InlineData(Key.Clear, VK_CLEAR)]
    [InlineData(Key.Return, VK_RETURN)]
    //[InlineData(Key.Enter, VK_RETURN)]
    [InlineData(Key.Pause, VK_PAUSE)]
    [InlineData(Key.Capital, VK_CAPITAL)]
    //[InlineData(Key.CapsLock, VK_CAPITAL)]
    [InlineData(Key.KanaMode, VK_KANA)]
    //[InlineData(Key.HangulMode, VK_KANA)]
    [InlineData(Key.JunjaMode, VK_JUNJA)]
    [InlineData(Key.FinalMode, VK_FINAL)]
    [InlineData(Key.HanjaMode, VK_KANJI)]
    //[InlineData(Key.KanjiMode, VK_KANJI)]
    [InlineData(Key.Escape, VK_ESCAPE)]
    [InlineData(Key.ImeConvert, VK_CONVERT)]
    [InlineData(Key.ImeNonConvert, VK_NONCONVERT)]
    [InlineData(Key.ImeAccept, VK_ACCEPT)]
    [InlineData(Key.ImeModeChange, VK_MODECHANGE)]
    [InlineData(Key.Space, VK_SPACE)]
    [InlineData(Key.Prior, VK_PRIOR)]
    [InlineData(Key.Next, VK_NEXT)]
    //[InlineData(Key.PageDown, VK_NEXT)]
    [InlineData(Key.End, VK_END)]
    [InlineData(Key.Home, VK_HOME)]
    [InlineData(Key.Left, VK_LEFT)]
    [InlineData(Key.Up, VK_UP)]
    [InlineData(Key.Right, VK_RIGHT)]
    [InlineData(Key.Down, VK_DOWN)]
    [InlineData(Key.Select, VK_SELECT)]
    [InlineData(Key.Print, VK_PRINT)]
    [InlineData(Key.Execute, VK_EXECUTE)]
    [InlineData(Key.Snapshot, VK_SNAPSHOT)]
    //[InlineData(Key.PrintScreen, VK_SNAPSHOT)]
    [InlineData(Key.Insert, VK_INSERT)]
    [InlineData(Key.Delete, VK_DELETE)]
    [InlineData(Key.Help, VK_HELP)]
    [InlineData(Key.D0, VK_0)]
    [InlineData(Key.D1, VK_1)]
    [InlineData(Key.D2, VK_2)]
    [InlineData(Key.D3, VK_3)]
    [InlineData(Key.D4, VK_4)]
    [InlineData(Key.D5, VK_5)]
    [InlineData(Key.D6, VK_6)]
    [InlineData(Key.D7, VK_7)]
    [InlineData(Key.D8, VK_8)]
    [InlineData(Key.D9, VK_9)]
    [InlineData(Key.A, VK_A)]
    [InlineData(Key.B, VK_B)]
    [InlineData(Key.C, VK_C)]
    [InlineData(Key.D, VK_D)]
    [InlineData(Key.E, VK_E)]
    [InlineData(Key.F, VK_F)]
    [InlineData(Key.G, VK_G)]
    [InlineData(Key.H, VK_H)]
    [InlineData(Key.I, VK_I)]
    [InlineData(Key.J, VK_J)]
    [InlineData(Key.K, VK_K)]
    [InlineData(Key.L, VK_L)]
    [InlineData(Key.M, VK_M)]
    [InlineData(Key.N, VK_N)]
    [InlineData(Key.O, VK_O)]
    [InlineData(Key.P, VK_P)]
    [InlineData(Key.Q, VK_Q)]
    [InlineData(Key.R, VK_R)]
    [InlineData(Key.S, VK_S)]
    [InlineData(Key.T, VK_T)]
    [InlineData(Key.U, VK_U)]
    [InlineData(Key.V, VK_V)]
    [InlineData(Key.W, VK_W)]
    [InlineData(Key.X, VK_X)]
    [InlineData(Key.Y, VK_Y)]
    [InlineData(Key.Z, VK_Z)]
    [InlineData(Key.LWin, VK_LWIN)]
    [InlineData(Key.RWin, VK_RWIN)]
    [InlineData(Key.Apps, VK_APPS)]
    [InlineData(Key.Sleep, VK_SLEEP)]
    [InlineData(Key.NumPad0, VK_NUMPAD0)]
    [InlineData(Key.NumPad1, VK_NUMPAD1)]
    [InlineData(Key.NumPad2, VK_NUMPAD2)]
    [InlineData(Key.NumPad3, VK_NUMPAD3)]
    [InlineData(Key.NumPad4, VK_NUMPAD4)]
    [InlineData(Key.NumPad5, VK_NUMPAD5)]
    [InlineData(Key.NumPad6, VK_NUMPAD6)]
    [InlineData(Key.NumPad7, VK_NUMPAD7)]
    [InlineData(Key.NumPad8, VK_NUMPAD8)]
    [InlineData(Key.NumPad9, VK_NUMPAD9)]
    [InlineData(Key.Multiply, VK_MULTIPLY)]
    [InlineData(Key.Add, VK_ADD)]
    [InlineData(Key.Separator, VK_SEPARATOR)]
    [InlineData(Key.Subtract, VK_SUBTRACT)]
    [InlineData(Key.Decimal, VK_DECIMAL)]
    [InlineData(Key.Divide, VK_DIVIDE)]
    [InlineData(Key.F1, VK_F1)]
    [InlineData(Key.F2, VK_F2)]
    [InlineData(Key.F3, VK_F3)]
    [InlineData(Key.F4, VK_F4)]
    [InlineData(Key.F5, VK_F5)]
    [InlineData(Key.F6, VK_F6)]
    [InlineData(Key.F7, VK_F7)]
    [InlineData(Key.F8, VK_F8)]
    [InlineData(Key.F9, VK_F9)]
    [InlineData(Key.F10, VK_F10)]
    [InlineData(Key.F11, VK_F11)]
    [InlineData(Key.F12, VK_F12)]
    [InlineData(Key.F13, VK_F13)]
    [InlineData(Key.F14, VK_F14)]
    [InlineData(Key.F15, VK_F15)]
    [InlineData(Key.F16, VK_F16)]
    [InlineData(Key.F17, VK_F17)]
    [InlineData(Key.F18, VK_F18)]
    [InlineData(Key.F19, VK_F19)]
    [InlineData(Key.F20, VK_F20)]
    [InlineData(Key.F21, VK_F21)]
    [InlineData(Key.F22, VK_F22)]
    [InlineData(Key.F23, VK_F23)]
    [InlineData(Key.F24, VK_F24)]
    [InlineData(Key.NumLock, VK_NUMLOCK)]
    [InlineData(Key.Scroll, VK_SCROLL)]
    [InlineData(Key.LeftShift, VK_LSHIFT)]
    [InlineData(Key.RightShift, VK_RSHIFT)]
    [InlineData(Key.LeftCtrl, VK_LCONTROL)]
    [InlineData(Key.RightCtrl, VK_RCONTROL)]
    [InlineData(Key.LeftAlt, VK_LMENU)]
    [InlineData(Key.RightAlt, VK_RMENU)]
    [InlineData(Key.BrowserBack, VK_BROWSER_BACK)]
    [InlineData(Key.BrowserForward, VK_BROWSER_FORWARD)]
    [InlineData(Key.BrowserRefresh, VK_BROWSER_REFRESH)]
    [InlineData(Key.BrowserStop, VK_BROWSER_STOP)]
    [InlineData(Key.BrowserSearch, VK_BROWSER_SEARCH)]
    [InlineData(Key.BrowserFavorites, VK_BROWSER_FAVORITES)]
    [InlineData(Key.BrowserHome, VK_BROWSER_HOME)]
    [InlineData(Key.VolumeMute, VK_VOLUME_MUTE)]
    [InlineData(Key.VolumeDown, VK_VOLUME_DOWN)]
    [InlineData(Key.VolumeUp, VK_VOLUME_UP)]
    [InlineData(Key.MediaNextTrack, VK_MEDIA_NEXT_TRACK)]
    [InlineData(Key.MediaPreviousTrack, VK_MEDIA_PREV_TRACK)]
    [InlineData(Key.MediaStop, VK_MEDIA_STOP)]
    [InlineData(Key.MediaPlayPause, VK_MEDIA_PLAY_PAUSE)]
    [InlineData(Key.LaunchMail, VK_LAUNCH_MAIL)]
    [InlineData(Key.SelectMedia, VK_LAUNCH_MEDIA_SELECT)]
    [InlineData(Key.LaunchApplication1, VK_LAUNCH_APP1)]
    [InlineData(Key.LaunchApplication2, VK_LAUNCH_APP2)]
    [InlineData(Key.OemSemicolon, VK_OEM_1)]
    [InlineData(Key.OemPlus, VK_OEM_PLUS)]
    [InlineData(Key.OemComma, VK_OEM_COMMA)]
    [InlineData(Key.OemMinus, VK_OEM_MINUS)]
    [InlineData(Key.OemPeriod, VK_OEM_PERIOD)]
    [InlineData(Key.Oem2, VK_OEM_2)]
    //[InlineData(Key.OemQuestion, VK_OEM_2)]
    [InlineData(Key.Oem3, VK_OEM_3)]
    //[InlineData(Key.OemTilde, VK_OEM_3)]
    [InlineData(Key.AbntC1, VK_C1)]
    [InlineData(Key.AbntC2, VK_C2)]
    [InlineData(Key.Oem4, VK_OEM_4)]
    //[InlineData(Key.OemOpenBrackets, VK_OEM_4)]
    //[InlineData(Key.Oem5, VK_OEM_5)]
    [InlineData(Key.OemPipe, VK_OEM_5)]
    [InlineData(Key.Oem6, VK_OEM_6)]
    //[InlineData(Key.OemCloseBrackets, VK_OEM_6)]
    [InlineData(Key.Oem7, VK_OEM_7)]
    //[InlineData(Key.OemQuotes, VK_OEM_7)]
    [InlineData(Key.Oem8, VK_OEM_8)]
    [InlineData(Key.Oem102, VK_OEM_102)]
    //[InlineData(Key.OemBackslash, VK_OEM_102)]
    [InlineData(Key.ImeProcessed, VK_PROCESSKEY)]
    [InlineData(Key.OemAttn, VK_OEM_ATTN)]
    //[InlineData(Key.DbeAlphanumeric, VK_OEM_ATTN)]
    [InlineData(Key.OemFinish, VK_OEM_FINISH)]
    //[InlineData(Key.DbeKatakana, VK_OEM_FINISH)]
    [InlineData(Key.OemCopy, VK_OEM_COPY)]
    //[InlineData(Key.DbeHiragana, VK_OEM_COPY)]
    [InlineData(Key.OemAuto, VK_OEM_AUTO)]
    //[InlineData(Key.DbeSbcsChar, VK_OEM_AUTO)]
    [InlineData(Key.OemEnlw, VK_OEM_ENLW)]
    //[InlineData(Key.DbeDbcsChar, VK_OEM_ENLW)]
    [InlineData(Key.OemBackTab, VK_OEM_BACKTAB)]
    //[InlineData(Key.DbeRoman, VK_OEM_BACKTAB)]
    [InlineData(Key.Attn, VK_ATTN)]
    //[InlineData(Key.DbeNoRoman, VK_ATTN)]
    [InlineData(Key.CrSel, VK_CRSEL)]
    //[InlineData(Key.DbeEnterWordRegisterMode, VK_CRSEL)]
    [InlineData(Key.ExSel, VK_EXSEL)]
    //[InlineData(Key.DbeEnterImeConfigureMode, VK_EXSEL)]
    [InlineData(Key.EraseEof, VK_EREOF)]
    //[InlineData(Key.DbeFlushString, VK_EREOF)]
    [InlineData(Key.Play, VK_PLAY)]
    //[InlineData(Key.DbeCodeInput, VK_PLAY)]
    [InlineData(Key.Zoom, VK_ZOOM)]
    //[InlineData(Key.DbeNoCodeInput, VK_ZOOM)]
    [InlineData(Key.NoName, VK_NONAME)]
    //[InlineData(Key.DbeDetermineString, VK_NONAME)]
    [InlineData(Key.Pa1, VK_PA1)]
    //[InlineData(Key.DbeEnterDialogConversionMode, VK_PA1)]
    [InlineData(Key.OemClear, VK_OEM_CLEAR)]
    [InlineData(Key.DeadCharProcessed, 0)]
    [InlineData((Key)int.MinValue, 0)]
    [InlineData((Key)(-1), 0)]
    [InlineData(Key.DeadCharProcessed + 1, 0)]
    [InlineData((Key)int.MaxValue, 0)]
    public void VirtualKeyFromKey_Invoke_ReturnsExpected(Key key, int expected)
    {
        Assert.Equal(expected, KeyInterop.VirtualKeyFromKey(key));
    }

    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_CANCEL = 0x03;
    private const int VK_MBUTTON = 0x04;
    private const int VK_XBUTTON1 = 0x05;
    private const int VK_XBUTTON2 = 0x06;
    private const int VK_BACK = 0x08;
    private const int VK_TAB = 0x09;
    private const int VK_CLEAR = 0x0C;
    private const int VK_RETURN = 0x0D;
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12;
    private const int VK_PAUSE = 0x13;
    private const int VK_CAPITAL = 0x14;
    private const int VK_KANA = 0x15;
    // private const int VK_HANGEUL = 0x15;
    // private const int VK_HANGUL = 0x15;
    private const int VK_JUNJA = 0x17;
    private const int VK_FINAL = 0x18;
    // private const int VK_HANJA = 0x19;
    private const int VK_KANJI = 0x19;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_CONVERT = 0x1C;
    private const int VK_NONCONVERT = 0x1D;
    private const int VK_ACCEPT = 0x1E;
    private const int VK_MODECHANGE = 0x1F;
    private const int VK_SPACE = 0x20;
    private const int VK_PRIOR = 0x21;
    private const int VK_NEXT = 0x22;
    private const int VK_END = 0x23;
    private const int VK_HOME = 0x24;
    private const int VK_LEFT = 0x25;
    private const int VK_UP = 0x26;
    private const int VK_RIGHT = 0x27;
    private const int VK_DOWN = 0x28;
    private const int VK_SELECT = 0x29;
    private const int VK_PRINT = 0x2A;
    private const int VK_EXECUTE = 0x2B;
    private const int VK_SNAPSHOT = 0x2C;
    private const int VK_INSERT = 0x2D;
    private const int VK_DELETE = 0x2E;
    private const int VK_HELP = 0x2F;
    private const int VK_0 = 0x30;
    private const int VK_1 = 0x31;
    private const int VK_2 = 0x32;
    private const int VK_3 = 0x33;
    private const int VK_4 = 0x34;
    private const int VK_5 = 0x35;
    private const int VK_6 = 0x36;
    private const int VK_7 = 0x37;
    private const int VK_8 = 0x38;
    private const int VK_9 = 0x39;
    private const int VK_A = 0x41;
    private const int VK_B = 0x42;
    private const int VK_C = 0x43;
    private const int VK_D = 0x44;
    private const int VK_E = 0x45;
    private const int VK_F = 0x46;
    private const int VK_G = 0x47;
    private const int VK_H = 0x48;
    private const int VK_I = 0x49;
    private const int VK_J = 0x4A;
    private const int VK_K = 0x4B;
    private const int VK_L = 0x4C;
    private const int VK_M = 0x4D;
    private const int VK_N = 0x4E;
    private const int VK_O = 0x4F;
    private const int VK_P = 0x50;
    private const int VK_Q = 0x51;
    private const int VK_R = 0x52;
    private const int VK_S = 0x53;
    private const int VK_T = 0x54;
    private const int VK_U = 0x55;
    private const int VK_V = 0x56;
    private const int VK_W = 0x57;
    private const int VK_X = 0x58;
    private const int VK_Y = 0x59;
    private const int VK_Z = 0x5A;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;
    private const int VK_APPS = 0x5D;
    // private const int VK_POWER = 0x5E;
    private const int VK_SLEEP = 0x5F;
    private const int VK_NUMPAD0 = 0x60;
    private const int VK_NUMPAD1 = 0x61;
    private const int VK_NUMPAD2 = 0x62;
    private const int VK_NUMPAD3 = 0x63;
    private const int VK_NUMPAD4 = 0x64;
    private const int VK_NUMPAD5 = 0x65;
    private const int VK_NUMPAD6 = 0x66;
    private const int VK_NUMPAD7 = 0x67;
    private const int VK_NUMPAD8 = 0x68;
    private const int VK_NUMPAD9 = 0x69;
    private const int VK_MULTIPLY = 0x6A;
    private const int VK_ADD = 0x6B;
    private const int VK_SEPARATOR = 0x6C;
    private const int VK_SUBTRACT = 0x6D;
    private const int VK_DECIMAL = 0x6E;
    private const int VK_DIVIDE = 0x6F;
    private const int VK_F1 = 0x70;
    private const int VK_F2 = 0x71;
    private const int VK_F3 = 0x72;
    private const int VK_F4 = 0x73;
    private const int VK_F5 = 0x74;
    private const int VK_F6 = 0x75;
    private const int VK_F7 = 0x76;
    private const int VK_F8 = 0x77;
    private const int VK_F9 = 0x78;
    private const int VK_F10 = 0x79;
    private const int VK_F11 = 0x7A;
    private const int VK_F12 = 0x7B;
    private const int VK_F13 = 0x7C;
    private const int VK_F14 = 0x7D;
    private const int VK_F15 = 0x7E;
    private const int VK_F16 = 0x7F;
    private const int VK_F17 = 0x80;
    private const int VK_F18 = 0x81;
    private const int VK_F19 = 0x82;
    private const int VK_F20 = 0x83;
    private const int VK_F21 = 0x84;
    private const int VK_F22 = 0x85;
    private const int VK_F23 = 0x86;
    private const int VK_F24 = 0x87;
    private const int VK_NUMLOCK = 0x90;
    private const int VK_SCROLL = 0x91;
    private const int VK_OEM_NEC_EQUAL = 0x92;
    // private const int VK_OEM_FJ_JISHO = 0x92;
    private const int VK_OEM_FJ_MASSHOU = 0x93;
    private const int VK_OEM_FJ_TOUROKU = 0x94;
    private const int VK_OEM_FJ_LOYA = 0x95;
    private const int VK_OEM_FJ_ROYA = 0x96;
    private const int VK_LSHIFT = 0xA0;
    private const int VK_RSHIFT = 0xA1;
    private const int VK_LCONTROL = 0xA2;
    private const int VK_RCONTROL = 0xA3;
    private const int VK_RMENU = 0xA5;
    private const int VK_LMENU = 0xA4;
    private const int VK_BROWSER_BACK = 0xA6;
    private const int VK_BROWSER_FORWARD = 0xA7;
    private const int VK_BROWSER_REFRESH = 0xA8;
    private const int VK_BROWSER_STOP = 0xA9;
    private const int VK_BROWSER_SEARCH = 0xAA;
    private const int VK_BROWSER_FAVORITES = 0xAB;
    private const int VK_BROWSER_HOME = 0xAC;
    private const int VK_VOLUME_MUTE = 0xAD;
    private const int VK_VOLUME_DOWN = 0xAE;
    private const int VK_VOLUME_UP = 0xAF;
    private const int VK_MEDIA_NEXT_TRACK = 0xB0;
    private const int VK_MEDIA_PREV_TRACK = 0xB1;
    private const int VK_MEDIA_STOP = 0xB2;
    private const int VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const int VK_LAUNCH_MAIL = 0xB4;
    private const int VK_LAUNCH_MEDIA_SELECT = 0xB5;
    private const int VK_LAUNCH_APP1 = 0xB6;
    private const int VK_LAUNCH_APP2 = 0xB7;
    private const int VK_OEM_1 = 0xBA;
    private const int VK_OEM_PLUS = 0xBB;
    private const int VK_OEM_COMMA = 0xBC;
    private const int VK_OEM_MINUS = 0xBD;
    private const int VK_OEM_PERIOD = 0xBE;
    private const int VK_OEM_2 = 0xBF;
    private const int VK_OEM_3 = 0xC0;
    private const int VK_C1 = 0xC1;   // Brazilian ABNT_C1 key (not defined in winuser.h).
    private const int VK_C2 = 0xC2;   // Brazilian ABNT_C2 key (not defined in winuser.h).
    private const int VK_GAMEPAD_A = 0xC3;
    private const int VK_GAMEPAD_B = 0xC4;
    private const int VK_GAMEPAD_X = 0xC5;
    private const int VK_GAMEPAD_Y = 0xC6;
    private const int VK_GAMEPAD_RIGHT_SHOULDER = 0xC7;
    private const int VK_GAMEPAD_LEFT_SHOULDER = 0xC8;
    private const int VK_GAMEPAD_LEFT_TRIGGER = 0xC9;
    private const int VK_GAMEPAD_RIGHT_TRIGGER = 0xCA;
    private const int VK_GAMEPAD_DPAD_UP = 0xCB;
    private const int VK_GAMEPAD_DPAD_DOWN = 0xCC;
    private const int VK_GAMEPAD_DPAD_LEFT = 0xCD;
    private const int VK_GAMEPAD_DPAD_RIGHT = 0xCE;
    private const int VK_GAMEPAD_MENU = 0xCF;
    private const int VK_GAMEPAD_VIEW = 0xD0;
    private const int VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1;
    private const int VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2;
    private const int VK_GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3;
    private const int VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4;
    private const int VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5;
    private const int VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6;
    private const int VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7;
    private const int VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8;
    private const int VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9;
    private const int VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA;
    private const int VK_OEM_4 = 0xDB;
    private const int VK_OEM_5 = 0xDC;
    private const int VK_OEM_6 = 0xDD;
    private const int VK_OEM_7 = 0xDE;
    private const int VK_OEM_8 = 0xDF;
    private const int VK_OEM_AX = 0xE1;
    private const int VK_OEM_102 = 0xE2;
    private const int VK_ICO_HELP = 0xE3;
    private const int VK_ICO_00= 0xE4;
    private const int VK_PROCESSKEY = 0xE5;
    private const int VK_ICO_CLEAR = 0xE6;
    private const int VK_PACKET = 0xE7;
    private const int VK_OEM_RESET = 0xE9;
    private const int VK_OEM_JUMP = 0xEA;
    private const int VK_OEM_PA1 = 0xEB;
    private const int VK_OEM_PA2 = 0xEC;
    private const int VK_OEM_PA3 = 0xED;
    private const int VK_OEM_WSCTRL = 0xEE;
    private const int VK_OEM_CUSEL = 0xEF;
    private const int VK_OEM_ATTN = 0xF0;
    private const int VK_OEM_FINISH = 0xF1;
    private const int VK_OEM_COPY = 0xF2;
    private const int VK_OEM_AUTO = 0xF3;
    private const int VK_OEM_ENLW = 0xF4;
    private const int VK_OEM_BACKTAB = 0xF5;
    private const int VK_ATTN = 0xF6;
    private const int VK_CRSEL = 0xF7;
    private const int VK_EXSEL = 0xF8;
    private const int VK_EREOF = 0xF9;
    private const int VK_PLAY = 0xFA;
    private const int VK_ZOOM = 0xFB;
    private const int VK_NONAME = 0xFC;
    private const int VK_PA1 = 0xFD;
    private const int VK_OEM_CLEAR = 0xFE;
}
