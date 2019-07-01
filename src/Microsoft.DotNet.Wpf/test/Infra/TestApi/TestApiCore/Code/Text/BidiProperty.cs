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
    /// Bidi Unicode range property
    /// </summary>
    internal class BidiProperty : IStringProperty
    {
        /// <summary>
        /// Dictionary to store Bidi control code points for RegExp use
        /// </summary>
        private Dictionary<string, char> bidiDictionary = new Dictionary<string, char>();

        private List<UnicodeRangeProperty> bidiPropertyRangeList = new List<UnicodeRangeProperty>();
        
        private List<UnicodeRange> latinRangeList = new List<UnicodeRange>();

        private static readonly int[] exclusions = {0x0604, 0x0605, 0x061C, 0x061D, 0x0620, 0x065F, 0xFBB2, 0xFBB3, 0xFBB4, 0xFBB5, 0xFBB6, 0xFBB7, 0xFBB8, 
            0xFBB9, 0xFBBA, 0xFBBB, 0xFBBC, 0xFBBD, 0xFBBE, 0xFBBF, 0xFBC0, 0xFBC1, 0xFBC2, 0xFBC3, 0xFBC4, 0xFBC5, 0xFBC6, 0xFBC7, 0xFBC8, 0xFBC9, 0xFBCA, 
            0xFBCB, 0xFBCD, 0xFBCE, 0xFBCF, 0xFBD0, 0xFBD1, 0xFBD2, 0xFD40, 0xFD41, 0xFD42, 0xFD43, 0xFD44, 0xFD45, 0xFD46, 0xFD47, 0xFD48, 0xFD49, 0xFD4A, 
            0xFD4B, 0xFD4C, 0xFD4D, 0xFD4E, 0xFD4F, 0xFD90, 0xFD91, 0xFDC8, 0xFDC9, 0xFDCA, 0xFDCB, 0xFDCC, 0xFDCD, 0xFDCE, 0xFDCF, 0xFDD0, 0xFDD1, 0xFDD2, 
            0xFDD3, 0xFDD4, 0xFDD5, 0xFDD6, 0xFDD7, 0xFDD8, 0xFDD9, 0xFDDA, 0xFDDB, 0xFDDC, 0xFDDD, 0xFDDE, 0xFDDF, 0xFDE0, 0xFDE1, 0xFDE2, 0xFDE3, 0xFDE4, 
            0xFDE5, 0xFDE6, 0xFDE7, 0xFDE8, 0xFDE9, 0xFDEA, 0xFDEB, 0xFDEC, 0xFDEB, 0xFDEC, 0xFDED, 0xFDEE, 0xFDEF, 0xFDFE, 0xFDFF, 0xFE75, 0xFEFD, 0xFEFE, 
            0xFEFF, 0x0590, 0x05C8, 0x05C9, 0x05CA, 0x05CB, 0x05CC, 0x05CD, 0x05CE, 0x05CF, 0x05EB, 0x05EC, 0x05ED, 0x05EE, 0x05EF, 0x05F5, 0x05F6, 0x05F7, 
            0x05F8, 0x05F9, 0x05FA, 0x05FB, 0x05FC, 0x05FD, 0x05FE, 0x05FF, 0xFB07, 0xFB08, 0xFB09, 0xFB0A, 0xFB0B, 0xFB0C, 0xFB0D, 0xFB0E, 0xFB0F, 0xFB10, 
            0xFB11, 0xFB12, 0xFB18, 0xFB19, 0xFB1A, 0xFB1B, 0xFB1C, 0xFB37, 0xFB3D, 0xFB3F, 0xFB42, 0xFB45};

        private int [] bidiMarks;

        /// <summary>
        /// Define minimum code points need to be a bidi string
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 2;
        
        /// <summary>
        /// Define SurrogatePairDictionary class
        /// <a href="http://unicode.org/reports/tr9/">Newline</a>
        /// </summary>
        public BidiProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;

            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                        unicodeDb,
                        range,
                        bidiPropertyRangeList,
                        "Arabic",
                        GroupAttributes.Name))
                {
                    isValid = true;
                }

                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    bidiPropertyRangeList,
                    "Hebrew",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
            }

            if (InitializeBidiDictionary(expectedRanges))
            {
                isValid = true;
            }

            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "BidiProperty, Bidi ranges are beyond expected range. " +
                    "Refer to Arabic and Hebrew ranges.");
            }

            // Reset isValid to validate Latin range
            isValid = false;
            foreach (UnicodeRange expectedRange in expectedRanges)
            {
                UnicodeRange range = RangePropertyCollector.GetRange(new UnicodeRange(0x0030, 0x0039), expectedRange);
                if (null != range)
                {
                    latinRangeList.Add(range);
                    isValid = true;
                }

                range = RangePropertyCollector.GetRange(new UnicodeRange(0x0041, 0x005A), expectedRange);
                if (null != range)
                {
                    latinRangeList.Add(range);
                    isValid = true;
                }

                range = RangePropertyCollector.GetRange(new UnicodeRange(0x0061, 0x007A), expectedRange);
                if (null != range)
                {
                    latinRangeList.Add(range);
                    isValid = true;
                }
            }

            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "BidiProperty, Bidi ranges are beyond expected range. " +
                    "0x0030 - 0x0039,  0x0041 - 0x005A, and 0x0061 - 0x007A ranges are needed to construct Bidi string.");
            }
        }

        /// <summary>
        /// Dictionary to store code points corresponding to culture.
        /// </summary>
        private bool InitializeBidiDictionary(Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;
            
            bidiDictionary.Add("LEFTTORIGHTMARK", '\u200E');
            bidiDictionary.Add("RIGHTTOLEFTMARK", '\u200F');
            bidiDictionary.Add("LEFTTORIGHTEMBEDDING", '\u202A'); // quoation needed
            bidiDictionary.Add("RIGHTTOLEFTEMBEDDING", '\u202B'); // quoation needed
            bidiDictionary.Add("POPDIRECTIONALFORMATTING", '\u202C');
            bidiDictionary.Add("LEFTTORIGHTOVERRIDE", '\u202D');
            bidiDictionary.Add("RIGHTTOLEFTOVERRIDE", '\u202E');

            int i = 0;
            bidiMarks = new int [bidiDictionary.Count];
            Dictionary<string, char>.ValueCollection valueColl = bidiDictionary.Values;
            foreach (char codePoint in valueColl)
            {
                foreach (UnicodeRange range in expectedRanges)
                {
                    if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                    {
                        bidiMarks[i++] = (int)codePoint;
                        isValid = true;
                    }
                }
            }
            Array.Resize(ref bidiMarks, i);
            return isValid;
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (UnicodeRangeProperty prop in bidiPropertyRangeList)
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
        /// Get random bidi string
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException("BidiProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            // only support Arabic and Hebrew for current version
            string bidiStr = string.Empty;
            Random rand = new Random(seed);
            int index = 0; 

            for (int i=0; i < numOfProperty; i++)
            {
                index = rand.Next(0, bidiPropertyRangeList.Count);
                bidiStr += TextUtil.GetRandomCodePoint(bidiPropertyRangeList[index].Range, 1, exclusions, seed);
                index = rand.Next(0, latinRangeList.Count);
                bidiStr += TextUtil.GetRandomCodePoint(latinRangeList[index], 1, null, seed);
            }

            return bidiStr;
        }
    }
}



