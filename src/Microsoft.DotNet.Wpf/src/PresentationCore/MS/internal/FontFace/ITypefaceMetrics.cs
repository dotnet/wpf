// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// 
//
//  Contents:  Base definition of typeface metrics and properties.
//
//


using System.Windows.Media;
using System.Windows.Markup;    // for XmlLanguage


namespace MS.Internal.FontFace
{
    /// <summary>
    /// Font metrics
    /// </summary>
    internal interface ITypefaceMetrics
    {
        /// <summary>
        /// (Western) x-height relative to em size.
        /// </summary>
        double XHeight { get; }


        /// <summary>
        /// Distance from baseline to top of English capital, relative to em size.
        /// </summary>
        double CapsHeight { get; }


        /// <summary>
        /// Distance from baseline to underline position
        /// </summary>
        double UnderlinePosition { get; }


        /// <summary>
        /// Underline thickness
        /// </summary>
        double UnderlineThickness { get; }


        /// <summary>
        /// Distance from baseline to strike-through position
        /// </summary>
        double StrikethroughPosition { get; }


        /// <summary>
        /// strike-through thickness
        /// </summary>
        double StrikethroughThickness { get; }


        /// <summary>
        /// Flag indicating if the font is symbol font
        /// </summary>
        bool Symbol { get; }

        /// <summary>
        /// Style simulation flags for this typeface.
        /// </summary>
        StyleSimulations StyleSimulations { get; }

        /// <summary>
        /// Collection of localized face names adjusted by the font differentiator.
        /// </summary>
        IDictionary<XmlLanguage, string> AdjustedFaceNames { get; }
    }
}
