// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;


namespace Microsoft.Test.Text
{
    /// <summary>
    /// Collect data according to name and save to PropertyData list
    /// </summary>
    internal static class RangePropertyCollector
    {
        /// <summary>
        /// Get Unicode range according to Unicode chart provided
        /// </summary>
        public static UnicodeRange GetUnicodeChartRange(UnicodeRangeDatabase unicodeDb, UnicodeChart chart)
        {
            foreach (Group script in unicodeDb.Scripts)
            {
                if (script.UnicodeChart == chart)
                {
                    return script.UnicodeRange;
                }

                if (null != script.SubGroups)
                {
                    foreach (SubGroup subScript in script.SubGroups)
                    {
                        if (subScript.UnicodeChart == chart)
                        {
                            return subScript.UnicodeRange;
                        }
                    }
                }
            }

            foreach (Group symbol in unicodeDb.SymbolsAndPunctuation)
            {
                if (symbol.UnicodeChart == chart)
                {
                    return symbol.UnicodeRange;
                }

                if (null != symbol.SubGroups)
                {
                    foreach (SubGroup subSymbol in symbol.SubGroups)
                    {
                        if (subSymbol.UnicodeChart == chart)
                        {
                            return subSymbol.UnicodeRange;
                        }
                    }
                }
            }

            throw new ArgumentException(@"Invalid UnicodeChart, " + Enum.GetName(typeof(UnicodeChart), chart) + ". No match in the database.");
        }

        /// <summary>
        /// Get new range - if expectedRange is smaller, new range is expectedRange. Otherwise, return false
        /// </summary>
        public static UnicodeRange GetRange(UnicodeRange range, UnicodeRange expectedRange)
        {
            if (0 == expectedRange.StartOfUnicodeRange && TextUtil.MaxUnicodePoint == expectedRange.EndOfUnicodeRange)
            {
                // don't care if whole Unicode range is given
                return new UnicodeRange(range.StartOfUnicodeRange,range.EndOfUnicodeRange);
            }

            if (expectedRange.StartOfUnicodeRange > range.EndOfUnicodeRange || expectedRange.EndOfUnicodeRange < range.StartOfUnicodeRange) return null;

            int low = expectedRange.StartOfUnicodeRange > range.StartOfUnicodeRange ? expectedRange.StartOfUnicodeRange : range.StartOfUnicodeRange;
            int high = expectedRange.EndOfUnicodeRange < range.EndOfUnicodeRange ? expectedRange.EndOfUnicodeRange : range.EndOfUnicodeRange;
            return new UnicodeRange(low, high);
        }
        
        /// <summary>
        /// Walk through Unicode range database to build up property according to Group attribute
        /// </summary>
        public static bool BuildPropertyDataList(
            UnicodeRangeDatabase unicodeDb,
            UnicodeRange expectedRange,
            List<UnicodeRangeProperty> dataList,
            string name,
            GroupAttributes attribute)
        {
            bool isAdded = false;
            
            foreach (Group script in unicodeDb.Scripts)
            {
                string scriptAttrib = script.GroupName;
                if (attribute == GroupAttributes.Name) scriptAttrib = script.Name;
                else if (attribute == GroupAttributes.Ids) scriptAttrib = script.Ids;
                
                if (scriptAttrib.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    UnicodeRange range = GetRange(script.UnicodeRange, expectedRange);
                    if (null != range)
                    {
                        dataList.Add(new UnicodeRangeProperty(TextUtil.UnicodeChartType.Script, script.Name, script.Ids, range));
                        isAdded = true;
                    }

                    if (null != script.SubGroups)
                    {
                        foreach (SubGroup subScript in script.SubGroups)
                        {
                            range = GetRange(subScript.UnicodeRange, expectedRange);
                            if (null != range)
                            {
                                dataList.Add(new UnicodeRangeProperty(
                                    TextUtil.UnicodeChartType.Script,
                                    subScript.SubGroupName, 
                                    subScript.SubIds, 
                                    range));
                                isAdded = true;
                            }
                        }
                    }
                }
            }
            
            foreach (Group symbol in unicodeDb.SymbolsAndPunctuation)
            {
                string symbolAttrib = symbol.GroupName;
                if (attribute == GroupAttributes.Name) symbolAttrib = symbol.Name;
                else if (attribute == GroupAttributes.Ids) symbolAttrib = symbol.Ids;
                
                if (symbolAttrib.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    TextUtil.UnicodeChartType type = TextUtil.UnicodeChartType.Other;
                    if ((symbol.GroupName.ToLower(CultureInfo.InvariantCulture)).Contains("symbols") || 
                        (symbol.Name.ToLower(CultureInfo.InvariantCulture)).Contains("symbols"))
                    {
                        type = TextUtil.UnicodeChartType.Symbol;
                    }
                    else if((symbol.GroupName.ToLower(CultureInfo.InvariantCulture)).Contains("punctuation") || 
                        (symbol.Name.ToLower(CultureInfo.InvariantCulture)).Contains("punctuation"))
                    {
                        type = TextUtil.UnicodeChartType.Punctuation;
                    }

                    UnicodeRange range = GetRange(symbol.UnicodeRange, expectedRange);
                    if (null != range)
                    {
                        dataList.Add(new UnicodeRangeProperty(type, symbol.Name, symbol.Ids, range));
                        isAdded = true;
                    }

                    if (null != symbol.SubGroups)
                    {
                        foreach (SubGroup subSymbol in symbol.SubGroups)
                        {
                            range = GetRange(subSymbol.UnicodeRange, expectedRange);
                            if (null != range)
                            {
                                dataList.Add(new UnicodeRangeProperty(type, subSymbol.SubGroupName, subSymbol.SubIds, range));
                                isAdded = true;
                            }
                        }
                    }
                }
            }
            return isAdded;
        }
    }
}

