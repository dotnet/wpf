// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTWEIGHT_H
#define __FONTWEIGHT_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The font weight enumeration describes common values for degree of blackness or thickness of strokes of characters in a font.
    /// Font weight values less than 1 or greater than 999 are considered to be invalid, and they are rejected by font API functions.
    /// </summary>
    private enum class FontWeight
    {
        /// <summary>
        /// Predefined font weight : Thin (100).
        /// </summary>
        Thin = 100,

        /// <summary>
        /// Predefined font weight : Extra-light (200).
        /// </summary>
        ExtraLight = 200,

        /// <summary>
        /// Predefined font weight : Ultra-light (200).
        /// </summary>
        UltraLight = 200,

        /// <summary>
        /// Predefined font weight : Light (300).
        /// </summary>
        Light = 300,

        /// <summary>
        /// Predefined font weight : Normal (400).
        /// </summary>
        Normal = 400,

        /// <summary>
        /// Predefined font weight : Regular (400).
        /// </summary>
        Regular = 400,

        /// <summary>
        /// Predefined font weight : Medium (500).
        /// </summary>
        Medium = 500,

        /// <summary>
        /// Predefined font weight : Demi-bold (600).
        /// </summary>
        DemiBold = 600,

        /// <summary>
        /// Predefined font weight : Semi-bold (600).
        /// </summary>
        SemiBOLD = 600,

        /// <summary>
        /// Predefined font weight : Bold (700).
        /// </summary>
        Bold = 700,

        /// <summary>
        /// Predefined font weight : Extra-bold (800).
        /// </summary>
        ExtraBold = 800,

        /// <summary>
        /// Predefined font weight : Ultra-bold (800).
        /// </summary>
        UltraBold = 800,

        /// <summary>
        /// Predefined font weight : Black (900).
        /// </summary>
        Black = 900,

        /// <summary>
        /// Predefined font weight : Heavy (900).
        /// </summary>
        Heavy = 900,

        /// <summary>
        /// Predefined font weight : Extra-black (950).
        /// </summary>
        ExtraBlack = 950,

        /// <summary>
        /// Predefined font weight : Ultra-black (950).
        /// </summary>
        UltraBlack = 950
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FONTWEIGHT_H