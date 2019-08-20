// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//
//
//  Contents:  Base definition of font family
//
//

using System;
using System.Diagnostics;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Markup;    // for XmlLanguage
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace MS.Internal.FontFace
{
    internal interface IFontFamily
    {
        /// <summary>
        /// Font family name table indexed by culture
        /// </summary>
        IDictionary<XmlLanguage, string> Names
        { get; }


        /// <summary>
        /// Distance from character cell top to English baseline relative to em size. 
        /// </summary>
        double Baseline(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode);

        double BaselineDesign
        {get;}
    

        /// <summary>
        /// Recommended baseline-to-baseline distance for text in this font
        /// </summary>
        double LineSpacing(double emSize, double toReal, double pixelsPerDip, TextFormattingMode textFormattingMode);

        double LineSpacingDesign
        { get; }
        
        

        /// <summary>
        /// Get typeface metrics of the specified style
        /// </summary>
        /// <param name="style">font style</param>
        /// <param name="weight">font weight</param>
        /// <param name="stretch">font stretch</param>
        /// <returns>typeface metrics</returns>
        ITypefaceMetrics GetTypefaceMetrics(
            FontStyle       style,
            FontWeight      weight,
            FontStretch     stretch
            );


        /// <summary>
        /// Gets the device font (if any) for the given style, weight, and stretch.
        /// </summary>
        IDeviceFont GetDeviceFont(FontStyle style, FontWeight weight, FontStretch stretch);

        
        /// <summary>
        /// Get family name correspondent to the first n-characters of the specified character string
        /// </summary>
        /// <param name="unicodeString">character string</param>
        /// <param name="culture">text culture info</param>
        /// <param name="digitCulture">culture used for digit subsitution or null</param>
        /// <param name="defaultSizeInEm">default size relative to em</param>
        /// <param name="cchAdvance">number of characters advanced</param>
        /// <param name="targetFamilyName">target family name</param>
        /// <param name="scaleInEm">size relative to em</param>
        /// <returns>number of character sharing the same family name and size</returns>
        /// <remarks>
        /// 
        /// Null target family name returned indicates that the font family cannot find target
        /// name of the character range being advanced.
        /// 
        /// Return value false indicates that the font family has no character map. 
        /// It is a font face family.
        /// 
        /// </remarks>
        bool GetMapTargetFamilyNameAndScale(
            CharacterBufferRange unicodeString,
            CultureInfo          culture,
            CultureInfo          digitCulture,
            double               defaultSizeInEm,
            out int              cchAdvance,
            out string           targetFamilyName,
            out double           scaleInEm
            );


        /// <summary>
        /// Returns the collection of typefaces supported by this font family.
        /// For composite font families we return a union of typefaces supported by nested families.
        /// </summary>
        /// <param name="familyIdentifier">Base URI and friendly family name to construct the resulting Typefaces from.</param>
        /// <returns>A collection of Typefaces supported.</returns>
        ICollection<Typeface> GetTypefaces(FontFamilyIdentifier familyIdentifier);
    }
}
