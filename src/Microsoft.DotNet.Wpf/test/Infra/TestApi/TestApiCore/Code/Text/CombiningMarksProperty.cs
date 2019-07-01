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
    /// Collect combining mark code points
    /// </summary>
    internal class CombiningMarksProperty : IStringProperty
    {
        /// <summary>
        /// Dictionary to store code point corresponding to culture.
        /// </summary>
        private Dictionary<string, char[]> combiningMarksDictionary = new Dictionary<string, char[]>();

        private List<UnicodeRangeProperty> combiningMarksPropertyRangeList = new List<UnicodeRangeProperty>();

        private static readonly int[] exclusions = {0xFE27, 0xFE28, 0xFE29, 0xFE2A, 0xFE2B, 0xFE2C, 0xFE2D, 0xFE2F, 0x1DE7, 0x1DE8, 0x1DE9, 0x1DEA,
            0x1DEB, 0x1DEC, 0x1DED, 0x1DEE, 0x1DEF, 0x1DF0, 0x1DF1, 0x1DF2, 0x1DF3, 0x1DF4, 0x1DF5, 0x1DF6, 0x1DF7, 0x1DF8, 0x1DF9, 0x1DFA, 0x1DFB, 0x1DFC};

        private int [] combiningMarks;

        /// <summary>
        /// Define minimum code points needed to be a combining mark string
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 2;
        
       /// <summary>
       /// Define CombiningMarksProperty class 
       /// <a href="http://www.unicode.org/charts/PDF/U0300.pdf">Newline</a>
       /// <a href="http://www.unicode.org/charts/PDF/U1DC0.pdf">Newline</a>
       /// <a href="http://www.unicode.org/charts/PDF/UFE20.pdf">Newline</a>
       /// </summary>
        public CombiningMarksProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;

            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    combiningMarksPropertyRangeList,
                    "Combining Diacritics",
                    GroupAttributes.GroupName))
                {
                    isValid = true;
                }
            }

            if (InitializeCombiningMarksDictionary(expectedRanges))
            {
                isValid = true;
            }

            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "CombiningMarksProperty, Combining mark ranges are beyond expected range. " +
                    "Refer to Combining Diacritics range.");
            }
        }
       
        private bool InitializeCombiningMarksDictionary(Collection<UnicodeRange> expectedRanges)
        {
            // Grave and acute accent
            char [] other = {'\u0302', '\u0307', '\u030A', '\u0315', '\u0316', '\u0317', '\u0318', '\u0319', '\u031A', '\u031C', '\u031D', 
                '\u031E', '\u031F', '\u0320', '\u0321', '\u0322', '\u0324', '\u032A', '\u032B', '\u032C', '\u032E', '\u0330', '\u0332', 
                '\u0333', '\u0334', '\u0335', '\u0336', '\u0337', '\u0338', '\u0339', '\u033A', '\u033B', '\u033C', '\u033D', '\u033F', 
                '\u0346', '\u0347', '\u0348', '\u0349', '\u034A', '\u034B', '\u034C', '\u034D', '\u034E', '\u034F', '\u0358', '\u0359',
                '\u035A', '\u035B', '\u035C', '\u035D', '\u035E', '\u0360', '\u0361', '\u0362', '\u0323', '\u0328', '\u032D', '\u032F',
                '\u1DC8', '\u1DC9', '\u1DCA', '\u1DCE', '\u1DCF', '\u1DD0', '\u1DD1', '\u1DD2', '\u1DD3',  '\u1DD4', '\u1DD5', '\u1DD6',
                '\u1DD7', '\u1DD8', '\u1DD9', '\u1DDA', '\u1DDB', '\u1DDC', '\u1DDD', '\u1DDE', '\u1DDF', '\u1DE0', '\u1DE1', '\u1DE2',
                '\u1DE3', '\u1DE4', '\u1DE5', '\u1DE6', '\uFE20', '\uFE21', '\uFE22', '\uFE23'};
            combiningMarksDictionary.Add("other", other);
            char [] vi = {'\u0303', '\u0308', '\u031B', '\u0323', '\u0340', '\u0341'};
            combiningMarksDictionary.Add("vi", vi);
            char [] el = {'\u0300','\u0301', '\u0304', '\u0305', '\u0306', '\u0308', '\u0313', '\u0314', '\u0331', '\u0342', '\u0343', 
                '\u0344', '\u0345', '\u1DC0', '\u1DC1', '\u1DC4', '\u1DC5', '\u1DC6', '\u1DC7', '\uFE24', '\uFE25', '\uFE26'};
            combiningMarksDictionary.Add("el", el); 
            char [] hu = {'\u030B', '\u0350', '\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', '\u0357'};
            combiningMarksDictionary.Add("hu", hu);
            char [] cs = {'\u030C'};
            combiningMarksDictionary.Add("cs", cs);
            char [] id = {'\u030D', '\u030E', '\u0325'};
            combiningMarksDictionary.Add("id", id);
            char [] ms = {'\u030D', '\u030E'};
            combiningMarksDictionary.Add("ms", ms);
            char [] srsp = {'\u030F', '\u0311', '\u0313', '\u0314', '\u033E', '\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', 
                '\u0357', '\u1DC3'};
            combiningMarksDictionary.Add("sr-sp", srsp);
            char [] hr = {'\u030F', '\u1DC3'};
            combiningMarksDictionary.Add("hr", hr);
            char [] hi = {'\u0310', '\u0325'};
            combiningMarksDictionary.Add("hi", hi);
            char [] azaz = {'\u0311', '\u0313', '\u0314', '\u033E', '\u0327'};
            combiningMarksDictionary.Add("az-az", azaz);
            char [] uzuz = {'\u0311', '\u0313', '\u0314', '\u033E'};
            combiningMarksDictionary.Add("uz-uz", uzuz);
            char [] lv = {'\u0312', '\u0326'};
            combiningMarksDictionary.Add("lv", lv);
            char [] fi = {'\u0326', '\u0350', '\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', '\u0357'};
            combiningMarksDictionary.Add("fi", fi);
            char [] hy = {'\u0313', '\u0314'};
            combiningMarksDictionary.Add("hy", hy);
            char [] he = {'\u0323'};
            combiningMarksDictionary.Add("he", he);
            char [] ar = {'\u0323'};
            combiningMarksDictionary.Add("ar", ar);
            char [] ro = {'\u0326', '\u0350', '\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', '\u0357'};
            combiningMarksDictionary.Add("ro", ro);
            char [] fr = {'\u0327'};
            combiningMarksDictionary.Add("fr", fr);
            char [] tr = {'\u0327'};
            combiningMarksDictionary.Add("tr", tr);
            char [] pl = {'\u0328'};
            combiningMarksDictionary.Add("pl", pl);
            char [] lt = {'\u0328', '\u035B', '\u1DCB', '\u1DCC'};
            combiningMarksDictionary.Add("lt", lt);
            char [] yoruba = {'\u0329'};
            combiningMarksDictionary.Add("yoruba", yoruba);
            char [] de = {'\u0329', '\u0363', '\u0364', '\u0365', '\u0366', '\u0367', '\u0368', '\u0369', '\u036A', '\u036B', '\u036C', '\u036D', 
                '\u036E', '\u036F'};
            combiningMarksDictionary.Add("de", de);
            char [] et = {'\u0350', '\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', '\u0357'};
            combiningMarksDictionary.Add("et", et);
            char [] ru = {'\u030B', '\u0350', '\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', '\u0357', '\u1DC3'};
            combiningMarksDictionary.Add("ru", ru);
            char [] sk = {'\u0351', '\u0352', '\u0353', '\u0354', '\u0355', '\u0356', '\u0357', '\u1DC3'};
            combiningMarksDictionary.Add("sk", sk);
            char [] be = {'\u1DC3'};
            combiningMarksDictionary.Add("be", be);
            char [] bg = {'\u1DC3'};
            combiningMarksDictionary.Add("bg", be);
            char [] mk = {'\u1DC3'};
            combiningMarksDictionary.Add("mk", mk);
            char [] sl = {'\u1DC3'};
            combiningMarksDictionary.Add("sl", sl);
            char [] uk = {'\u1DC3'};
            combiningMarksDictionary.Add("uk", uk);
            char [] symbol = {'\u20D0', '\u20D1', '\u20D2', '\u20D3', '\u20D4', '\u20D5', '\u20D6', '\u20D7', '\u20D8', '\u20D9', '\u20DA', 
                '\u20DF', '\u20E0', '\u20E1', '\u20E2', '\u20E3', '\u20E4', '\u20E5', '\u20E6', '\u20E7', '\u20E8', '\u20E9', '\u20EA', '\u20EB', 
                '\u20EC', '\u20ED', '\u20EF', '\u20F0'};
            combiningMarksDictionary.Add("symbol", symbol);

            bool isValid = false;
            int i = 0;
            combiningMarks = new int [other.Length + vi.Length + el.Length + hu.Length + cs.Length + id.Length + ms.Length + srsp.Length + hr.Length + 
                hi.Length + azaz.Length + uzuz.Length + lv.Length + fi.Length + hy.Length + he.Length + ar.Length + ro.Length + fr.Length + tr.Length + 
                pl.Length + lt.Length + yoruba.Length + de.Length + et.Length + ru.Length + sk.Length + be.Length + bg.Length + mk.Length + sl.Length + 
                uk.Length + symbol.Length];
            Dictionary<string, char[]>.ValueCollection valueColl = combiningMarksDictionary.Values;
            foreach (char [] values in valueColl)
            {
                foreach (char codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                        if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                        {
                            combiningMarks[i++] = (int)codePoint;
                            isValid = true;
                        }
                    }
                }
            }
            Array.Resize(ref combiningMarks, i);
            return isValid;
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (UnicodeRangeProperty prop in combiningMarksPropertyRangeList)
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
        /// Get random combining marks code points
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException(
                    "CombiningMarksProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            Random rand = new Random(seed);
            string combiningMarkStr = string.Empty;
            int index = rand.Next(0, combiningMarksPropertyRangeList.Count);
            combiningMarkStr += TextUtil.GetRandomCodePoint(combiningMarksPropertyRangeList[index].Range, numOfProperty, exclusions, seed);

            return combiningMarkStr;
        }
    }
}

