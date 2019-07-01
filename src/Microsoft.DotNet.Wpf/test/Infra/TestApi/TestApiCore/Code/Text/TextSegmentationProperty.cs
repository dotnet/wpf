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
    /// Collect of text segmentation code points
    /// </summary>
    internal class TextSegmentationProperty : IStringProperty
    {
        /// <summary>
        /// Dictionary to store code points corresponding to culture.
        /// </summary>
        private Dictionary<string, char[]> sampleGraphemeClusterDictionary = new Dictionary<string, char[]>();
        private Dictionary<string, char[]> graphemeClusterBreakPropertyValuesDictionary = new Dictionary<string, char[]>();
        private Dictionary<string, char[]> wordBreakPropertyValuesDictionary = new Dictionary<string, char[]>();
        private Dictionary<string, char[]> sentenceBreakPropertyValuesDictionary = new Dictionary<string, char[]>();

        private List<UnicodeRangeProperty> textSegmentationRangeList = new List<UnicodeRangeProperty>(); 

        private int [] textSegmentationCodePoints;

        /// <summary>
        /// Define minimum code point needed to be a text segmentation string
        /// </summary>
        public static readonly int MINNUMOFCODEPOINT = 1;
        
        /// <summary>
        /// Define LineBreakDictionary class, 
        /// <a href="http://www.unicode.org/reports/tr29/">Newline</a>
        /// </summary>
        public TextSegmentationProperty(UnicodeRangeDatabase unicodeDb, Collection<UnicodeRange> expectedRanges)
        {
            bool isValid = false;

            foreach (UnicodeRange range in expectedRanges)
            {
                if (RangePropertyCollector.BuildPropertyDataList(
                    unicodeDb,
                    range,
                    textSegmentationRangeList,
                    "Controls",
                    GroupAttributes.Name))
                {
                    isValid = true;
                }
            }
                
            if (InitializeDictionaries(expectedRanges))
            {
                isValid = true;
            }

            if (!isValid)
            {
                throw new ArgumentOutOfRangeException("expectedRanges", "TextSegmentationProperty, " + 
                    "code points for text segmentation ranges are beyond expected range. " + "Refert to Controls ranges");
            }
        }

        private bool InitializeDictionaries(Collection<UnicodeRange> expectedRanges)
        {
            char [] ko = {'\u1100', '\u1161', '\u11A8'};
            sampleGraphemeClusterDictionary.Add("Ko", ko);
            char [] ta = {'\u0BA8', '\u0BBF'};
            sampleGraphemeClusterDictionary.Add("ta", ta);
            char [] th = {'\u0E40', '\u0E01'};
            sampleGraphemeClusterDictionary.Add("th", th);
            char [] devanagari  = {'\u0937', '\u093F', '\u0915', '\u094D', '\u0937', '\u093F'};
            sampleGraphemeClusterDictionary.Add("devanagari", devanagari);
            char [] sk = {'\u0063', '\u0068'};
            sampleGraphemeClusterDictionary.Add("sk", sk);
            char [] other = {'\u0067', '\u0308', '\u006B', '\u02B7'};
            sampleGraphemeClusterDictionary.Add("other", other);

            char [] all = {'\u000D', '\u000A', '\u0000', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006', '\u0007', '\u0008', '\u0009', '\u000B',
                '\u000C', '\u000E', '\u000F', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014', '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001A',
                '\u001B', '\u001C', '\u001D', '\u001E', '\u001F', '\u0020', '\u007F', '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086',
                '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F', '\u0090', '\u0091', '\u0092', '\u0093', '\u0094',
                '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F', '\u00A0', '\u00AD', '\u2000',
                '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A'};
            graphemeClusterBreakPropertyValuesDictionary.Add("all", all);
            char [] th1 = {'\u0E30', '\u0E32', '\u0E33', '\u0E40', '\u0E41', '\u0E42', '\u0E43', '\u0E44', '\u0E45'};
            graphemeClusterBreakPropertyValuesDictionary.Add("th", th1);
            char [] lao = {'\u0EB0', '\u0EB2', '\u0EB3', '\u0EC0', '\u0EC1', '\u0EC2', '\u0EC3', '\u0EC4'};
            graphemeClusterBreakPropertyValuesDictionary.Add("lao", lao);
             char [] ko1 = {'\u1100', '\u1101', '\u1102', '\u1103', '\u1104', '\u1105', '\u1106', '\u1107', '\u1108', '\u1109', '\u110A', '\u110B', '\u110C',
                '\u110D', '\u110E', '\u110F', '\u1110', '\u1111', '\u1112', '\u1113', '\u1114', '\u1115', '\u1116', '\u1117', '\u1118', '\u1119', '\u111A',
                '\u111B', '\u111C', '\u111D', '\u111E', '\u111F', '\u1120', '\u1121', '\u1122', '\u1123', '\u1124', '\u1125', '\u1126', '\u1127', '\u1128',
                '\u1129', '\u112A', '\u112B', '\u112C', '\u112D', '\u112E', '\u112F', '\u1130', '\u1131', '\u1132', '\u1133', '\u1134', '\u1135', '\u1136',
                '\u1137', '\u1138', '\u1139', '\u1140', '\u1141', '\u1142', '\u1143', '\u1144', '\u1145', '\u1146', '\u1147', '\u1148', '\u1149', '\u114A',
                '\u114B', '\u114C', '\u114D', '\u114E', '\u114F', '\u1150', '\u1151', '\u1152', '\u1153', '\u1154', '\u1155', '\u1156', '\u1157', '\u1158',
                '\u1159', '\u111F', '\u1160', '\u1161', '\u1162', '\u1163', '\u1164', '\u1165', '\u1166', '\u1167', '\u1168', '\u1169', '\u116A', '\u116B',
                '\u116C', '\u116D', '\u116E', '\u116F', '\u1170', '\u1171', '\u1172', '\u1173', '\u1174', '\u1175', '\u1176', '\u1177', '\u1178', '\u1179',
                '\u117A', '\u117B', '\u117C', '\u117D', '\u117E', '\u117F', '\u1180', '\u1181', '\u1182', '\u1183', '\u1184', '\u1185', '\u1186', '\u1187',
                '\u1188', '\u1189', '\u118A', '\u118B', '\u118C', '\u118D', '\u118E', '\u118F', '\u1190', '\u1191', '\u1192', '\u1193', '\u1194', '\u1195',
                '\u1196', '\u1197', '\u1198', '\u1199', '\u119A', '\u119B', '\u119C', '\u119E', '\u119F', '\u11A0', '\u11A1', '\u11A2', '\u11A8', '\u11A9',
                '\u11AA', '\u11AB', '\u11AC', '\u11AD', '\u11AE', '\u11AF', '\u11B0', '\u11B1', '\u11B2', '\u11B3', '\u11B4', '\u11B5', '\u11B6', '\u11B7',
                '\u11B8', '\u11B9', '\u11BA', '\u11BB', '\u11BC', '\u11BD', '\u11BE', '\u11BF', '\u11C0', '\u11C1', '\u11C2', '\u11C3', '\u11C4', '\u11C5',
                '\u11C6', '\u11C7', '\u11C8', '\u11C9', '\u11CA', '\u11CB', '\u11CC', '\u11CE', '\u11CF', '\u11D0', '\u11D1', '\u11D2', '\u11D3', '\u11D4',
                '\u11D5', '\u11D6', '\u11D7', '\u11D8', '\u11D9', '\u11DA', '\u11DB', '\u11DC', '\u11DE', '\u11DF', '\u11F0', '\u11F1', '\u11F2', '\u11F3',
                '\u11F4', '\u11F5', '\u11F7', '\u11F8', '\u11F9', '\uAC00', '\uAC1C', '\uAC38', '\uAC01', '\uAC02', '\uAC03', '\uAc04'};
            graphemeClusterBreakPropertyValuesDictionary.Add("ko", ko1);

            char [] all1 = {'\u000A', '\u000D', '\u000B', '\u000C', '\u0020', '\u0027', '\u0085', '\u002D', '\u002E', '\u202F','\u00A0', '\u2028', '\u2029',
                '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A', '\u2010', '\u2011', '\u2018', 
                '\u2019', '\u201B', '\u2024', '\uFE52', '\uFF07', '\uFF0E', '\u00B7', '\u05F4', '\u2027', '\u003A', '\u0387', '\uFE13', '\uFE55', '\uFF1A', 
                '\u066C', '\uFE50', '\uFE54', '\uFE63', '\uFF0D', '\uFF0C', '\uFF1B'};
            wordBreakPropertyValuesDictionary.Add("all", all1);
            char [] katakana = {'\u3031', '\u3032', '\u3033', '\u3034', '\u3035', '\u309B', '\u309C', '\u30A0', '\u30FC', '\uFF70'};
            wordBreakPropertyValuesDictionary.Add("ja", katakana);
            char [] he = {'\u05F3'};
            wordBreakPropertyValuesDictionary.Add("he", he);
            char [] hy = {'\u055A', '\u058A'};
            wordBreakPropertyValuesDictionary.Add("hy", hy);
            char [] tibet = {'\u0F0B'};
            wordBreakPropertyValuesDictionary.Add("tibet", tibet);
            char [] mongolia = {'\u1806'};
            wordBreakPropertyValuesDictionary.Add("mongolia", mongolia);

            char [] all2 = {'\u000A', '\u000D', '\u0085', '\u00A0', '\u05F3', '\u2000', '\u2001', '\u2002', '\u2003', '\u2004', '\u2005', '\u2006', '\u2007',
                '\u2008', '\u2009', '\u200A', '\u2028', '\u2029', '\u002E', '\u2024', '\uFE52', '\uFF0E', '\u002D', '\u003A', '\u055D', '\u060C', '\u060D',
                '\u07F8', '\u1802', '\u1808', '\u2013', '\u2014', '\u3001', '\uFE10', '\uFE11', '\uFE13', '\uFE31', '\uFE32', '\uFE50', '\uFE51', '\uFE55', 
                '\uFE58', '\uFE63', '\uFF0C', '\uFF0D', '\uFF1A', '\uFF64'};
            sentenceBreakPropertyValuesDictionary.Add("all", all2);

            bool isValid = false;
            int i = 0;
            textSegmentationCodePoints = new int [ko.Length + ta.Length + th.Length + devanagari.Length + sk.Length + other.Length + all.Length + th1.Length
                + lao.Length + ko1.Length + all1.Length + katakana.Length + he.Length + hy.Length + tibet.Length + mongolia.Length + all2.Length];

            Dictionary<string, char[]>.ValueCollection valueColl1 = sampleGraphemeClusterDictionary.Values;
            foreach (char [] values in valueColl1)
            {
                foreach (char codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                         if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                         {
                             textSegmentationCodePoints[i++] = (int)codePoint;
                             isValid = true;
                         }
                    }
                }
            }
            
            Dictionary<string, char[]>.ValueCollection valueColl2 = graphemeClusterBreakPropertyValuesDictionary.Values;
            foreach (char [] values in valueColl2)
            {
                foreach (char codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                         if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                         {
                             textSegmentationCodePoints[i++] = (int)codePoint;
                             isValid = true;
                         }
                    }
                }
            }

            Dictionary<string, char[]>.ValueCollection valueColl3 = wordBreakPropertyValuesDictionary.Values;
            foreach(char [] values in valueColl3)
            {
                foreach (char codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                         if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                         {
                             textSegmentationCodePoints[i++] = (int)codePoint;
                             isValid = true;
                         }
                    }
                }
            }

            Dictionary<string, char[]>.ValueCollection valueColl4 = sentenceBreakPropertyValuesDictionary.Values;
            foreach(char [] values in valueColl4)
            {
                foreach (char codePoint in values)
                {
                    foreach (UnicodeRange range in expectedRanges)
                    {
                         if (codePoint >= range.StartOfUnicodeRange && codePoint <= range.EndOfUnicodeRange)
                         {
                             textSegmentationCodePoints[i++] = (int)codePoint;
                             isValid = true;
                         }
                    }
                }
            }
            Array.Resize(ref textSegmentationCodePoints, i);
            Array.Sort(textSegmentationCodePoints);
            
            return isValid;
        }

        /// <summary>
        /// Check if code point is in the property range
        /// </summary>
        public bool IsInPropertyRange(int codePoint)
        {
            bool isIn = false;
            foreach (UnicodeRangeProperty prop in textSegmentationRangeList)
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
        /// Get number code points
        /// </summary>
        public string GetRandomCodePoints(int numOfProperty, int seed)
        {
            if (numOfProperty < 1)
            {
                throw new ArgumentOutOfRangeException(
                    "TextSegmentationProperty, numOfProperty, " + numOfProperty + " cannot be less than one.");
            }

            string textSegmentationStr = string.Empty;
            Random rand = new Random(seed);
            for (int i= 0; i < numOfProperty; i++)
            {
                int index = rand.Next(0, textSegmentationCodePoints.Length);
                textSegmentationStr += TextUtil.IntToString(textSegmentationCodePoints[index]);
            }

            return textSegmentationStr;
        }
    }
}

