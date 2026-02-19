// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
//
//
//  Contents:  Unicode classification entry point
//
//

using System.Runtime.InteropServices;
using System.Windows.Media.TextFormatting;

namespace MS.Internal
{
    /// <summary>
    /// This class is used as a level on indirection for classes in managed c++ to be able to utilize methods
    /// from the static class Classification.
    /// We cannot make MC++ reference PresentationCore.dll since this will result in cirular reference.
    /// </summary>
    internal class ClassificationUtility : MS.Internal.Text.TextInterface.IClassification
    {
        // We have restored this list from WPF 3.x.
        // The original list can be found under
        // $/Dev10/pu/WPF/wpf/src/Core/CSharp/MS/Internal/Shaping/Script.cs
        internal static readonly bool[] ScriptCaretInfo = new bool[]
        {
            /* Default              */    false,
            /* Arabic               */    false,
            /* Armenian             */    false,
            /* Bengali              */    true,
            /* Bopomofo             */    false,
            /* Braille              */    false,
            /* Buginese             */    true,
            /* Buhid                */    false,
            /* CanadianSyllabics    */    false,
            /* Cherokee             */    false,
            /* CJKIdeographic       */    false,
            /* Coptic               */    false,
            /* CypriotSyllabary     */    false,
            /* Cyrillic             */    false,
            /* Deseret              */    false,
            /* Devanagari           */    true,
            /* Ethiopic             */    false,
            /* Georgian             */    false,
            /* Glagolitic           */    false,
            /* Gothic               */    false,
            /* Greek                */    false,
            /* Gujarati             */    true,
            /* Gurmukhi             */    true,
            /* Hangul               */    true,
            /* Hanunoo              */    false,
            /* Hebrew               */    true,
            /* Kannada              */    true,
            /* Kana                 */    false,
            /* Kharoshthi           */    true,
            /* Khmer                */    true,
            /* Lao                  */    true,
            /* Latin                */    false,
            /* Limbu                */    true,
            /* LinearB              */    false,
            /* Malayalam            */    true,
            /* MathematicalAlphanumericSymbols */ false,
            /* Mongolian            */    true,
            /* MusicalSymbols       */    false,
            /* Myanmar              */    true,
            /* NewTaiLue            */    true,
            /* Ogham                */    false,
            /* OldItalic            */    false,
            /* OldPersianCuneiform  */    false,
            /* Oriya                */    true,
            /* Osmanya              */    false,
            /* Runic                */    false,
            /* Shavian              */    false,
            /* Sinhala              */    true,
            /* SylotiNagri          */    true,
            /* Syriac               */    false,
            /* Tagalog              */    false,
            /* Tagbanwa             */    false,
            /* TaiLe                */    false,
            /* Tamil                */    true,
            /* Telugu               */    true,
            /* Thaana               */    true,
            /* Thai                 */    true,
            /* Tibetan              */    true,
            /* Tifinagh             */    false,
            /* UgariticCuneiform    */    false,
            /* Yi                   */    false,
            /* Digit                */    false,
            /* Control              */    false,
            /* Mirror               */    false,
        };

        private static ClassificationUtility _classificationUtilityInstance = new ClassificationUtility();

        internal static ClassificationUtility Instance
        {
            get
            {
                return _classificationUtilityInstance;
            }
        }

        public void GetCharAttribute(
                                    int unicodeScalar,
                                    out bool isCombining,
                                    out bool needsCaretInfo,
                                    out bool isIndic,
                                    out bool isDigit,
                                    out bool isLatin,
                                    out bool isStrong,
                                    out bool isScriptAgnosticCombining
                                    )
        {
            CharacterAttribute charAttribute = Classification.CharAttributeOf((int)Classification.GetUnicodeClass(unicodeScalar));

            byte itemClass = charAttribute.ItemClass;
            isCombining = (itemClass == (byte)ItemClass.SimpleMarkClass 
                        || itemClass == (byte)ItemClass.ComplexMarkClass
                        || Classification.IsIVS(unicodeScalar));

            isStrong = (itemClass == (byte)ItemClass.StrongClass);

            int script = charAttribute.Script;
            needsCaretInfo = ScriptCaretInfo[script];

            ScriptID scriptId = (ScriptID)script;
            isDigit = scriptId == ScriptID.Digit;
            isLatin = scriptId == ScriptID.Latin;
            if (isLatin)
            {
                isIndic = false;
            }
            else
            {
                isIndic = IsScriptIndic(scriptId);
            }

            isScriptAgnosticCombining = Classification.IsScriptAgnosticCombining(unicodeScalar);
        }

        /// <summary>
        /// Check whether two Unicode scalar values belong to the same script.
        /// </summary>
        public bool IsSameScript(int unicodeScalar1, int unicodeScalar2)
        {
            return Classification.IsSameScript(unicodeScalar1, unicodeScalar2);
        }

        /// <summary>
        /// Returns true if specified script is Indic.
        /// </summary>
        private static bool IsScriptIndic(ScriptID scriptId)
        {
            if (scriptId == ScriptID.Bengali
                 || scriptId == ScriptID.Devanagari
                 || scriptId == ScriptID.Gurmukhi
                 || scriptId == ScriptID.Gujarati
                 || scriptId == ScriptID.Kannada
                 || scriptId == ScriptID.Malayalam
                 || scriptId == ScriptID.Oriya
                 || scriptId == ScriptID.Tamil
                 || scriptId == ScriptID.Telugu)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Hold the classification table pointers. 
    /// </summary>    
    internal static class Classification
    {
        /// <summary>
        /// This structure has a cloned one in the unmanaged side. Doing any change in this
        /// structure should have the same change on unmanaged side too.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct CombiningMarksClassificationData
        {
            internal IntPtr CombiningCharsIndexes; // Two dimentional array of base char classes,
            internal int    CombiningCharsIndexesTableLength;
            internal int    CombiningCharsIndexesTableSegmentLength;
            
            internal IntPtr CombiningMarkIndexes; // Combining mark classes array, with length = length
            internal int    CombiningMarkIndexesTableLength;
            
            internal IntPtr CombinationChars; // Two dimentional array of combined characters
            internal int    CombinationCharsBaseCount;
            internal int    CombinationCharsMarkCount;
        }
        
        /// <summary>
        /// This structure has a cloned one in the unmanaged side. doing any change in  that
        /// structure should have same change in the unmanaged side too.
        /// </summary>    
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct RawClassificationTables
        {
            internal IntPtr UnicodeClasses;
            internal IntPtr CharacterAttributes;
            internal IntPtr Mirroring;
            internal CombiningMarksClassificationData CombiningMarksClassification;
        };

        [DllImport(DllImport.PresentationNative, EntryPoint="MILGetClassificationTables")]
        internal static extern void MILGetClassificationTables(out RawClassificationTables ct);
        static Classification()
        {
            unsafe 
            {
                RawClassificationTables ct = new RawClassificationTables();
                MILGetClassificationTables(out ct);

                _unicodeClassTable = ct.UnicodeClasses;
                _charAttributeTable = ct.CharacterAttributes;
                _mirroredCharTable = ct.Mirroring;
                _combiningMarksClassification = ct.CombiningMarksClassification;
            }
        }

        /// <summary>
        /// Lookup Unicode character class for a Unicode UTF16 value
        /// </summary>
        public static short GetUnicodeClassUTF16(char codepoint)
        {
            unsafe 
            {
                short **plane0 = UnicodeClassTable[0];
                Invariant.Assert((long)plane0 >= (long)UnicodeClass.Max);

                short* pcc = plane0[codepoint >> 8];
                return ((long) pcc < (long) UnicodeClass.Max ?
                    (short)pcc : pcc[codepoint & 0xFF]);
            }
        }


        /// <summary>
        /// Lookup Unicode character class for a Unicode scalar value
        /// </summary>
        public static short GetUnicodeClass(int unicodeScalar)
        {
            unsafe
            {
                Invariant.Assert(unicodeScalar >= 0 && unicodeScalar <= 0x10FFFF);
                short **ppcc = UnicodeClassTable[((unicodeScalar >> 16) & 0xFF) % 17];

                if ((long)ppcc < (long)UnicodeClass.Max)
                    return (short)ppcc;

                short *pcc = ppcc[(unicodeScalar & 0xFFFF) >> 8];

                if ((long)pcc < (long)UnicodeClass.Max)
                    return (short)pcc;

                return pcc[unicodeScalar & 0xFF];
            }
        }


        /// <summary>
        /// Check whether two Unicode scalar values belong to the same script
        /// </summary>
        public static bool IsSameScript(int unicodeScalar1, int unicodeScalar2)
        {
            unsafe
            {
                short unicodeClass1 = GetUnicodeClass(unicodeScalar1);
                short unicodeClass2 = GetUnicodeClass(unicodeScalar2);
                if (unicodeClass1 != unicodeClass2)
                {
                    CharacterAttribute a1 = Classification.CharAttributeTable[unicodeClass1];
                    CharacterAttribute a2 = Classification.CharAttributeTable[unicodeClass2];
                    if (a1.Script != a2.Script)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Check whether the character is a script-agnostic combining mark (font extender) that should
        /// stay with its base character regardless of script differences.
        /// </summary>
        /// <remarks>
        /// Corresponds to a subset of DWriteCore's is_font_extender predicate, covering characters
        /// that are not already handled by IsCombining + IsSameScript.  These are combining marks
        /// whose Unicode script is not the same as the base character's script, so that emoji
        /// sequences like "1️⃣" (digit + VS16 + U+20E3 combining enclosing keycap) stay together.
        /// <para>
        /// Note: ZWJ (U+200D) is NOT listed here because it is a JoinerClass character.
        /// IsCombining() returns false for it, so this function would never be reached for ZWJ.
        /// ZWJ is handled upstream by IsJoiner() and the prevWasJoiner logic in MapCharacters.
        /// </para>
        /// </remarks>
        public static bool IsScriptAgnosticCombining(int unicodeScalar)
        {
            // Variation Selectors VS1-VS16 (U+FE00-U+FE0F)
            if (unicodeScalar >= 0xFE00 && unicodeScalar <= 0xFE0F)
                return true;

            // Ideographic Variation Selectors VS17-VS256 (U+E0100-U+E01EF)
            if (IsIVS(unicodeScalar))
                return true;

            // Combining Diacritical Marks Extended (U+1AB0-U+1AFF)
            if (unicodeScalar >= 0x1AB0 && unicodeScalar <= 0x1AFF)
                return true;

            // Combining Diacritical Marks Supplement (U+1DC0-U+1DFF)
            if (unicodeScalar >= 0x1DC0 && unicodeScalar <= 0x1DFF)
                return true;

            // Combining Diacritical Marks for Symbols (U+20D0-U+20FF) - includes U+20E3 keycap
            if (unicodeScalar >= 0x20D0 && unicodeScalar <= 0x20FF)
                return true;

            // Combining Half Marks (U+FE20-U+FE2F)
            if (unicodeScalar >= 0xFE20 && unicodeScalar <= 0xFE2F)
                return true;

            // Emoji Modifiers / Skin tones (U+1F3FB-U+1F3FF)
            if (unicodeScalar >= 0x1F3FB && unicodeScalar <= 0x1F3FF)
                return true;

            return false;
        }

        /// <summary>
        /// Compute Unicode scalar value from unicode codepoint stream
        /// </summary>
        internal static int UnicodeScalar(
            CharacterBufferRange unicodeString,
            out int              sizeofChar
            )
        {
            Invariant.Assert(unicodeString.CharacterBuffer != null && unicodeString.Length > 0);

            int ch = unicodeString[0];
            sizeofChar = 1;

            if (    unicodeString.Length >= 2
                &&  (ch & 0xFC00) == 0xD800
                &&  (unicodeString[1] & 0xFC00) == 0xDC00
                )
            {
                ch = (((ch & 0x03FF) << 10) | (unicodeString[1] & 0x3FF)) + 0x10000;
                sizeofChar++;
            }

            return ch;
        }


        /// <summary>
        /// Check whether the character is combining mark
        /// </summary>
        public static bool IsCombining(int unicodeScalar)
        {
            unsafe
            {
                byte itemClass = Classification.CharAttributeTable[GetUnicodeClass(unicodeScalar)].ItemClass;

                return itemClass == (byte)ItemClass.SimpleMarkClass
                    || itemClass == (byte)ItemClass.ComplexMarkClass
                    || IsIVS(unicodeScalar);
            }
        }

        /// <summary>
        /// Check whether the character is a joiner character
        /// </summary>
        public static bool IsJoiner(int unicodeScalar)
        {
            unsafe
            {
                byte itemClass = Classification.CharAttributeTable[GetUnicodeClass(unicodeScalar)].ItemClass;
                
                return itemClass == (byte) ItemClass.JoinerClass;
            }
        }

        /// <summary>
        /// Check whether the character is an IVS selector character
        /// </summary>
        public static bool IsIVS(int unicodeScalar)
        {
            // An Ideographic Variation Sequence (IVS) is a sequence of two
            // coded characters, the first being a character with the
            // Unified_Ideograph property, the second being a variation
            // selector character in the range U+E0100 to U+E01EF.
            return unicodeScalar >= 0xE0100 && unicodeScalar <= 0xE01EF;
        }

        /// <summary>
        /// Scan UTF16 character string until a character with specified attributes is found
        /// </summary>
        /// <returns>character index of first character matching the attribute.</returns>
        public static int AdvanceUntilUTF16(
            CharacterBuffer     charBuffer,
            int                 offsetToFirstChar,
            int                 stringLength,
            ushort              mask,
            out ushort          charFlags
            )
        {
            int i = offsetToFirstChar;
            int limit = offsetToFirstChar + stringLength;
            charFlags = 0;

            while (i < limit)
            {
                unsafe
                {
                    ushort flags = (ushort)Classification.CharAttributeTable[(int)GetUnicodeClassUTF16(charBuffer[i])].Flags;

                    if((flags & mask) != 0)
                        break;

                    charFlags |= flags;
                }
                i++;
            }
            return i - offsetToFirstChar;
        }

        /// <summary>
        /// Scan character string until a character that is not the specified ItemClass is found
        /// </summary>
        /// <returns>character index of first character that is not the specified ItemClass</returns>
        public static int AdvanceWhile(
            CharacterBufferRange unicodeString, 
            ItemClass            itemClass 
            )
        {            
            int i     = 0;
            int limit = unicodeString.Length;
            int sizeofChar = 0; 
            
            while (i < limit)
            {
                int ch = Classification.UnicodeScalar(
                    new CharacterBufferRange(unicodeString, i, limit - i), 
                    out sizeofChar
                    ); 
            
                unsafe
                {
                    byte currentClass = (byte) Classification.CharAttributeTable[(int)GetUnicodeClass(ch)].ItemClass;
                    if (currentClass != (byte) itemClass)
                        break;
                }
                
                i += sizeofChar;
            }
            
            return i;
        }

        private static unsafe short*** UnicodeClassTable => (short***)_unicodeClassTable;

        private static unsafe CharacterAttribute* CharAttributeTable => (CharacterAttribute*)_charAttributeTable;

        internal static CharacterAttribute CharAttributeOf(int charClass)
        {
            unsafe
            {
                Invariant.Assert(charClass >= 0 && charClass < (int) UnicodeClass.Max);
                return CharAttributeTable[charClass]; 
            }
        }

        private static readonly IntPtr _unicodeClassTable;
        private static readonly IntPtr _charAttributeTable;
        private static readonly IntPtr _mirroredCharTable;
        private static readonly CombiningMarksClassificationData _combiningMarksClassification;
    }
}
