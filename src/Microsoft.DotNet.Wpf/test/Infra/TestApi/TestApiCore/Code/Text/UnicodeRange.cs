// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// Represents a Unicode range.<para/> 
    /// A UnicodeRange instance can be created by either providing start and end of the 
    /// desired Unicode range or by providing a <see cref="UnicodeChart"/>.
    /// </summary>
    public class UnicodeRange
    {
        /// <summary>
        /// Create a UnicodeRange instance, using the provided UnicodeChart Enum type. 
        /// </summary>
        /// <param name="chart">Group name of scripts, symbols or punctuations (e.g. "European Scripts", "Punctuation", etc.)</param>
        public UnicodeRange(UnicodeChart chart)
        {
            UnicodeRange range = RangePropertyCollector.GetUnicodeChartRange(new UnicodeRangeDatabase(), chart);
            startOfUnicodeRange = range.StartOfUnicodeRange;
            endOfUnicodeRange = range.EndOfUnicodeRange;
        }

        /// <summary>
        /// Create a UnicodeRange instance, using the provided start and end of the Unicode range 
        /// </summary>
        /// <param name="start">Start of the Unicode range (e.g. 0x0000, etc.)</param>
        /// <param name="end">End of the Unicode range (e.g. 0xFFFF, etc.)</param>
        public UnicodeRange(int start, int end)
        {
            Init(start, end);
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="range">A UnicodeRange object to be copied</param>
        public UnicodeRange(UnicodeRange range)
        {
            startOfUnicodeRange = range.StartOfUnicodeRange;
            endOfUnicodeRange = range.EndOfUnicodeRange;
        }

        private void Init(int low, int high)
        {
            if (low > high)
            {
                throw new ArgumentOutOfRangeException ("UnicodeRange, low " + low + " shouldn't be greater than high " + high + " value.");
            }
            else if (low < 0)
            {
                throw new ArgumentOutOfRangeException ("UnicodeRange, low is" + low + ", cannot be less than 0x0.");
            }
            else if (high > TextUtil.MaxUnicodePoint)
            {
                throw new ArgumentOutOfRangeException ("UnicodeRange, high cannot be greater than " + 
                    String.Format(CultureInfo.InvariantCulture, "0x{0:X}", TextUtil.MaxUnicodePoint) + ".");
            }
            startOfUnicodeRange = low;
            endOfUnicodeRange = high;
        }
        
        /// <summary>
        /// Get the start of the Unicode range
        /// </summary>
        public int StartOfUnicodeRange { get { return startOfUnicodeRange; } }

        /// <summary>
        /// Get the end of the Unicode range
        /// </summary>
        public int EndOfUnicodeRange { get { return endOfUnicodeRange; } }

        private int startOfUnicodeRange;
        private int endOfUnicodeRange;
    }
}

