// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Generated content for marker symbol
//
//


using System;
using System.Diagnostics;
using System.Windows.Threading;
using System.Text;
using System.Globalization;

using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using MS.Internal;

using SR=MS.Internal.PresentationCore.SR;
using SRID=MS.Internal.PresentationCore.SRID;

namespace MS.Internal.TextFormatting
{
    /// <summary>
    /// Implementation of TextSource for used by marker
    /// </summary>
    internal sealed class TextMarkerSource : TextSource
    {
        private char[]                      _characterArray;
        private TextRunProperties           _textRunProperties;
        private TextParagraphProperties     _textParagraphProperties;

        private const char NumberSuffix = '.';

        private const string DecimalNumerics = "0123456789";
        private const string LowerLatinNumerics = "abcdefghijklmnopqrstuvwxyz";
        private const string UpperLatinNumerics = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static string[][] RomanNumerics = new string[][]
        {
            new string[] { "m??", "cdm", "xlc", "ivx" }, 
            new string[] { "M??", "CDM", "XLC", "IVX" }
        };

        internal TextMarkerSource(
            TextParagraphProperties     textParagraphProperties,
            TextMarkerStyle             markerStyle,
            int                         autoNumberingIndex
            )
        {
            Debug.Assert(markerStyle != TextMarkerStyle.None);

            _textParagraphProperties = textParagraphProperties;
            TextRunProperties defaultRunProperties = _textParagraphProperties.DefaultTextRunProperties;
            PixelsPerDip = defaultRunProperties.PixelsPerDip;
            string symbolString = null;

            if(IsKnownSymbolMarkerStyle(markerStyle))
            {
                switch(markerStyle)
                {
                    case TextMarkerStyle.Disc:
                        symbolString = "\x9f"; break;

                    case TextMarkerStyle.Circle:
                        symbolString = "\xa1"; break;

                    case TextMarkerStyle.Square:
                        symbolString = "\x71"; break;

                    case TextMarkerStyle.Box:
                        symbolString = "\xa7"; break;
                }

                Typeface defaultTypeface = defaultRunProperties.Typeface;

                // recreate a new marker run properties based on symbol typeface e.g. Wingding
                _textRunProperties = new GenericTextRunProperties(
                    new Typeface(
                        new FontFamily("Wingdings"), 
                        defaultTypeface.Style, 
                        defaultTypeface.Weight, 
                        defaultTypeface.Stretch
                        ),
                    defaultRunProperties.FontRenderingEmSize, 
                    defaultRunProperties.FontHintingEmSize,
                    PixelsPerDip, 
                    defaultRunProperties.TextDecorations, 
                    defaultRunProperties.ForegroundBrush,
                    defaultRunProperties.BackgroundBrush,
                    defaultRunProperties.BaselineAlignment,
                    CultureMapper.GetSpecificCulture(defaultRunProperties.CultureInfo),
                    null // default number substitution for culture
                    );
            }
            else if(IsKnownIndexMarkerStyle(markerStyle))
            {
                // Internal client code should have already validated this.
                Debug.Assert(autoNumberingIndex > 0);

                _textRunProperties = defaultRunProperties;

                int counter = autoNumberingIndex;

                switch(markerStyle)
                {
                    case TextMarkerStyle.Decimal:
                        _characterArray = ConvertNumberToString(counter, false, DecimalNumerics);
                        break;

                    case TextMarkerStyle.LowerLatin:
                        _characterArray = ConvertNumberToString(counter, true, LowerLatinNumerics);
                        break;

                    case TextMarkerStyle.UpperLatin:
                        _characterArray = ConvertNumberToString(counter, true, UpperLatinNumerics);
                        break;

                    case TextMarkerStyle.LowerRoman:
                        symbolString = ConvertNumberToRomanString(counter, false);
                        break;
                            
                    case TextMarkerStyle.UpperRoman:
                        symbolString = ConvertNumberToRomanString(counter, true);
                        break;
                }
            }
            else
            {
                Debug.Assert(false, "Invalid marker style");
            }

            if(symbolString != null)
            {
                _characterArray = symbolString.ToCharArray();
            }

            Debug.Assert(_characterArray != null);
        }

        #region TextSource implementation

        /// <summary>
        /// TextFormatter to get a text run started at specified text source position
        /// </summary>
        public override TextRun GetTextRun(
            int         textSourceCharacterIndex
            )
        {
            if (textSourceCharacterIndex < _characterArray.Length)
            {
                _textRunProperties.PixelsPerDip = PixelsPerDip;
                return new TextCharacters(
                    _characterArray,
                    textSourceCharacterIndex,
                    _characterArray.Length - textSourceCharacterIndex,
                    _textRunProperties
                    );
            }

            // Because LS ignores the character for TextEndOfLine characters in marker,
            // We must return TextEndOfParagraph to termine fetching.              
            return new TextEndOfParagraph(1);
        }


        /// <summary>
        /// TextFormatter to get text immediately before specified text source position.
        /// </summary>        
        public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(
            int         textSourceCharacterIndexLimit
            )
        {
            CharacterBufferRange charString = CharacterBufferRange.Empty;

            if (textSourceCharacterIndexLimit > 0)
            {   
                charString = new CharacterBufferRange(
                    new CharacterBufferReference(_characterArray, 0),
                    Math.Min(_characterArray.Length, textSourceCharacterIndexLimit)
                    );
            }        

            return new TextSpan<CultureSpecificCharacterBufferRange> (
                textSourceCharacterIndexLimit,
                new CultureSpecificCharacterBufferRange(CultureMapper.GetSpecificCulture(_textRunProperties.CultureInfo), charString)
                );
        }


        /// <summary>
        /// TextFormatter to map a text source character index to a text effect character index        
        /// </summary>
        /// <param name="textSourceCharacterIndex"> text source character index </param>
        /// <returns> the text effect index corresponding to the text effect character index </returns>
        public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(
            int textSourceCharacterIndex
            )
        {
            throw new NotSupportedException();
        }
        #endregion

        /// <summary>
        /// Convert a number to string, consisting of digits followed by the NumberSuffix character.
        /// </summary>
        /// <param name="number">Number to convert.</param>
        /// <param name="oneBased">True if there is no zero digit (e.g., alpha numbering).</param>
        /// <param name="numericSymbols">Set of digits (e.g., 0-9 or a-z).</param>
        /// <returns>Returns the number string as an array of characters.</returns>
        private static char[] ConvertNumberToString(int number, bool oneBased, string numericSymbols)
        {
            //  Whether zero-based or one-based numbering is used affects how we
            //  count and how we determine the maximum number of values for a
            //  given number of digits.
            //
            //  The following table illustrates how counting differs. In both
            //  cases we're using base-2 numbering (i.e., two distinct digits),
            //  but with 1-based counting each of those two digits can be a
            //  significant leading digit.
            //
            //            0-based     1-based
            //    ----------------------------
            //      0           0          --
            //      1           1           a
            //      2          10           b
            //      3          11          aa
            //      4         100          ab
            //      5         101          ba
            //      6         110          bb
            //      7         111         aaa
            //      8        1000         aab
            //      9        1001         aba
            //     10        1010         abb
            //     11        1011         baa
            //     12        1100         bab
            //     13        1101         bba
            //     14        1110         bbb
            //     15        1111        aaaa
            //     16       10000        aaab
            //
            //  For zero-based counting, adding a leading zero does not change
            //  the value of a number. Thus, the set of all N-digit numbers is
            //  a proper subset of the set of (N+1)-digit numbers. Thus the set
            //  of values that can be represented by N *or fewer* digits is the
            //  same as the number of combinations of exactly N digits, i.e.,
            //
            //      b ^ N
            //
            //  where b is the base of the numbering system.
            //
            //  For one-based counting, there is no zero digit. Thus, the set
            //  of N-digit numbers and the set of (N+1)-digit numbers are
            //  disjoint sets. Thus, while the number of combinations of
            //  *exactly* N digits is still b ^ N, the maximum value that
            //  can be represented by N *or fewer* digits is:
            //
            //  Max(N)
            //      where N = 1   :   b
            //      where N > 1   :   (b ^ N) + Max(N - 1)
            //
            if (oneBased)
            {
                // Subtract 1 from 1-based numbers so we can use zero-based
                // indexing. The formula for Max(N) given above should now be
                // thought of as a limit rather than a maximum.
                --number;
            }

            Debug.Assert(number >= 0);

            char[] result;

            int b = numericSymbols.Length;
            if (number < b)
            {
                // Optimize common case of single-digit numbers.
                result = new char[2]; // digit + suffix
                result[0] = numericSymbols[number];
                result[1] = NumberSuffix;
            }
            else
            {
                // Disjoint is 1 if and only if the set of numbers with N
                // digits and the set of numbers with (N+1) digits are
                // disjoint (see comment above). Otherwise it is zero.
                int disjoint = oneBased ? 1 : 0;

                // Count digits. We stop when the limit (i.e., 1 + the max value 
                // for the current number of digits) exceeds the specified number.
                int digits = 1;
                for (long limit = b, pow = b; (long)number >= limit; ++digits)
                {
                    // Neither of the following calculations can overflow because
                    // we know both pow and limit are <= number (which is an int)
                    // and b is at most 26.
                    pow *= b;
                    limit = pow + (limit * disjoint);
                }

                // Build string in reverse order starting with suffix.
                result = new char[digits + 1]; // digits + suffix
                result[digits] = NumberSuffix;
                for (int i = digits - 1; i >= 0; --i)
                {
                    result[i] = numericSymbols[number % b];
                    number = (number / b) - disjoint;
                }
            }

            return result;
        }

        /// <summary>
        /// Convert 1-based number to a Roman numeric string
        /// followed by NumberSuffix character.
        /// </summary>
        /// <remarks>
        /// Roman number is 1-based. The Roman numeric string is a series of symbols. Following
        /// is the list of symbols and its value.
        /// 
        ///     Symbol      Value
        ///         I           1
        ///         V           5  
        ///         X          10
        ///         L          50
        ///         C         100
        ///         D         500
        ///         M        1000
        /// 
        /// The rule of Roman number prohibits the use of more than 3 consecutive identical symbol
        /// but using subtraction of symbol standing for multiples of 10, so the value 4 is written
        /// as IV (5-1) rather than IIII. 
        /// 
        /// Due to the writing rule and the fact that the symbol represents not the numeral digit 
        /// but the value of the number. Roman number system cannot represent value larger than 3999.
        /// 
        /// See, http://www.ccsn.nevada.edu/math/ancient_systems.htm
        /// 
        /// However, there exists a more relaxing use of Roman numbers to represent values 4000 and  
        /// 4999 by using 4 consecutive M. The value 4999 is than written as 'MMMMCMXCIX'. Such use
        /// however is not widely accepted.
        /// 
        /// See, http://www.guernsey.net/~sgibbs/roman.html
        /// 
        /// For values larger than 3999, an overscore is used on the symbol to indicate 1000 multiplication.
        ///                                    ___
        /// So, value 7000 would be written as VII. This writing rule has a fair amount of disagreement
        /// since it is widely understood that it is not invented by the Romans and they rarely had a
        /// need for large numbers during their time. Furthermore, accepting this writing rule just
        /// for the sake of being able to write larger number would create a new limitation of the values
        /// greater than 3,999,999. Unicode 4.0 does not encode these overscore symbols.
        /// 
        /// See, http://www.gwydir.demon.co.uk/jo/roman/number.htm
        ///      http://www.novaroma.org/via_romana/numbers.html
        /// 
        /// Implementation-wise, IE adopts a general limitation of 3999 and simply convert the value
        /// into a regular numeric form.
        /// 
        /// We'll follow the mainstream and adopt the 3999 limit. The fallback would also do would IE does.
        /// 
        /// </remarks>
        private static string ConvertNumberToRomanString(
            int     number,
            bool    uppercase
            )
        {
            if (number > 3999)
            {
                // Roman numeric string not supported
                return number.ToString(CultureInfo.InvariantCulture);
            }

            StringBuilder builder = new StringBuilder();

            AddRomanNumeric(builder, number / 1000, RomanNumerics[uppercase ? 1 : 0][0]);
            number %= 1000;
            AddRomanNumeric(builder, number / 100, RomanNumerics[uppercase ? 1 : 0][1]);
            number %= 100;
            AddRomanNumeric(builder, number / 10, RomanNumerics[uppercase ? 1 : 0][2]);
            number %= 10;
            AddRomanNumeric(builder, number, RomanNumerics[uppercase ? 1 : 0][3]);

            builder.Append(NumberSuffix);

            return builder.ToString();
        }


        /// <summary>
        /// Convert number 0 - 9 into Roman numeric
        /// </summary>
        /// <param name="builder">string builder</param>
        /// <param name="number">number to convert</param>
        /// <param name="oneFiveTen">Roman numeric char for one five and ten</param>
        private static void AddRomanNumeric(
            StringBuilder       builder,
            int                 number,
            string              oneFiveTen
            )
        {
            Debug.Assert(number >= 0 && number <= 9);

            if (number >= 1 && number <= 9)
            {
                if (number == 4 || number == 9)
                    builder.Append(oneFiveTen[0]);

                if (number == 9)
                {
                    builder.Append(oneFiveTen[2]);
                }
                else
                {
                    if (number >= 4)
                        builder.Append(oneFiveTen[1]);

                    for (int i = number % 5; i > 0 && i < 4; i--)
                        builder.Append(oneFiveTen[0]);
                }
            }
        }


        internal static bool IsKnownSymbolMarkerStyle(TextMarkerStyle markerStyle)
        {
            return (
                    markerStyle == TextMarkerStyle.Disc 
                ||  markerStyle == TextMarkerStyle.Circle 
                ||  markerStyle == TextMarkerStyle.Square 
                ||  markerStyle == TextMarkerStyle.Box
                );
        }

        internal static bool IsKnownIndexMarkerStyle(TextMarkerStyle markerStyle)
        {
            return  (   
                    markerStyle == TextMarkerStyle.Decimal
                ||  markerStyle == TextMarkerStyle.LowerLatin
                ||  markerStyle == TextMarkerStyle.UpperLatin
                ||  markerStyle == TextMarkerStyle.LowerRoman
                ||  markerStyle == TextMarkerStyle.UpperRoman
                );
        }
    }
}
