// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Information about list markers for different types of list. 
// NOTE: This logic depends on the logic used in MS.Internal.TextFormatting.TextMarkerSource.cs 
// This file must always be kept up to date with changes in TextMarkerSource
//

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;   // List
using MS.Internal.Text;             
using System.Windows.Media;       // FormattedText and Brush
using MS.Internal.TextFormatting; // TextMarkerSource
using System.Text;                // StringBuilder

namespace MS.Internal.PtsHost
{
    /// <summary>
    /// ListMarkerSourceInfo contains information about list markers for different types of lists
    /// </summary>
    internal sealed class ListMarkerSourceInfo
    {
        /// <summary>
        /// Private constructor to prevent the compiler from generating a default constructor. 
        /// </summary>
        private ListMarkerSourceInfo()
        {
        }

        /// <summary>
        /// Calculate padding for a list
        /// </summary>
        /// <param name="list">
        /// Specified List element 
        /// </param>
        /// <param name="lineHeight">
        /// Line height for list element
        /// </param>
        internal static Thickness CalculatePadding(List list, double lineHeight, double pixelsPerDip)
        {
            FormattedText formattedMarker = GetFormattedMarker(list, pixelsPerDip);
            double leftPadding = formattedMarker.Width + 1.5 * lineHeight;
            leftPadding = (double)((int)(leftPadding / lineHeight) + 1) * lineHeight;
            return new Thickness(leftPadding, 0, 0, 0);
        }

        /// <summary>
        /// Returns FormattedText for the largets marker in a list
        /// </summary>
        /// <param name="list">
        /// List element for which formatted marker is to be calculated
        /// </param>
        private static FormattedText GetFormattedMarker(List list, double pixelsPerDip)
        {
            string markerString = "";
            FormattedText formattedMarker;

            if (IsKnownSymbolMarkerStyle(list.MarkerStyle))
            {
                switch (list.MarkerStyle)
                {
                    case TextMarkerStyle.Disc:
                        markerString = "\x9f";
                        break;

                    case TextMarkerStyle.Circle:
                        markerString = "\xa1";
                        break;

                    case TextMarkerStyle.Square:
                        markerString = "\x71";
                        break;

                    case TextMarkerStyle.Box:
                        markerString = "\xa7";
                        break;
                }

                // Create new formatted text with typeface using a symbol font, e.g. Wingdings
                Typeface typeface = DynamicPropertyReader.GetModifiedTypeface(list, new FontFamily("Wingdings"));
                
                formattedMarker = new FormattedText(markerString, DynamicPropertyReader.GetCultureInfo(list), list.FlowDirection,
                                      typeface, list.FontSize, list.Foreground, pixelsPerDip);
}
            else if (IsKnownIndexMarkerStyle(list.MarkerStyle))
            {
                // Assume at least one element will be added and format accordingly
                int startIndex = list.StartIndex;
                Invariant.Assert(startIndex > 0);
                int size = list.ListItems.Count;
                int highestIndex;
                if (int.MaxValue - size < startIndex)
                {
                    // Highest index will exceed max value of int. Clamp to int.MaxValue
                    highestIndex = int.MaxValue;
                }
                else
                {
                    highestIndex = (size == 0) ? startIndex : startIndex + size - 1;
                }
                switch (list.MarkerStyle)
                {
                    case TextMarkerStyle.Decimal:
                        markerString = ConvertNumberToString(highestIndex, false, DecimalNumerics);
                        break;

                    case TextMarkerStyle.LowerLatin:
                        markerString = ConvertNumberToString(highestIndex, true, LowerLatinNumerics);
                        break;

                    case TextMarkerStyle.UpperLatin:
                        markerString = ConvertNumberToString(highestIndex, true, UpperLatinNumerics);
                        break;

                    case TextMarkerStyle.LowerRoman:
                        markerString = GetStringForLargestRomanMarker(startIndex, highestIndex, false);
                        break;

                    case TextMarkerStyle.UpperRoman:
                        markerString = GetStringForLargestRomanMarker(startIndex, highestIndex, true);
                        break;
                }

                // Create new formatted text using List defaulls                
                formattedMarker = new FormattedText(markerString, DynamicPropertyReader.GetCultureInfo(list), list.FlowDirection,
                                      DynamicPropertyReader.GetTypeface(list), list.FontSize, list.Foreground, pixelsPerDip);
            }
            else
            {
                // Assume a disc
                markerString = "\x9f";
                // Create new formatted text with typeface using a symbol font, e.g. Wingdings
                Typeface typeface = DynamicPropertyReader.GetModifiedTypeface(list, new FontFamily("Wingdings"));

                formattedMarker = new FormattedText(markerString, DynamicPropertyReader.GetCultureInfo(list), list.FlowDirection,
                                      typeface, list.FontSize, list.Foreground, pixelsPerDip);
            }
            return formattedMarker;
        }

        /// <summary>
        /// Convert a number to string, consisting of digits followed by the NumberSuffix character.
        /// From TextMarkerSource, uses same conversion
        /// </summary>
        /// <param name="number">Number to convert.</param>
        /// <param name="oneBased">True if there is no zero digit (e.g., alpha numbering).</param>
        /// <param name="numericSymbols">Set of digits (e.g., 0-9 or a-z).</param>
        /// <returns>Returns the number string as an array of characters.</returns>
        private static string ConvertNumberToString(int number, bool oneBased, string numericSymbols)
        {
            if (oneBased)
            {
                // Subtract 1 from 1-based numbers so we can use zero-based indexing
                --number;
            }

            Invariant.Assert(number >= 0);

            char[] result;
            int b = numericSymbols.Length;
            if (number < b)
            {
                // Optimize common case of single-digit numbers.
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
                // Build string in reverse order starting with suffix.
                result = new char[digits + 1]; // digits + suffix
                result[digits] = NumberSuffix;
                for (int i = digits - 1; i >= 0; --i)
                {
                    result[i] = numericSymbols[number % b];
                    number = (number / b) - disjoint;
                }
            }

            return new string(result);
        }

        /// <summary>
        /// Returns string for the largest roman marker in a list with Roman numbering style
        /// </summary>
        /// <param name="startIndex">
        /// Start index of list
        /// </param>
        /// <param name="highestIndex">
        /// Number of elements in the list
        /// </param>
        /// <param name="uppercase">
        /// True if list uses uppercase mode
        /// </param>
        private static string GetStringForLargestRomanMarker(int startIndex, int highestIndex, bool uppercase)
        {
            int largestMarkerIndex = 0;
            if (highestIndex > 3999)
            {
                // Roman numerals are 1-based and there is no accepted convention
                // for writing numbers larger than 3999. For anything larger than this,
                // we assume the largest value under 3999, which is 3888 or
                // MMMDCCCLXXXVIII. TextSourceMarker willnot support larger values anyway.
                return uppercase ? LargestRomanMarkerUpper : LargestRomanMarkerLower;
            }
            else
            {
                largestMarkerIndex = GetIndexForLargestRomanMarker(startIndex, highestIndex);
            }
            return ConvertNumberToRomanString(largestMarkerIndex, uppercase);
        }

        /// <summary>
        /// Returns the index of the roman marker that will have largest width, i.e. most letters
        /// </summary>
        /// <param name="startIndex">
        /// Start index of the list
        /// </param>
        /// <param name="highestIndex">
        /// Highest-numbered index in the list
        /// </param>
        private static int GetIndexForLargestRomanMarker(int startIndex, int highestIndex)
        {
            int largestIndex = 0;

            if (startIndex == 1)
            {
                // Do quick search by looking only at size increments
                int thousands = highestIndex / 1000;
                highestIndex = highestIndex % 1000;            
                for (int i = 0; i < RomanNumericSizeIncrements.Length; i++)
                {
                    Invariant.Assert(highestIndex >= RomanNumericSizeIncrements[i]);
                    if (highestIndex == RomanNumericSizeIncrements[i])
                    {
                        // This is the largest index. 
                        largestIndex = highestIndex;
                        break;
                    }
                    else
                    {
                        Invariant.Assert(highestIndex > RomanNumericSizeIncrements[i]);
                        if (i < RomanNumericSizeIncrements.Length - 1)
                        {
                            if (highestIndex >= RomanNumericSizeIncrements[i + 1])
                            {
                                // Size does not lie within this increment range. Keep searching
                                continue;
                            }
                        }

                        // Size is either larger than the largest increment value,
                        // or lies between two increment values in which case we
                        // take the lower one
                        largestIndex = RomanNumericSizeIncrements[i];
                        break;
                    }
                }
                if (thousands > 0)
                {
                    // M's will be added to largest index for extra thousands
                    largestIndex = thousands * 1000 + largestIndex;
                }
            }
            else
            {
                // Quick search will not work. Look at each index
                int largestIndexSize = 0;
                for (int i = startIndex; i <= highestIndex; i++)
                {
                    // Format as roman string. It does not matter if we use upper or lowercase formatting here since 
                    // we are only counting number of letters in each string. This is not strictly correct - 
                    // III might be smaller than XX in some fonts - but we cannot format text each time.
                    string romanString = ConvertNumberToRomanString(i, true);
                    if (romanString.Length > largestIndexSize)
                    {
                        largestIndex = i;
                        largestIndexSize = romanString.Length;
                    }
                }
            }

            Invariant.Assert(largestIndex > 0);
            return largestIndex;
        }

        /// <summary>
        /// Convert 1-based number to a Roman numeric string
        /// followed by NumberSuffix character.
        /// </summary>
        private static string ConvertNumberToRomanString(
            int number,
            bool uppercase)
        {
            Invariant.Assert(number <= 3999);

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
        /// Convert number 0 - 9 into Roman numeric. From TextMarkerSource
        /// </summary>
        /// <param name="builder">string builder</param>
        /// <param name="number">number to convert</param>
        /// <param name="oneFiveTen">Roman numeric char for one five and ten</param>
        private static void AddRomanNumeric(
            StringBuilder builder,
            int number,
            string oneFiveTen)
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

        private static bool IsKnownSymbolMarkerStyle(TextMarkerStyle markerStyle)
        {
            return (
                    markerStyle == TextMarkerStyle.Disc
                || markerStyle == TextMarkerStyle.Circle
                || markerStyle == TextMarkerStyle.Square
                || markerStyle == TextMarkerStyle.Box
                );
        }

        private static bool IsKnownIndexMarkerStyle(TextMarkerStyle markerStyle)
        {
            return (
                    markerStyle == TextMarkerStyle.Decimal
                || markerStyle == TextMarkerStyle.LowerLatin
                || markerStyle == TextMarkerStyle.UpperLatin
                || markerStyle == TextMarkerStyle.LowerRoman
                || markerStyle == TextMarkerStyle.UpperRoman
                );
        }

        // Const values for marker conversion
        private static char NumberSuffix = '.';
        private static string DecimalNumerics = "0123456789";
        private static string LowerLatinNumerics = "abcdefghijklmnopqrstuvwxyz";
        private static string UpperLatinNumerics = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private static string LargestRomanMarkerUpper = "MMMDCCCLXXXVIII";
        private static string LargestRomanMarkerLower = "mmmdccclxxxviii";

        private static string[][] RomanNumerics = new string[][]
        {
            new string[] { "m??", "cdm", "xlc", "ivx" }, 
            new string[] { "M??", "CDM", "XLC", "IVX" }
        };

        private static int[] RomanNumericSizeIncrements =
            new int[] { 1, 2, 3, 8, 18, 28, 38, 88, 188, 288, 388, 888 };
}
}

