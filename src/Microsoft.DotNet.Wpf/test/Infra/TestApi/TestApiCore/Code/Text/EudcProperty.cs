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
    /// Collect EUDC code points
    /// </summary>
    internal class EudcProperty : IStringProperty
    {
        private List<UnicodeRangeProperty> eudcRangeList = new List<UnicodeRangeProperty>();

        private int low;  // 0xE000

        private int high; // 0xF8FF

        /// <summary>
        /// Define minimum code point needed to be an EUDC string
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 1;
        

        /// <summary>
        /// Define SurrogatePairDictionary class
        /// <a href="http://www.unicode.org/charts/PDF/UE000.pdf">Newline</a>
        /// </summary>
        public EudcProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            low = 0xE000; high = 0xF8FF;

            bool isValid = false;
            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    eudcRangeList,
                    "Private Use",
                    GroupAttributes.GroupName))
                {
                    isValid = true;
                }
            }

            if(!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "EudcProperty, EUDC ranges are beyond expected range. " +
                    "Refer to Private Use range.");
            }
            
            foreach (UnicodeRangeProperty data in eudcRangeList)
            {
                if (data.Name.Equals("Private Use Area", StringComparison.OrdinalIgnoreCase))
                {
                    low = data.Range.StartOfUnicodeRange;
                    high = data.Range.EndOfUnicodeRange;
                    break;
                }
            }
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (UnicodeRangeProperty prop in eudcRangeList)
            {
                if (codePoint >= prop.Range.StartOfUnicodeRange && codePoint <= prop.Range.EndOfUnicodeRange)
                {
                    isIn = true;
                    break;
                }
            }

            return isIn;
        }
        
        /// <summary>
        /// Get random EUDC code points
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException("EudcProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            string eudcStr = string.Empty;
            eudcStr += TextUtil.GetRandomCodePoint(new UnicodeRange(low, high), numOfProperty, null, seed);

            return eudcStr;
        }
    }
}


