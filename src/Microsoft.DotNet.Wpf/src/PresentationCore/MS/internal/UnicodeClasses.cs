// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This is automatic generated file.  Do not modify by hand.
// Script Generator: TablesGenerator
// Generated on Monday 3/19/2007 9:23:40 AM
//



using System.Runtime.InteropServices;


namespace MS.Internal
{
    internal enum DirectionClass : byte
    {
        Left                 = 0,             // Left character
        Right                = 1,             // Right character
        ArabicNumber         = 2,             // Arabic Number
        EuropeanNumber       = 3,             // European number
        ArabicLetter         = 4,             // Arabic Letter
        EuropeanSeparator    = 5,             // European separator
        CommonSeparator      = 6,             // Common separator
        EuropeanTerminator   = 7,             // European terminator
        NonSpacingMark       = 8,             // Non-spacing mark
        BoundaryNeutral      = 9,             // Boundary neutral
        GenericNeutral       = 10,            // Generic neutral, internal type
        ParagraphSeparator   = 11,            // Paragraph sperator
        LeftToRightEmbedding = 12,            // Left-to-right embedding
        LeftToRightOverride  = 13,            // Left-to-right override
        RightToLeftEmbedding = 14,            // Right-to-left embedding
        RightToLeftOverride  = 15,            // Right-to-left override
        PopDirectionalFormat = 16,            // Pop directional format character
        SegmentSeparator     = 17,            // Segment separator
        WhiteSpace           = 18,            // White space
        OtherNeutral         = 19,            // Other neutral
        ClassInvalid         = 20,            // Invalid classification mark, internal
        ClassMax             = 21
    };



    internal enum ItemClass : byte
    {
        DigitClass          = 0x0,
        ANClass             = 0x1,
        CSClass             = 0x2,
        ESClass             = 0x3,
        ETClass             = 0x4,
        StrongClass         = 0x5,
        WeakClass           = 0x6,
        SimpleMarkClass     = 0x7,
        ComplexMarkClass    = 0x8,
        ControlClass        = 0x9,
        JoinerClass         = 0xA,
        NumberSignClass     = 0xB,
        MaxClass            = 0xC
    };

    /// <summary>
    /// Script identifier mapped one-to-one to OpenType published script tag
    /// </summary>

    internal enum ScriptID : byte
    {
        Default             = 0x0,
        Arabic              = 0x1,
        Armenian            = 0x2,
        Bengali             = 0x3,
        Bopomofo            = 0x4,
        Braille             = 0x5,
        Buginese            = 0x6,
        Buhid               = 0x7,
        CanadianSyllabics   = 0x8,
        Cherokee            = 0x9,
        CJKIdeographic      = 0xA,
        Coptic              = 0xB,
        CypriotSyllabary    = 0xC,
        Cyrillic            = 0xD,
        Deseret             = 0xE,
        Devanagari          = 0xF,
        Ethiopic            = 0x10,
        Georgian            = 0x11,
        Glagolitic          = 0x12,
        Gothic              = 0x13,
        Greek               = 0x14,
        Gujarati            = 0x15,
        Gurmukhi            = 0x16,
        Hangul              = 0x17,
        Hanunoo             = 0x18,
        Hebrew              = 0x19,
        Kannada             = 0x1A,
        Kana                = 0x1B,
        Kharoshthi          = 0x1C,
        Khmer               = 0x1D,
        Lao                 = 0x1E,
        Latin               = 0x1F,
        Limbu               = 0x20,
        LinearB             = 0x21,
        Malayalam           = 0x22,
        MathematicalAlphanumericSymbols= 0x23,
        Mongolian           = 0x24,
        MusicalSymbols      = 0x25,
        Myanmar             = 0x26,
        NewTaiLue           = 0x27,
        Ogham               = 0x28,
        OldItalic           = 0x29,
        OldPersianCuneiform = 0x2A,
        Oriya               = 0x2B,
        Osmanya             = 0x2C,
        Runic               = 0x2D,
        Shavian             = 0x2E,
        Sinhala             = 0x2F,
        SylotiNagri         = 0x30,
        Syriac              = 0x31,
        Tagalog             = 0x32,
        Tagbanwa            = 0x33,
        TaiLe               = 0x34,
        Tamil               = 0x35,
        Telugu              = 0x36,
        Thaana              = 0x37,
        Thai                = 0x38,
        Tibetan             = 0x39,
        Tifinagh            = 0x3A,
        UgariticCuneiform   = 0x3B,
        Yi                  = 0x3C,
        Digit               = 0x3D,
        Control             = 0x3E,
        Mirror              = 0x3F,
        Max                 = 0x40
    };


    internal enum CharacterAttributeFlags : ushort
    {
        CharacterComplex    = 0x1,
        CharacterRTL        = 0x2,
        CharacterLineBreak  = 0x4,
        CharacterFormatAnchor= 0x8,
        CharacterFastText   = 0x10,
        CharacterIdeo       = 0x20,
        CharacterExtended   = 0x40,
        CharacterSpace      = 0x80,
        CharacterDigit      = 0x100,
        CharacterParaBreak  = 0x200,
        CharacterCRLF       = 0x400,
        CharacterLetter     = 0x800
    };


    internal enum CharBreakingType : byte
    {
        NoBreak             = 0x0,
        ControlBreak        = 0x1,
        DigitBreak          = 0x2,
        PairMirrorBreak     = 0x4,
        SingleMirrorBreak   = 0x8
    };

    internal enum UnicodeClass : ushort
    {
        Max = 0x1D8,
    };

    // CharacterAttribute is manged struct. we need to keep it word align because we use
    // equivalent unmanaged struct which should be same size. the unmanaged struct has same name
    // CharacterAttribute. in the code we have a pointer to the array of CharacterAttribute
    // and the compiler index this pointer using the size of that struct so it is important
    // to be sure both structures (managed and unmaganed) has same size.


    [StructLayout(LayoutKind.Sequential, Pack=1)]

    internal struct  CharacterAttribute
    {
        internal byte               Script;
        internal byte               ItemClass;
        internal ushort             Flags;
        internal byte               BreakType;
        internal DirectionClass     BiDi;
        internal short              LineBreak;
    };
} // namespace


