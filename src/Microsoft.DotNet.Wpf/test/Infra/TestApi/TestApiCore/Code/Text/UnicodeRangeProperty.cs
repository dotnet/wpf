// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Test.Text
{
    /// <summary>
    /// UnicodeRangeProperty class to store data for a property
    /// </summary>
    internal class UnicodeRangeProperty
    {
        private TextUtil.UnicodeChartType type;
        public string cultureIds;

        /// <summary>
        /// constructor of PropertyData stuct
        /// </summary>
        public UnicodeRangeProperty(TextUtil.UnicodeChartType type, string name, string ids, UnicodeRange range)
        {
            Type = type;
            Name = name;
            CultureIDs = ids;
            Range = new UnicodeRange(range.StartOfUnicodeRange, range.EndOfUnicodeRange);
        }

        /// <summary>
        /// Default constructor to null or zero all attributes
        /// </summary>
        public UnicodeRangeProperty()
        {
            Type = TextUtil.UnicodeChartType.Other;
            Name = null;
            CultureIDs = null;
            Range = new UnicodeRange(0, 0);
        }
        
        /// <summary>
        /// type of the property
        /// </summary>
        public TextUtil.UnicodeChartType Type { set { type = value; } }

        /// <summary>
        /// name of the property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// culuture IDs for the property
        /// </summary>
        public string CultureIDs { set { cultureIds = value; } }

        /// <summary>
        /// UnicodeRange for the property
        /// </summary>
        public UnicodeRange Range { get; set; }
    }
}

