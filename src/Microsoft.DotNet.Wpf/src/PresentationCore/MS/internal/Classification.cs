// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Unicode classification entry point
//
//

using System;
using System.Diagnostics;
using MS.Internal;
using System.Windows;
using System.Security;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Media.TextFormatting;
using MS.Internal.PresentationCore;

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

        static private ClassificationUtility _classificationUtilityInstance = new ClassificationUtility();

        static internal ClassificationUtility Instance
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
                                    out bool isStrong
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

        ///<SecurityNote>
        /// Critical - as this code performs an elevation. 
        ///</SecurityNote>
        [SecurityCritical]
        [SuppressUnmanagedCodeSecurity]
        [DllImport(DllImport.PresentationNative, EntryPoint="MILGetClassificationTables")]
        internal static extern void MILGetClassificationTables(out RawClassificationTables ct);
        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and retrieves pointers that it stores locally
        ///    The pointers retrieved are not validated for correctness and they are later dereferenced.
        ///    TreatAsSafe: The constructor is safe since it simply stores these pointers. The risk here 
        ///    in the future is not of these pointers being spoofed since they are not settable from outside.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        static Classification()
        {
            unsafe 
            {
                RawClassificationTables ct = new RawClassificationTables();
                MILGetClassificationTables(out ct);

                _unicodeClassTable   = new SecurityCriticalData<IntPtr>(ct.UnicodeClasses);
                _charAttributeTable  = new SecurityCriticalData<IntPtr>(ct.CharacterAttributes);
                _mirroredCharTable   = new SecurityCriticalData<IntPtr>(ct.Mirroring);
                
                _combiningMarksClassification = new SecurityCriticalData<CombiningMarksClassificationData>(ct.CombiningMarksClassification);
            }
        }

        /// <summary>
        /// Lookup Unicode character class for a Unicode UTF16 value
        /// </summary>
        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and dereferences a location in
        ///    a prepopulated Array. The risk is you might derefence a bogus memory
        ///    location. 
        ///    TreatAsSafe: This code is ok since it reduces codepoint to one of 256 possible
        ///    values and will always succeed. Also this information is ok to expose.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        static public short GetUnicodeClassUTF16(char codepoint)
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
        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and derefences a pointer retrieved from unmanaged code
        ///    TreatAsSafe: There is bounds checking in place and this dereferences a valid structure which
        ///    is guaranteed to be populated
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        static public short GetUnicodeClass(int unicodeScalar)
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
        /// Compute Unicode scalar value from unicode codepoint stream
        /// </summary>
        static internal int UnicodeScalar(
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
        /// <SecurityNote>
        ///    Critical: This code acceses a function call that returns a pointer (get_CharAttributeTable).
        ///    It trusts the value passed in to derfence the table with no implicit bounds or validity checks.
        ///    TreatAsSafe: This information is safe to expose at the same time the unicodeScalar passed in
        ///    is validated for bounds
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        static public bool IsCombining(int unicodeScalar)
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
        /// <SecurityNote>
        ///    Critical: This code acceses a function call that returns a pointer (get_CharAttributeTable).
        ///    It trusts the value passed in to derfence the table with no implicit bounds or validity checks.
        ///    TreatAsSafe: This information is safe to expose at the same time the unicodeScalar passed in
        ///    is validated for bounds
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        static public bool IsJoiner(int unicodeScalar)
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
        static public bool IsIVS(int unicodeScalar)
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
        /// <SecurityNote>
        ///    Critical: This code acceses a function call that returns a pointer (get_CharAttributeTable).
        ///    It keeps accesing a buffer with no validation in terms of the variables passed in. 
        ///    TreatAsSafe: This information is safe to expose, as in the worst case it tells you information
        ///    of where the next UTF16 character is. Also the constructor for characterbuffer can be one of three
        ///    a string, a char array or an unmanaged char*. The third case is critical and tightly controlled
        ///    so the risk of bogus length is significantly mitigated.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]
        static public int AdvanceUntilUTF16(
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
        /// <SecurityNote>
        ///    Critical: This code acceses a function call that returns a pointer (get_CharAttributeTable). It acceses
        ///    elements in an array with no type checking.
        ///    TreatAsSafe: This code exposes the index of the next non UTF16 character in a run. This is ok to expose
        ///    Also the calls to CharBuffer and CahrAttribute do the requisite bounds checking.
        /// </SecurityNote>
        [SecurityCritical,SecurityTreatAsSafe]        
        static public int AdvanceWhile(
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

        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and returns a pointer
        /// </SecurityNote>
        private static unsafe short*** UnicodeClassTable
        {
            [SecurityCritical]
            get { return (short***)_unicodeClassTable.Value; }
        }
        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and returns a pointer
        /// </SecurityNote>
        private static unsafe CharacterAttribute* CharAttributeTable
        {
            [SecurityCritical]
            get { return (CharacterAttribute*)_charAttributeTable.Value; }
        }

        /// <SecurityNote>
        ///    Critical: This accesses unsafe code and indexes into an array
        ///    Safe    : This method does bound check on the input char class.
        /// </SecurityNote>
        [SecurityCritical, SecurityTreatAsSafe]
        internal static CharacterAttribute CharAttributeOf(int charClass)
        {   
            unsafe 
            { 
                Invariant.Assert(charClass >= 0 && charClass < (int) UnicodeClass.Max);
                return CharAttributeTable[charClass]; 
            }
        }

        static private readonly SecurityCriticalData<IntPtr>  _unicodeClassTable;
        static private readonly SecurityCriticalData<IntPtr> _charAttributeTable;
        static private readonly SecurityCriticalData<IntPtr> _mirroredCharTable;
        static private readonly SecurityCriticalData<CombiningMarksClassificationData> _combiningMarksClassification;
    }
}
