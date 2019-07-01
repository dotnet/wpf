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
    /// Collect surrogate pairs
    /// </summary>
    internal class SurrogatePairProperty : IStringProperty
    {
        private List<UnicodeRangeProperty> surrogatePairRangeList = new List<UnicodeRangeProperty>();
        private UnicodeRange surrogateRange;
        
        private int highMin = 0; // 0xD800 V5.2
        private int highMax = 0; // 0xDBFF V5.2
        private int lowMin  = 0; // 0xDC00 V5.2
        private int lowMax  = 0; // 0xDFFF V5.2

        /// <summary>
        /// Define minimum code point needed to have surrogate pair
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 2;

        /// <summary>
        /// Define SurrogatePairProperty class
        /// <a href="http://www.unicode.org/charts/PDF/UD800.pdf">Newline</a>
        /// <a href="http://www.unicode.org/charts/PDF/UDC00.pdf">Newline</a>
        /// </summary>
        public SurrogatePairProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;

            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    surrogatePairRangeList,
                    "Surrogates",
                    GroupAttributes.GroupName))
                {
                    foreach (UnicodeRangeProperty data in surrogatePairRangeList)
                    {
                        if (data.Name.Equals("High Surrogates", StringComparison.OrdinalIgnoreCase))
                        {
                            highMin = data.Range.StartOfUnicodeRange;
                            highMax = data.Range.EndOfUnicodeRange;
                        }
                        else if (data.Name.Equals("Low Surrogates", StringComparison.OrdinalIgnoreCase))
                        {
                            lowMin = data.Range.StartOfUnicodeRange;
                            lowMax = data.Range.EndOfUnicodeRange;
                        }
                    }
                    isValid = true;
                }

                surrogateRange = RangePropertyCollector.GetRange(new UnicodeRange(0x10000, TextUtil.MaxUnicodePoint), range);
                if (null != surrogateRange)
                {
                    isValid = true;
                }
            }

            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "SurrogatePairProperty, SurrogatePair ranges are beyond expected range. " + 
                    "Refert to Surrogates range and UTF32.");
            }
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            if (codePoint > 0xFFFF)
            {
                if (null != surrogateRange)
                {
                    if (codePoint >= surrogateRange.StartOfUnicodeRange && codePoint <= surrogateRange.EndOfUnicodeRange)
                    {
                        isIn = true;
                    }
                }
            }

            if (0 != highMin)
            {
                if ((codePoint >= highMin && codePoint <= highMax) || (codePoint >= lowMin && codePoint <= lowMax))
                {
                    isIn = true;
                }
            }

            return isIn;
        }
        
        /// <summary>
        /// Get random Surrogate pairs
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            // NumOfProperty means number of pair
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException("SurrogatePairProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            string surrogateStr = string.Empty;
            Random rnd = new Random(seed);
            for (int i=1; i <= numOfProperty; i++)
            {
                if (null != surrogateRange && 0 != highMin)
                {
                    if (0 == rnd.Next(0, 1))
                    {
                        surrogateStr += Convert.ToChar(rnd.Next(highMin, highMax));
                        surrogateStr += Convert.ToChar(rnd.Next(lowMin, lowMax));
                    }
                    else
                    {
                        surrogateStr += TextUtil.IntToString(rnd.Next(surrogateRange.StartOfUnicodeRange, surrogateRange.EndOfUnicodeRange));
                    }
                }
                else if (0 != highMin)
                {
                    surrogateStr += Convert.ToChar(rnd.Next(highMin, highMax));
                    surrogateStr += Convert.ToChar(rnd.Next(lowMin, lowMax));
                }
                else
                {
                    surrogateStr += TextUtil.IntToString(rnd.Next(surrogateRange.StartOfUnicodeRange, surrogateRange.EndOfUnicodeRange));
                }
            }

            return surrogateStr;
        }
    }
}

