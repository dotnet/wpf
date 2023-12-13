// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.ComponentModel;
using System.Windows.Markup;

namespace System.Windows.Input 
{
    /// <summary>
    ///     An enumeration of all of the possible key values on a keyboard.
    /// </summary>
    [TypeConverter(typeof(KeyConverter))]
    [ValueSerializer(typeof(KeyValueSerializer))]
    public enum Key
    {
        /// <summary>
        ///     No key pressed.
        /// </summary>
        None,

        /// <summary>
        ///     The CANCEL key.
        /// </summary>
        Cancel,

        /// <summary>
        ///     The BACKSPACE key.
        /// </summary>
        Back,

        /// <summary>
        ///     The TAB key.
        /// </summary>
        Tab,

        /// <summary>
        ///     The LineFeed key.
        /// </summary>
        LineFeed,

        /// <summary>
        ///     The CLEAR key.
        /// </summary>
        Clear,

        /// <summary>
        ///     The RETURN key.
        /// </summary>
        Return,

        /// <summary>
        ///     The ENTER key.
        /// </summary>
        Enter          = Return,
        
        /// <summary>
        ///     The PAUSE key.
        /// </summary>
        Pause,
        
        /// <summary>
        ///     The CAPS LOCK key.
        /// </summary>
        Capital,
        
        /// <summary>
        ///     The CAPS LOCK key.
        /// </summary>
        CapsLock       = Capital,
        
        /// <summary>
        ///     The IME Kana mode key.
        /// </summary>
        KanaMode,
        
        /// <summary>
        ///     The IME Hangul mode key.
        /// </summary>
        HangulMode    = KanaMode,
        
        /// <summary>
        ///     The IME Junja mode key.
        /// </summary>
        JunjaMode,
        
        /// <summary>
        ///     The IME Final mode key.
        /// </summary>
        FinalMode,
        
        /// <summary>
        ///     The IME Hanja mode key.
        /// </summary>
        HanjaMode,
        
        /// <summary>
        ///     The IME Kanji mode key.
        /// </summary>
        KanjiMode     = HanjaMode,
        
        /// <summary>
        ///     The ESC key.
        /// </summary>
        Escape,
        
        /// <summary>
        ///     The IME Convert key.
        /// </summary>
        ImeConvert,
        
        /// <summary>
        ///     The IME NonConvert key.
        /// </summary>
        ImeNonConvert,
        
        /// <summary>
        ///     The IME Accept key.
        /// </summary>
        ImeAccept, 
        
        /// <summary>
        ///     The IME Mode change request.
        /// </summary>
        ImeModeChange,
        
        /// <summary>
        ///     The SPACEBAR key.
        /// </summary>
        Space,
        
        /// <summary>
        ///     The PAGE UP key.
        /// </summary>
        Prior,
        
        /// <summary>
        ///     The PAGE UP key.
        /// </summary>
        PageUp         = Prior,
        
        /// <summary>
        ///     The PAGE DOWN key.
        /// </summary>
        Next,
        
        /// <summary>
        ///     The PAGE DOWN key.
        /// </summary>
        PageDown       = Next,
        
        /// <summary>
        ///     The END key.
        /// </summary>
        End,
        
        /// <summary>
        ///     The HOME key.
        /// </summary>
        Home,
        
        /// <summary>
        ///     The LEFT ARROW key.
        /// </summary>
        Left,
        
        /// <summary>
        ///     The UP ARROW key.
        /// </summary>
        Up,
        
        /// <summary>
        ///     The RIGHT ARROW key.
        /// </summary>
        Right,
        
        /// <summary>
        ///     The DOWN ARROW key.
        /// </summary>
        Down,
        
        /// <summary>
        ///     The SELECT key.
        /// </summary>
        Select,
        
        /// <summary>
        ///     The PRINT key.
        /// </summary>
        Print,
        
        /// <summary>
        ///     The EXECUTE key.
        /// </summary>
        Execute,
        
        /// <summary>
        ///     The PRINT SCREEN key.
        /// </summary>
        Snapshot,
        
        /// <summary>
        ///     The PRINT SCREEN key.
        /// </summary>
        PrintScreen    = Snapshot,
        
        /// <summary>
        ///     The INS key.
        /// </summary>
        Insert,
        
        /// <summary>
        ///     The DEL key.
        /// </summary>
        Delete,
        
        /// <summary>
        ///     The HELP key.
        /// </summary>
        Help,
        
        /// <summary>
        ///     The 0 key.
        /// </summary>
        D0, // 0
        
        /// <summary>
        ///     The 1 key.
        /// </summary>
        D1, // 1
        
        /// <summary>
        ///     The 2 key.
        /// </summary>
        D2, // 2
        
        /// <summary>
        ///     The 3 key.
        /// </summary>
        D3, // 3
        
        /// <summary>
        ///     The 4 key.
        /// </summary>
        D4, // 4
        
        /// <summary>
        ///     The 5 key.
        /// </summary>
        D5, // 5
        
        /// <summary>
        ///     The 6 key.
        /// </summary>
        D6, // 6
        
        /// <summary>
        ///     The 7 key.
        /// </summary>
        D7, // 7
        
        /// <summary>
        ///     The 8 key.
        /// </summary>
        D8, // 8
        
        /// <summary>
        ///     The 9 key.
        /// </summary>
        D9, // 9
        
        /// <summary>
        ///     The A key.
        /// </summary>
        A,
        
        /// <summary>
        ///     The B key.
        /// </summary>
        B,
        
        /// <summary>
        ///     The C key.
        /// </summary>
        C,
        
        /// <summary>
        ///     The D key.
        /// </summary>
        D,
        
        /// <summary>
        ///     The E key.
        /// </summary>
        E,
        
        /// <summary>
        ///     The F key.
        /// </summary>
        F,
        
        /// <summary>
        ///     The G key.
        /// </summary>
        G,
        
        /// <summary>
        ///     The H key.
        /// </summary>
        H,
        
        /// <summary>
        ///     The I key.
        /// </summary>
        I,
        
        /// <summary>
        ///     The J key.
        /// </summary>
        J,
        
        /// <summary>
        ///     The K key.
        /// </summary>
        K,
        
        /// <summary>
        ///     The L key.
        /// </summary>
        L,
        
        /// <summary>
        ///     The M key.
        /// </summary>
        M,
        
        /// <summary>
        ///     The N key.
        /// </summary>
        N,
        
        /// <summary>
        ///     The O key.
        /// </summary>
        O,
        
        /// <summary>
        ///     The P key.
        /// </summary>
        P,
        
        /// <summary>
        ///     The Q key.
        /// </summary>
        Q,
        
        /// <summary>
        ///     The R key.
        /// </summary>
        R,
        
        /// <summary>
        ///     The S key.
        /// </summary>
        S,
        
        /// <summary>
        ///     The T key.
        /// </summary>
        T,
        
        /// <summary>
        ///     The U key.
        /// </summary>
        U,
        
        /// <summary>
        ///     The V key.
        /// </summary>
        V,
        
        /// <summary>
        ///     The W key.
        /// </summary>
        W,
        
        /// <summary>
        ///     The X key.
        /// </summary>
        X,
        
        /// <summary>
        ///     The Y key.
        /// </summary>
        Y,
        
        /// <summary>
        ///     The Z key.
        /// </summary>
        Z,
        
        /// <summary>
        ///     The left Windows logo key (Microsoft Natural Keyboard).
        /// </summary>
        LWin,
        
        /// <summary>
        ///     The right Windows logo key (Microsoft Natural Keyboard).
        /// </summary>
        RWin,
        
        /// <summary>
        ///     The Application key (Microsoft Natural Keyboard).
        /// </summary>
        Apps,

        // Missing VK_POWER?

        /// <summary>
        ///     The Computer Sleep key.
        /// </summary>
        Sleep,
        
        /// <summary>
        ///     The 0 key on the numeric keypad.
        /// </summary>
        NumPad0,
        
        /// <summary>
        ///     The 1 key on the numeric keypad.
        /// </summary>
        NumPad1,
        
        /// <summary>
        ///     The 2 key on the numeric keypad.
        /// </summary>
        NumPad2,
        
        /// <summary>
        ///     The 3 key on the numeric keypad.
        /// </summary>
        NumPad3,
        
        /// <summary>
        ///     The 4 key on the numeric keypad.
        /// </summary>
        NumPad4,
        
        /// <summary>
        ///     The 5 key on the numeric keypad.
        /// </summary>
        NumPad5,
        
        /// <summary>
        ///     The 6 key on the numeric keypad.
        /// </summary>
        NumPad6,
        
        /// <summary>
        ///     The 7 key on the numeric keypad.
        /// </summary>
        NumPad7,
        
        /// <summary>
        ///     The 8 key on the numeric keypad.
        /// </summary>
        NumPad8,
        
        /// <summary>
        ///     The 9 key on the numeric keypad.
        /// </summary>
        NumPad9,
        
        /// <summary>
        ///     The Multiply key.
        /// </summary>
        Multiply,
        
        /// <summary>
        ///     The Add key.
        /// </summary>
        Add,
        
        /// <summary>
        ///     The Separator key.
        /// </summary>
        Separator,
        
        /// <summary>
        ///     The Subtract key.
        /// </summary>
        Subtract,
        
        /// <summary>
        ///     The Decimal key.
        /// </summary>
        Decimal,
        
        /// <summary>
        ///     The Divide key.
        /// </summary>
        Divide,
        
        /// <summary>
        ///     The F1 key.
        /// </summary>
        F1,
        
        /// <summary>
        ///     The F2 key.
        /// </summary>
        F2,
        
        /// <summary>
        ///     The F3 key.
        /// </summary>
        F3,
        
        /// <summary>
        ///     The F4 key.
        /// </summary>
        F4,
        
        /// <summary>
        ///     The F5 key.
        /// </summary>
        F5,
        
        /// <summary>
        ///     The F6 key.
        /// </summary>
        F6,
        
        /// <summary>
        ///     The F7 key.
        /// </summary>
        F7,
        
        /// <summary>
        ///     The F8 key.
        /// </summary>
        F8,
        
        /// <summary>
        ///     The F9 key.
        /// </summary>
        F9,
        
        /// <summary>
        ///     The F10 key.
        /// </summary>
        F10,
        
        /// <summary>
        ///     The F11 key.
        /// </summary>
        F11,
        
        /// <summary>
        ///     The F12 key.
        /// </summary>
        F12,
        
        /// <summary>
        ///     The F13 key.
        /// </summary>
        F13,
        
        /// <summary>
        ///     The F14 key.
        /// </summary>
        F14,
        
        /// <summary>
        ///     The F15 key.
        /// </summary>
        F15,
        
        /// <summary>
        ///     The F16 key.
        /// </summary>
        F16,
        
        /// <summary>
        ///     The F17 key.
        /// </summary>
        F17,
        
        /// <summary>
        ///     The F18 key.
        /// </summary>
        F18,
        
        /// <summary>
        ///     The F19 key.
        /// </summary>
        F19,
        
        /// <summary>
        ///     The F20 key.
        /// </summary>
        F20,
        
        /// <summary>
        ///     The F21 key.
        /// </summary>
        F21,
        
        /// <summary>
        ///     The F22 key.
        /// </summary>
        F22,
        
        /// <summary>
        ///     The F23 key.
        /// </summary>
        F23,
        
        /// <summary>
        ///     The F24 key.
        /// </summary>
        F24,
        
        /// <summary>
        ///     The NUM LOCK key.
        /// </summary>
        NumLock,
        
        /// <summary>
        ///     The SCROLL LOCK key.
        /// </summary>
        Scroll,
        
        /// <summary>
        ///     The left SHIFT key.
        /// </summary>
        LeftShift,
        
        /// <summary>
        ///     The right SHIFT key.
        /// </summary>
        RightShift, 
        
        /// <summary>
        ///     The left CTRL key.
        /// </summary>
        LeftCtrl, 
        
        /// <summary>
        ///     The right CTRL key.
        /// </summary>
        RightCtrl,
        
        /// <summary>
        ///     The left ALT key.
        /// </summary>
        LeftAlt,  
        
        /// <summary>
        ///     The right ALT key.
        /// </summary>
        RightAlt,
        
        /// <summary>
        ///     The Browser Back key.
        /// </summary>
        BrowserBack,
        
        /// <summary>
        ///     The Browser Forward key.
        /// </summary>
        BrowserForward,
        
        /// <summary>
        ///     The Browser Refresh key.
        /// </summary>
        BrowserRefresh,
        
        /// <summary>
        ///     The Browser Stop key.
        /// </summary>
        BrowserStop,
        
        /// <summary>
        ///     The Browser Search key.
        /// </summary>
        BrowserSearch,
        
        /// <summary>
        ///     The Browser Favorites key.
        /// </summary>
        BrowserFavorites,
        
        /// <summary>
        ///     The Browser Home key.
        /// </summary>
        BrowserHome,
        
        /// <summary>
        ///     The Volume Mute key.
        /// </summary>
        VolumeMute,
        
        /// <summary>
        ///     The Volume Down key.
        /// </summary>
        VolumeDown,
        
        /// <summary>
        ///     The Volume Up key.
        /// </summary>
        VolumeUp,
        
        /// <summary>
        ///     The Media Next Track key.
        /// </summary>
        MediaNextTrack,
        
        /// <summary>
        ///     The Media Previous Track key.
        /// </summary>
        MediaPreviousTrack,
        
        /// <summary>
        ///     The Media Stop key.
        /// </summary>
        MediaStop,
        
        /// <summary>
        ///     The Media Play Pause key.
        /// </summary>
        MediaPlayPause,
        
        /// <summary>
        ///     The Launch Mail key.
        /// </summary>
        LaunchMail,
        
        /// <summary>
        ///     The Select Media key.
        /// </summary>
        SelectMedia,
        
        /// <summary>
        ///     The Launch Application1 key.
        /// </summary>
        LaunchApplication1,
        
        /// <summary>
        ///     The Launch Application2 key.
        /// </summary>
        LaunchApplication2,
        
        /// <summary>
        ///     The Oem 1 key.
        /// </summary>
        Oem1,

        /// <summary>
        ///     The Oem Semicolon key.
        /// </summary>
        OemSemicolon  = Oem1,
        
        /// <summary>
        ///     The Oem plus key.
        /// </summary>
        OemPlus,
        
        /// <summary>
        ///     The Oem comma key.
        /// </summary>
        OemComma,
        
        /// <summary>
        ///     The Oem Minus key.
        /// </summary>
        OemMinus,
        
        /// <summary>
        ///     The Oem Period key.
        /// </summary>
        OemPeriod,
        
        /// <summary>
        ///     The Oem 2 key.
        /// </summary>
        Oem2,
        
        /// <summary>
        ///     The Oem Question key.
        /// </summary>
        OemQuestion   = Oem2,
        
        /// <summary>
        ///     The Oem 3 key.
        /// </summary>
        Oem3,

        /// <summary>
        ///     The Oem tilde key.
        /// </summary>
        OemTilde      = Oem3,

        /// <summary>
        ///     The ABNT_C1 Portuguese (Brazilian) key.
        /// </summary>
        AbntC1,

        /// <summary>
        ///     The ABNT_C2 Portuguese (Brazilian) key.
        /// </summary>
        AbntC2,
        
        /// <summary>
        ///     The Oem 4 key.
        /// </summary>
        Oem4,
        
        /// <summary>
        ///     The Oem Open Brackets key.
        /// </summary>
        OemOpenBrackets = Oem4,
        
        /// <summary>
        ///     The Oem 5 key.
        /// </summary>
        Oem5,
        
        /// <summary>
        ///     The Oem Pipe key.
        /// </summary>
        OemPipe       = Oem5,
        
        /// <summary>
        ///     The Oem 6 key.
        /// </summary>
        Oem6,
        
        /// <summary>
        ///     The Oem Close Brackets key.
        /// </summary>
        OemCloseBrackets = Oem6,
        
        /// <summary>
        ///     The Oem 7 key.
        /// </summary>
        Oem7,
        
        /// <summary>
        ///     The Oem Quotes key.
        /// </summary>
        OemQuotes     = Oem7,
        
        /// <summary>
        ///     The Oem8 key.
        /// </summary>
        Oem8,

        // Future: VK_OEM_AX = 0xE1
        
        /// <summary>
        ///     The Oem 102 key.
        /// </summary>
        Oem102,
        
        /// <summary>
        ///     The Oem Backslash key.
        /// </summary>
        OemBackslash  = Oem102,

        // Future: VK_ICO_HELP = 0xE3;
        // Future: VK_ICO_00 = 0xE4;
        
        /// <summary>
        ///     A special key masking the real key being processed by an IME.
        /// </summary>
        ImeProcessed,

        /// <summary>
        ///     A special key masking the real key being processed as a system key.
        /// </summary>
        System,
        
        // Future: VK_OEM_RESET = 0xE9;
        // Future: VK_OEM_JUMP = 0xEA;
        // Future: VK_OEM_PA1 = 0xEB;
        // Future: VK_OEM_PA2 = 0xEC;
        // Future: VK_OEM_PA3 = 0xED;
        // Future: VK_OEM_WSCTRL = 0xEE;
        // Future: VK_OEM_CUSEL = 0xEF;

        /// <summary>
        ///     The OEM_ATTN key.
        /// </summary>
        OemAttn,

        /// <summary>
        ///     The DBE_ALPHANUMERIC key.
        /// </summary>
        DbeAlphanumeric = OemAttn,

        /// <summary>
        ///     The OEM_FINISH key.
        /// </summary>
        OemFinish,

        /// <summary>
        ///     The DBE_KATAKANA key.
        /// </summary>
        DbeKatakana = OemFinish,

        /// <summary>
        ///     The OEM_COPY key.
        /// </summary>
        OemCopy,

        /// <summary>
        ///     The DBE_HIRAGANA key.
        /// </summary>
        DbeHiragana = OemCopy,

        /// <summary>
        ///     The OEM_AUTO key.
        /// </summary>
        OemAuto,

        /// <summary>
        ///     The DBE_SBCSCHAR key.
        /// </summary>
        DbeSbcsChar = OemAuto,

        /// <summary>
        ///     The OEM_ENLW key.
        /// </summary>
        OemEnlw,

        /// <summary>
        ///     The DBE_DBCSCHAR key.
        /// </summary>
        DbeDbcsChar = OemEnlw,

        /// <summary>
        ///     The OEM_BACKTAB key.
        /// </summary>
        OemBackTab,

        /// <summary>
        ///     The DBE_ROMAN key.
        /// </summary>
        DbeRoman = OemBackTab,

        /// <summary>
        ///     The ATTN key.
        /// </summary>
        Attn,

        /// <summary>
        ///     The DBE_NOROMAN key.
        /// </summary>
        DbeNoRoman = Attn,
        
        /// <summary>
        ///     The CRSEL key.
        /// </summary>
        CrSel,

        /// <summary>
        ///     The DBE_ENTERWORDREGISTERMODE key.
        /// </summary>
        DbeEnterWordRegisterMode = CrSel,
        
        /// <summary>
        ///     The EXSEL key.
        /// </summary>
        ExSel,

        /// <summary>
        ///     The DBE_ENTERIMECONFIGMODE key.
        /// </summary>
        DbeEnterImeConfigureMode = ExSel,
        
        /// <summary>
        ///     The ERASE EOF key.
        /// </summary>
        EraseEof,

        /// <summary>
        ///     The DBE_FLUSHSTRING key.
        /// </summary>
        DbeFlushString = EraseEof,
        
        /// <summary>
        ///     The PLAY key.
        /// </summary>
        Play,

        /// <summary>
        ///     The DBE_CODEINPUT key.
        /// </summary>
        DbeCodeInput = Play,
        
        /// <summary>
        ///     The ZOOM key.
        /// </summary>
        Zoom,

        /// <summary>
        ///     The DBE_NOCODEINPUT key.
        /// </summary>
        DbeNoCodeInput = Zoom,
        
        /// <summary>
        ///     A constant reserved for future use.
        /// </summary>
        NoName,

        /// <summary>
        ///     The DBE_DETERMINESTRING key.
        /// </summary>
        DbeDetermineString = NoName,
        
        /// <summary>
        ///     The PA1 key.
        /// </summary>
        Pa1,

        /// <summary>
        ///     The DBE_ENTERDLGCONVERSIONMODE key.
        /// </summary>
        DbeEnterDialogConversionMode = Pa1,
        
        /// <summary>
        ///     The CLEAR key.
        /// </summary>
        OemClear,

        /// <summary>
        ///  Indicates the key is part of a dead-key composition
        /// </summary>
        DeadCharProcessed,
    }
}
