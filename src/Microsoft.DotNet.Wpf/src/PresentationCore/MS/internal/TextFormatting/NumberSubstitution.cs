// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal.FontCache;
using MS.Internal.FontFace;
using MS.Internal.Shaping;
using MS.Win32;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// DigitState contains the high-level logic used to convert the number culture implied by
    /// text run properties to the low-level digit culture used for shaping.
    /// </summary>
    internal class DigitState
    {
        /// <summary>
        /// DigitCulture gets a CultureInfo with the actual symbols and digits used for digit
        /// substitution. If no substitution is required, this property is null.
        /// </summary>
        internal CultureInfo DigitCulture
        {
            get { return _digitCulture; }
        }

        /// <summary>
        /// RequiresNumberSubstitution is true if digit substitution is required (DigitCulture != null)
        /// and false if digit substitution is not required (DigitCulture == null).
        /// </summary>
        internal bool RequiresNumberSubstitution
        {
            get { return _digitCulture != null; }
        }

        /// <summary>
        /// Contextual is true if contextual digit substitution is required. If so, DigitCulture specifies
        /// the digits to use in Arabic contexts. In non-Arabic contexts, null should be used as the
        /// digit culture.
        /// </summary>
        internal bool Contextual
        {
            get { return _contextual; }
        }

        /// <summary>
        /// Resolves number substitution method to one of following values:
        ///     European
        ///     Traditional
        ///     NativeNational
        /// </summary>
        internal static NumberSubstitutionMethod GetResolvedSubstitutionMethod(TextRunProperties properties, CultureInfo digitCulture, out bool ignoreUserOverride)
        {
            ignoreUserOverride = true;
            NumberSubstitutionMethod resolvedMethod = NumberSubstitutionMethod.European;

            if (digitCulture != null)
            {
                NumberSubstitutionMethod method;
                CultureInfo numberCulture = GetNumberCulture(properties, out method, out ignoreUserOverride);

                if (numberCulture != null)
                {
                    // First, disambiguate AsCulture method, which depends on digit substitution contained in CultureInfo.NumberFormat
                    if (method == NumberSubstitutionMethod.AsCulture)
                    {
                        switch (numberCulture.NumberFormat.DigitSubstitution)
                        {
                            case DigitShapes.Context:
                                method = NumberSubstitutionMethod.Context;
                                break;
                            case DigitShapes.NativeNational:
                                method = NumberSubstitutionMethod.NativeNational;
                                break;
                            default:
                                method = NumberSubstitutionMethod.European;
                                break;
                        }
                    }

                    // Next, disambiguate Context method, which maps to Traditional if digitCulture != null
                    resolvedMethod = method;
                    if (resolvedMethod == NumberSubstitutionMethod.Context)
                    {
                        resolvedMethod = NumberSubstitutionMethod.Traditional;
                    }
                }
            }
            
            return resolvedMethod;
        }

        /// <summary>
        /// SetTextRunProperties initializes the DigitCulture and Contextual properties to reflect the
        /// specified text run properties. 
        /// </summary>
        internal void SetTextRunProperties(TextRunProperties properties)
        {
            // Determine the number culture and substitution method.
            bool ignoreUserOverride;
            NumberSubstitutionMethod method;
            CultureInfo numberCulture = GetNumberCulture(properties, out method, out ignoreUserOverride);

            // The digit culture is a function of the number culture and the substitution method. Only
            // determine the digit culture if either of these two parameters change.
            if (!object.ReferenceEquals(numberCulture, _lastNumberCulture) || method != _lastMethod)
            {
                _lastNumberCulture = numberCulture;
                _lastMethod = method;

                _digitCulture = GetDigitCulture(numberCulture, method, out _contextual);
            }
        }

        private static CultureInfo GetNumberCulture(TextRunProperties properties, out NumberSubstitutionMethod method, out bool ignoreUserOverride)
        {
            ignoreUserOverride = true;
            NumberSubstitution sub = properties.NumberSubstitution;
            if (sub == null)
            {
                method = NumberSubstitutionMethod.AsCulture;
                return CultureMapper.GetSpecificCulture(properties.CultureInfo);
            }

            method = sub.Substitution;

            switch (sub.CultureSource)
            {
                case NumberCultureSource.Text:
                    return CultureMapper.GetSpecificCulture(properties.CultureInfo);

                case NumberCultureSource.User:
                    ignoreUserOverride = false;
                    return CultureInfo.CurrentCulture;

                case NumberCultureSource.Override:
                    return sub.CultureOverride;
            }

            return null;
        }

        private CultureInfo GetDigitCulture(CultureInfo numberCulture, NumberSubstitutionMethod method, out bool contextual)
        {
            contextual = false;

            if (numberCulture == null)
            {
                return null;
            }

            if (method == NumberSubstitutionMethod.AsCulture)
            {
                switch (numberCulture.NumberFormat.DigitSubstitution)
                {
                    case DigitShapes.Context:
                        method = NumberSubstitutionMethod.Context;
                        break;

                    case DigitShapes.NativeNational:
                        method = NumberSubstitutionMethod.NativeNational;
                        break;

                    default:
                        return null;
                }
            }

            CultureInfo digitCulture;

            switch (method)
            {
                case NumberSubstitutionMethod.Context:
                    if (IsArabic(numberCulture) || IsFarsi(numberCulture))
                    {
                        contextual = true;
                        digitCulture = GetTraditionalCulture(numberCulture);
                    }
                    else
                    {
                        digitCulture = null;
                    }
                    break;

                case NumberSubstitutionMethod.NativeNational:
                    if (!HasLatinDigits(numberCulture))
                    {
                        digitCulture = numberCulture;
                    }
                    else
                    {
                        digitCulture = null;
                    }
                    break;

                case NumberSubstitutionMethod.Traditional:
                    digitCulture = GetTraditionalCulture(numberCulture);
                    break;

                default:
                    digitCulture = null;
                    break;
            }

            return digitCulture;
        }

        private static bool HasLatinDigits(CultureInfo culture)
        {
            string[] digits = culture.NumberFormat.NativeDigits;
            for (int i = 0; i < 10; ++i)
            {
                string d = digits[i];
                if (d.Length != 1 || d[0] != (char)('0' + i))
                    return false;
            }
            return true;
        }

        private static bool IsArabic(CultureInfo culture)
        {
            return (culture.LCID & 0xFF) == 0x01;
        }

        private static bool IsFarsi(CultureInfo culture)
        {
            return (culture.LCID & 0xFF) == 0x29;
        }

        #region Traditional Cultures

        /// <summary>
        /// Returns the digit culture to use for traditional number substitution given the
        /// specified number culture.
        /// </summary>
        private CultureInfo GetTraditionalCulture(CultureInfo numberCulture)
        {
            int lcid = numberCulture.LCID;

            // Do we already have a traditional culture for this LCID?
            if (_lastTraditionalCulture != null && _lastTraditionalCulture.LCID == lcid)
            {
                return _lastTraditionalCulture;
            }

            // Branch using the primary language ID (the low-order word of the LCID). If a language
            // maps to more than one script, we then branch on the entire LCID. The mapping of cultures
            // (LCIDs) to scripts is based on the following spreadsheet:
            // http://winworld/teams/giftweb/lme/typography/Font%20Technology%20Infrastructure/OpenType%20and%20OTLS/lcid%20to%20OT%20ScriptLang.xls.
            //
            // If the LCID maps to a script for which we have traditional digits, we return the
            // the corresponding property. For example, the Marathi and Sanscrit languages map to
            // the Devangari script so we return the TraditionalDevangari property. The script names
            // are the English names in ISO-15924 (http://www.unicode.org/iso15924/iso15924-codes.html).
            // The "Arabic" script is a special case as it has two sets of digits, and therefore two
            // properties: TraditionalArabic and TraditionalEasternArabic.
            //
            // See also the Uniscribe number substitution spec:
            // http://winworld/teams/giftweb/lme/typography/Font%20Technology%20Infrastructure/Uniscribe%20and%20Shaping%20Engines/Digit%20Substitution.doc.
            // 
            CultureInfo digitCulture = null;

            switch (lcid & 0xFF)
            {
                case 0x01: // Arabic
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0660,         // Unicode value of Arabic digit zero
                        true);          // Arabic percent, decimal, and group symbols
                    break;
                case 0x1e: // Thai
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0e50,         // Unicode value of Thai digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x20: // Urdu
                case 0x29: // Persian
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x06F0,         // Unicode value of Eastern Arabic digit zero
                        true);          // Arabic percent, decimal, and group symbols
                    break;
                case 0x39: // Hindi
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0966,         // Unicode value of Devanagari digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x45: // Bengali
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x09e6,         // Unicode value of Bengali digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x46: // Punjabi
                    // This language maps to more than one script; branch on the lcid.
                    if (lcid == 0x0446) // Punjabi (India)
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x0a66,         // Unicode value of Gurmukhi digit zero
                            false);         // European percent, decimal, and group symbols
                    else if (lcid == 0x0846) // Punjabi (Pakistan)
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x06F0,         // Unicode value of Eastern Arabic digit zero
                            true);          // Arabic percent, decimal, and group symbols
                    break;
                case 0x47: // Gujarati
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0ae6,         // Unicode value of Gujarati digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x48: // Oriya
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0b66,         // Unicode value of Oriya digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x49: // Tamil
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0be6,         // Unicode value of Tamil digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x4a: // Teluga
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0c66,         // Unicode value of Teluga digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x4b: // Kannada
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0ce6,         // Unicode value of Kannada digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x4c: // Malayalam
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0d66,         // Unicode value of Malayalam digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x4d: // Assamese
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x09e6,         // Unicode value of Bengali digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x4e: // Marathi
                case 0x4f: // Sanskrit
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0966,         // Unicode value of Devanagari digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x50: // Mongolian
                    // This language maps to more than one script; branch on the lcid.
                    if (lcid == 0x0850) // Mongolian (PRC)
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x1810,         // Unicode value of Mongolian digit zero
                            false);         // European percent, decimal, and group symbols
                    break;
                case 0x51: // Tibetan
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0f20,         // Unicode value of Tibetan digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x53: // Khmer
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x17e0,         // Unicode value of Khmer digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x54: // Lao
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0ed0,         // Unicode value of Lao digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x55: // Burmese
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x1040,         // Unicode value of Myanmar (Burmese) digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x57: // Konkani
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0966,         // Unicode value of Devanagari digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x58: // Manipuri - India
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x09e6,         // Unicode value of Bengali digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x59: // Sindhi
                    // This language maps to more than one script; branch on the lcid.
                    if (lcid == 0x0459) // Sindhi (India)
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x0966,         // Unicode value of Devanagari digit zero
                            false);         // European percent, decimal, and group symbols
                    else if (lcid == 0x0859) // Sindhi (Pakistan)
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x06F0,         // Unicode value of Eastern Arabic digit zero
                            true);          // Arabic percent, decimal, and group symbols
                    break;
                case 0x5f: // Tamazight
                    // This language maps to more than one script; branch on the lcid.
                    if (lcid == 0x045f) // Tamazight (Berber/Arabic)
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x0660,         // Unicode value of Arabic digit zero
                            true);          // Arabic percent, decimal, and group symbols

                    break;
                case 0x60: // Kashmiri
                    // This language maps to more than one script; branch on the lcid.
                    if (lcid == 0x0460) // Kashmiri (Arabic); ks-PK
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x06F0,         // Unicode value of Eastern Arabic digit zero
                            true);          // Arabic percent, decimal, and group symbols
                    else if (lcid == 0x0860) // Kashmiri; ks-IN
                        digitCulture = CreateTraditionalCulture(
                            numberCulture,  // culture to clone
                            0x0966,         // Unicode value of Devanagari digit zero
                            false);         // European percent, decimal, and group symbols
                    break;
                case 0x61: // Nepali
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x0966,         // Unicode value of Devanagari digit zero
                        false);         // European percent, decimal, and group symbols
                    break;
                case 0x63: // Pashto
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x06F0,         // Unicode value of Eastern Arabic digit zero
                        true);          // Arabic percent, decimal, and group symbols
                    break;
                case 0x8c: // Dari
                    digitCulture = CreateTraditionalCulture(
                        numberCulture,  // culture to clone
                        0x06F0,         // Unicode value of Eastern Arabic digit zero
                        true);          // Arabic percent, decimal, and group symbols
                    break;
            }

            if (digitCulture == null)
            {
                // No hard-coded mapping for this LCID. Use the given culture if it has non-Latin digits,
                // otherwise return null. Don't cache the number culture because we didn't create it and
                // its digits may depend on other things than the LCID (e.g., it may be a custom culture).
                if (!HasLatinDigits(numberCulture))
                {
                    digitCulture = numberCulture;
                }
            }
            else
            {
                // We have a mapping for this LCID. Cache the digit culture in case we're called with
                // the same LCID again. 
                _lastTraditionalCulture = digitCulture;
            }

            return digitCulture;
        }

        // Create a modifiable culture with the same properties as the specified number culture,
        // but with digits '0' through '9' starting at the specified unicode value.
        private CultureInfo CreateTraditionalCulture(CultureInfo numberCulture, int firstDigit, bool arabic)
        {
            // Create the digit culture by cloning the given number culture. According to MSDN, 
            // "CultureInfo.Clone is a shallow copy with exceptions. The objects returned by 
            // the NumberFormat and the DateTimeFormat properties are also cloned, so that the 
            // CultureInfo clone can modify the properties of NumberFormat and DateTimeFormat 
            // without affecting the original CultureInfo."
            CultureInfo digitCulture = (CultureInfo)numberCulture.Clone();

            // Create the array of digits.
            string[] digits = new string[10];

            if (firstDigit < 0x10000)
            {
                for (int i = 0; i < 10; ++i)
                {
                    digits[i] = new string((char)(firstDigit + i), 1);
                }
            }
            else
            {
                for (int i = 0; i < 10; ++i)
                {
                    int n = firstDigit + i - 0x10000;

                    digits[i] = new string(
                        new char[] {
                            (char)((n >> 10) | 0xD800),     // high surrogate
                            (char)((n & 0x03FF) | 0xDC00)   // low surrogate
                            }
                        );
                }
            }

            // Set the digits.
            digitCulture.NumberFormat.NativeDigits = digits;

            if (arabic)
            {
                digitCulture.NumberFormat.PercentSymbol = "\u066a";
                digitCulture.NumberFormat.NumberDecimalSeparator = "\u066b";
                digitCulture.NumberFormat.NumberGroupSeparator = "\u066c";
            }
            else
            {
                digitCulture.NumberFormat.PercentSymbol = "%";
                digitCulture.NumberFormat.NumberDecimalSeparator = ".";
                digitCulture.NumberFormat.NumberGroupSeparator = ",";
            }

            return digitCulture;
        }

        private CultureInfo _lastTraditionalCulture;

        #endregion

        private NumberSubstitutionMethod _lastMethod;
        private CultureInfo _lastNumberCulture;

        private CultureInfo _digitCulture;
        private bool _contextual;
    }


    /// <summary>
    /// DigitMap maps unicode code points (from the backing store) to unicode code
    /// points (to be rendered) based on a specified digit culture.
    /// </summary>
    internal struct DigitMap
    {
        private NumberFormatInfo _format;
        private string[] _digits;

        internal DigitMap(CultureInfo digitCulture)
        {
            if (digitCulture != null)
            {
                _format = digitCulture.NumberFormat;
                _digits = _format.NativeDigits;
            }
            else
            {
                _format = null;
                _digits = null;
            }
        }

        internal int this[int ch]
        {
            get
            {
                if (_format != null && IsDigitOrSymbol(ch))
                {
                    uint n = (uint)ch - '0';
                    if (n < 10)
                    {
                        ch = StringToScalar(_digits[n], ch);
                    }
                    else if (ch == '%')
                    {
                        ch = StringToScalar(_format.PercentSymbol, ch);
                    }
                    else if (ch == ',')
                    {
                        ch = StringToScalar(_format.NumberGroupSeparator, ch);
                    }
                    else
                    {
                        ch = StringToScalar(_format.NumberDecimalSeparator, ch);
                    }
                }

                return ch;
            }
        }

        /// <summary>
        /// In some cases, our first choice for a substituted code point is not present 
        /// in many older fonts. To avoid displaying missing glyphs in such cases, this 
        /// function returns the alternate character to fall back to if the specified 
        /// substituted character does not exist in the font. The return value is zero
        /// if there is no fallback character.
        /// </summary>
        internal static int GetFallbackCharacter(int ch)
        {
            switch (ch)
            {
                case 0x066B: return (int)',';   // Arabic decimal point -> Western comma
                case 0x066C: return 0x060C;     // Arabic thousands separator -> Arabic comma
                case 0x0BE6: return (int)'0';   // Tamil zero -> Western zero
            }

            return 0;   // no fallback character
        }

        private static int StringToScalar(string s, int defaultValue)
        {
            if (s.Length == 1)
            {
                return (int)s[0];
            }
            else if (s.Length == 2 &&
                IsHighSurrogate((int)s[0]) &&
                IsLowSurrogate((int)s[1]))
            {
                return MakeUnicodeScalar((int)s[0], (int)s[1]);
            }
            else
            {
                return defaultValue;
            }
        }

        internal static bool IsHighSurrogate(int ch)
        {
            return ch >= 0xd800 && ch < 0xdc00;
        }

        internal static bool IsLowSurrogate(int ch)
        {
            return ch >= 0xdc00 && ch < 0xe000;
        }

        internal static bool IsSurrogate(int ch)
        {
            return IsHighSurrogate(ch) || IsLowSurrogate(ch);
        }

        internal static int MakeUnicodeScalar(int hi, int lo)
        {
            return ((hi & 0x03ff) << 10 | (lo & 0x03ff)) + 0x10000;
        }

        private static bool IsDigitOrSymbol(int ch)
        {
            // The code points we're interested in are in the range 0x25 - 0x39.
            const int first = 0x25; // percent
            const int last  = 0x39; // '9'

            // Make sure we're in range. This is necessary because (mask >> N)
            // where N is some large number does not yield zero, but rather is
            // equivalent to (mask >> (N % 32)).
            if ((uint)(ch - first) <= (uint)(last - first))
            {
                // Let mask be an array of bits indexed by code point, with
                // first as the base for indexing.
                const uint mask =
                    (1U << ('%' - first)) |  // U+0025
                    (1U << (',' - first)) |  // U+002C
                    (1U << ('.' - first)) |  // U+002E
                    (1U << ('0' - first)) |  // U+0030
                    (1U << ('1' - first)) |  // U+0031
                    (1U << ('2' - first)) |  // U+0032
                    (1U << ('3' - first)) |  // U+0033
                    (1U << ('4' - first)) |  // U+0034
                    (1U << ('5' - first)) |  // U+0035
                    (1U << ('6' - first)) |  // U+0036
                    (1U << ('7' - first)) |  // U+0037
                    (1U << ('8' - first)) |  // U+0038
                    (1U << ('9' - first));   // U+0039

                // Return the bit that correponds to the given code point.
                return ((mask >> (ch - first)) & 1) != 0;
            }
            else
            {
                // The code point is out of our given range.
                return false;
            }
        }
    }
}
