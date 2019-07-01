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
    /// Collect line break code points
    /// </summary>
    internal class LineBreakProperty : IStringProperty
    {
        /// <summary>
        /// Dictionary to store code points corresponding to culture.
        /// </summary>
        private Dictionary<string, char[]> lineBreakCharDictionary = new Dictionary<string, char[]>();

        private int [] lineBreakCodePoints;

        /// <summary>
        /// Define minimum code point needed to be a string has line break
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 1;
        
        /// <summary>
        /// Define LineBreakProperty class, 
        /// <a href="http://unicode.org/reports/tr13/tr13-5.html">Newline</a>
        /// </summary>
        public LineBreakProperty(Collection<UnicodeRange> expectedRangse)
        {
            if (!InitializeLineBreakCharDictionary(expectedRangse))
            {
                throw new ArgumentOutOfRangeException("expectedRangse", "LineBreakProperty, Linebreak ranges are beyond expected range. " + 
                    "Refert to CR, LF, CRLF, NEL, VT, FF, LS, and PS.");
            }
        }

        private bool InitializeLineBreakCharDictionary(Collection<UnicodeRange> expectedRanges)
        {
            char [] cr = {'\u000D'};
            lineBreakCharDictionary.Add("CR", cr);
            char [] lf = {'\u000A'};
            lineBreakCharDictionary.Add("LF", lf);
            char [] crlf = {'\u000D', '\u000A'};
            lineBreakCharDictionary.Add("CRLF", crlf);
            char [] nel = {'\u0085'};
            lineBreakCharDictionary.Add("NEL", nel);
            char [] vt = {'\u000B'};
            lineBreakCharDictionary.Add("VT", vt);
            char [] ff = {'\u000C'};
            lineBreakCharDictionary.Add("FF", ff); 
            char [] ls = {'\u2028'};
            lineBreakCharDictionary.Add("LS", ls);
            char [] ps = {'\u2029'};
            lineBreakCharDictionary.Add("PS", ps);

            int i = 0;
            bool isValid = false;
            lineBreakCodePoints = new int [cr.Length + lf.Length + crlf.Length + nel.Length + vt.Length + ff.Length + ls.Length + ps.Length];
            Dictionary<string, char[]>.ValueCollection valueColl = lineBreakCharDictionary.Values;
            foreach (char[] values in valueColl)
            {
                foreach (char codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                        if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                        {
                            lineBreakCodePoints[i++] = (int)codePoint;
                            isValid = true;
                        }
                    }
                }
            }
            Array.Resize(ref lineBreakCodePoints, i);
            return isValid;
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (int i in lineBreakCodePoints)
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
        /// Get next line break points 
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException("LineBreakProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            string lineBreakStr = string.Empty;
            Random rand = new Random(seed);
            for (int i=0; i < numOfProperty; i++)
            {
                lineBreakStr += TextUtil.IntToString(lineBreakCodePoints[rand.Next(0, lineBreakCodePoints.Length)]);
            }

            return lineBreakStr;
        }
    }
}
