// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;


namespace Microsoft.Test.Text
{
    /// <summary>
    /// Collect number code points
    /// </summary>
    internal class NumberProperty : IStringProperty
    {
        /// <summary>
        /// Dictionary to store code point corresponding to culture.
        /// </summary>
        private Dictionary<string, char[]> numberDictionary = new Dictionary<string, char[]>();

        private List<UnicodeRangeProperty> numberDigitRangeList = new List<UnicodeRangeProperty>(); 

        private int [] numberCodePoints;

        /// <summary>
        /// Define minimum code point needed to be a string has number
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 1;
        
        /// <summary>
        /// Define NumberProperty class, 
        /// <a href="http://unicode.org/reports/tr13/tr13-5.html">Newline</a>
        /// </summary>
        public NumberProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;

            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    numberDigitRangeList,
                    "Numbers and Digits",
                    GroupAttributes.GroupName))
                {
                    isValid = true;
                }
            }
            
            if (InitializeNumberCharDictionary(expectedRanges))
            {
                isValid = true;
            }

            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "NumberProperty, number ranges are beyond expected range. " +
                    "Refer to latin numberals and point, percent, plus, and minus signs, and comma.");
            }
        }

        private bool InitializeNumberCharDictionary(Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;
            char [] latin = {'\u0030', '\u0031', '\u0032', '\u0033', '\u0034', '\u0035', '\u0036', '\u0037', '\u0038', '\u0039'};
            numberDictionary.Add("latin", latin);
            char [] piont = {'\u002E'};
            numberDictionary.Add("piont", piont);
            char [] percent = {'\u0025'};
            numberDictionary.Add("percent", percent);
            char [] minus = {'\u002D'};
            numberDictionary.Add("minus", minus);
            char [] plus = {'\u002B'};
            numberDictionary.Add("plus", plus);
            char [] comma = {'\u002C'};
            numberDictionary.Add("comma", comma);

            int i = 0;
            numberCodePoints = new int [latin.Length];
            foreach (char codePoint in numberDictionary["latin"])
            {
                foreach (UnicodeRange range in expectedRanges)
                {
                    if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                    {
                        numberCodePoints[i++] = (int)codePoint;
                        isValid = true;
                    }
                }
            }
            Array.Resize(ref numberCodePoints, i);
            
            return isValid;
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (int i in numberCodePoints)
            {
                if (i == codePoint)
                {
                    isIn = true;
                    break;
                }
            }

            return isIn;
        }

        /// <summary>
        /// Get number code points
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException("NumberProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            string numStr = string.Empty;
            Random rand = new Random(seed);
            for (int i= 0; i < numOfProperty; i++)
            {
                int index = rand.Next(0, numberCodePoints.Length);
                numStr += TextUtil.IntToString(numberCodePoints[index]);
            }

            return numStr;
        }
    }
}

