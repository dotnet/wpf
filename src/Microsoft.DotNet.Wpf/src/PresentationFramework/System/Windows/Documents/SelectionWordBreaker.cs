// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Word breaker used for TextSelection's auto-word selection and
//              ctl-arrow navigation.
//

namespace System.Windows.Documents
{
    using MS.Win32;
    using MS.Internal; // Invariant

    // Word breaker used for TextSelection's auto-word selection and ctl-arrow
    // navigation.
    //
    // Unicode code points are broken down into classes, and with several exceptions
    // word breaks are defined as locations where the classes of two consequative
    // code points differ.
    //
    // This code is based on RichEdit's WB_MOVEWORDLEFT/RIGHT implementation.
    // It supports east-asian and european scripts, but not south-east asian
    // scripts such as Thai, Khmer, or Lao.
    internal static class SelectionWordBreaker
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Returns true if position points to a word break in the supplied
        // char array.  position is an inter-character offset -- 0 points
        // to the space preceeding the first char, 1 points between the
        // first and second char, etc.
        //
        // insideWordDirection specifies whether we're looking for a word start
        // or word end.  If insideWordDirection == LogicalDirection.Forward, then
        // text = "abc def", position = 4 will return true, but if the direction is
        // backward, no word boundary will be found (looking backward position is
        // at the edge of whitespace, not a word).
        //
        // This method requires at least MinContextLength chars ahead of and
        // following position to give accurate results, but no more.
        internal static bool IsAtWordBoundary(char[] text, int position, LogicalDirection insideWordDirection)
        {
            CharClass[] classes = GetClasses(text);

            // If the inside text is blank, it's not a word boundary.
            if (insideWordDirection == LogicalDirection.Backward)
            {
                if (position == text.Length)
                {
                    return true;
                }
                if (position == 0 || IsWhiteSpace(text[position - 1], classes[position - 1]))
                {
                    return false;
                }
            }
            else
            {
                if (position == 0)
                {
                    return true;
                }
                if (position == text.Length || IsWhiteSpace(text[position], classes[position]))
                {
                    return false;
                }
            }

            UInt16[] charType3 = new UInt16[2];

            SafeNativeMethods.GetStringTypeEx(0 /* ignored */, SafeNativeMethods.CT_CTYPE3, new char[] { text[position - 1], text[position] }, 2, charType3);

            // Otherwise we're at a word boundary if the classes of the surrounding text differ.
            return IsWordBoundary(text[position - 1], text[position]) ||
                   (
                    !IsSameClass(charType3[0], classes[position - 1], charType3[1], classes[position]) &&
                    !IsMidLetter(text, position - 1, classes) &&
                    !IsMidLetter(text, position, classes) 
                   );
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        // The minimum char count required to give accurate word breaking
        // results.
        //
        // This value specifies the count in each direction, so in general
        // calls to IsAtWordBoundary will require at least MinContextLength*2
        // chars surrounding the test position.
        internal static int MinContextLength
        {
            get
            {
                return 2;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        // Returns true if the position between a pair of consequative chars is
        // always a word break.
        private static bool IsWordBoundary(char previousChar, char followingChar)
        {
            bool isWordBoundary = false;

            if (followingChar == CarriageReturnChar)
            {
                // xxCR
                isWordBoundary = true;
            }

            return isWordBoundary;
        }

        // Returns true if the char specified by index is a MidLetter as defined
        // by the Unicode Standard Annex #29.  (Actually we use a subset of all
        // possible MidLetter values.)
        //
        // MidLetters are exceptions to the rule that consequative characters
        // with different classes are word breaks.
        private static bool IsMidLetter(char []text, int index, CharClass []classes)
        {
            Invariant.Assert(text.Length == classes.Length);
            return (text[index] == ApostropheChar || text[index] == RightSingleQuotationChar || text[index] == SoftHyphenChar) &&
                   (index > 0 && index + 1 < classes.Length) &&
                   ((classes[index - 1] == CharClass.Alphanumeric && classes[index + 1] == CharClass.Alphanumeric) ||
                    (text[index] == QuotationMarkChar && IsHebrew(text[index - 1]) && IsHebrew(text[index + 1])));
        }

        // Returns true if the specified C3 type matches an east-asian code point.
        private static bool IsIdeographicCharType(UInt16 charType3)
        {
            return (charType3 & (SafeNativeMethods.C3_KATAKANA | SafeNativeMethods.C3_HIRAGANA | SafeNativeMethods.C3_IDEOGRAPH)) != 0;
        }

        // Return true if two chars are in the same class.
        // Ideographic chars are a special case -- each is considered to be
        // unique except for several exceptions in japanese.
        private static bool IsSameClass(UInt16 preceedingType3, CharClass preceedingClass,
                                        UInt16 followingType3, CharClass followingClass)
        {
            const UInt16 IdeographicKanaTypes = SafeNativeMethods.C3_HALFWIDTH | SafeNativeMethods.C3_FULLWIDTH | SafeNativeMethods.C3_KATAKANA | SafeNativeMethods.C3_HIRAGANA;
            const UInt16 IdeographicTypes = IdeographicKanaTypes | SafeNativeMethods.C3_IDEOGRAPH;
            bool isSameClass;

            // Assume just one of the two chars is ideographic, in which case
            // the chars are not in the same class.
            isSameClass = false;

            if (IsIdeographicCharType(preceedingType3) && IsIdeographicCharType(followingType3))
            {
                // Both chars are ideographic.

                UInt16 typeDelta = (UInt16)((preceedingType3 & IdeographicTypes) ^ (followingType3 & IdeographicTypes));

                // Only a few japanese ideographic chars are considered the same class.
                isSameClass = (preceedingType3 & IdeographicKanaTypes) != 0 &&
                              (typeDelta == 0 ||
                               typeDelta == SafeNativeMethods.C3_FULLWIDTH ||
                               typeDelta == SafeNativeMethods.C3_HIRAGANA ||
                               typeDelta == (SafeNativeMethods.C3_FULLWIDTH | SafeNativeMethods.C3_HIRAGANA));
            }
            else if (!IsIdeographicCharType(preceedingType3) && !IsIdeographicCharType(followingType3))
            {
                // Neither char is ideographic.
                isSameClass = (preceedingClass & CharClass.WBF_CLASS) == (followingClass & CharClass.WBF_CLASS);
            }

            return isSameClass;
        }

        // Returns true is the specified char is whitespace.
        private static bool IsWhiteSpace(char ch, CharClass charClass)
        {
            return (charClass & CharClass.WBF_CLASS) == CharClass.Blank && ch != ObjectReplacementChar;
        }

        // Computes the character classes for each char of an array of text.
        private static CharClass[] GetClasses(char[] text)
        {
            CharClass[] classes = new CharClass[text.Length];

            for (int i = 0; i < text.Length; i++)
            {
                CharClass classification;
                char ch = text[i];

                if (ch < 0x0100)
                {
                    classification = (CharClass)_latinClasses[ch];
                }
                else if (IsKorean(ch))
                {
                    classification = CharClass.Alphanumeric;
                }
                else if (IsThai(ch))
                {
                    classification = CharClass.Alphanumeric;
                }
                else if (ch == ObjectReplacementChar)
                {
                    classification = CharClass.Blank | CharClass.WBF_BREAKAFTER;
                }
                else
                {
                    UInt16[] charType1 = new UInt16[1];

                    SafeNativeMethods.GetStringTypeEx(0 /* ignored */, SafeNativeMethods.CT_CTYPE1, new char[] { ch }, 1, charType1);

                    if ((charType1[0] & SafeNativeMethods.C1_SPACE) != 0)
                    {
                        if ((charType1[0] & SafeNativeMethods.C1_BLANK) != 0)
                        {
                            classification = CharClass.Blank | CharClass.WBF_ISWHITE;
                        }
                        else
                        {
                            classification = CharClass.WhiteSpace | CharClass.WBF_ISWHITE;
                        }
                    }
                    else if ((charType1[0] & SafeNativeMethods.C1_PUNCT) != 0 && !IsDiacriticOrKashida(ch))
                    {
                        classification = CharClass.Punctuation;
                    }
                    else
                    {
                        classification = CharClass.Alphanumeric;
                    }
                }

                classes[i] = classification;
            }

            return classes;
        }

        // Returns true if a char is a non-spacing diacritic or kashida.
        private static bool IsDiacriticOrKashida(char ch)
        {
            UInt16 []charType3 = new UInt16[1];

            SafeNativeMethods.GetStringTypeEx(0 /* ignored */, SafeNativeMethods.CT_CTYPE3, new char[] { ch }, 1, charType3);

            return (charType3[0] & (SafeNativeMethods.C3_DIACRITIC | SafeNativeMethods.C3_NONSPACING | SafeNativeMethods.C3_VOWELMARK | SafeNativeMethods.C3_KASHIDA)) != 0;
        }

        // Returns true if a character falls within a specified code point range.
        private static bool IsInRange(uint lower, char ch, uint upper)
        {
            return (lower <= (uint)ch && (uint)ch <= upper);
        }

        // Returns true if the specified char is a Korean char.
        private static bool IsKorean(char ch)
        {
            return IsInRange(0xac00, ch, 0xd7ff);
        }

        // Returns true if the specified char is a Thai char.
        private static bool IsThai(char ch)
        {
            return IsInRange(0x0e00, ch, 0x0e7f);
        }

        // Returns true if the specified char is a Hebrew char.
        private static bool IsHebrew(char ch)
        {
            return IsInRange(0x05d0, ch, 0x05f2);
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // Unicode line feed char.
        const char LineFeedChar = (char)0x000a;
        // Unicode carriage return char.
        const char CarriageReturnChar = (char)0x000d;
        // Unicode quotation mark char.
        const char QuotationMarkChar = (char)0x0022;
        // Unicode apostrophe char.
        const char ApostropheChar = (char)0x0027;
        // Unicode soft hyphen char.
        const char SoftHyphenChar = (char)0x00ad;
        // Unicode right single quotation char.
        const char RightSingleQuotationChar = (char)0x2019;
        // Unicode object replacement char.
        private const char ObjectReplacementChar = (char)0xfffc;

        // A sub-set of the GetStringTypeEx C1 char classifications.
        [Flags]
        private enum CharClass : byte
        {
            // Low-order nibble is classification.
            Alphanumeric = 0,
            Punctuation = 1,
            Blank = 2,
            WhiteSpace = 4,
            // High-order nibble holds attributes (matching rich edit's documented WBF flags).
            WBF_CLASS = 0xf,        // Mask for low order nibble.
            WBF_ISWHITE = 0x10,     // Whitespace char.
            WBF_BREAKAFTER = 0x40,  // Break char.
        }

        // Character classifications for u+0000 - u+00ff.
        static readonly byte []_latinClasses = new byte[] {
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, //0x00
        0x00, 0x13, 0x14, 0x14, 0x14, 0x14, 0x00, 0x00, //0x08
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x10
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x18
        0x32, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, //0x20
        0x01, 0x01, 0x01, 0x01, 0x01, 0x41, 0x01, 0x01, //0x28
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x30
        0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, //0x38
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x40
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x48
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x50
        0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x01, //0x58
        0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x60
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x68
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x70
        0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01, 0x00, //0x78
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x80
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x88
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x90
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0x98
        0x12, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, //0xA0
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, //0xA8
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, //0xB0
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, //0xB8
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0xC0
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0xC8
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, //0xD0
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0xD8
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0xE0
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0xE8
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, //0xF0
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};//0xF8

        #endregion Private Fields
    }
}
