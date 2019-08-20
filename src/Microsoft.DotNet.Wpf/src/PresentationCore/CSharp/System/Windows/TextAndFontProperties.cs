// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Public enumeration of text and font properties. 
//

//---------------------------------------------------------------------------

using System;

namespace System.Windows
{
    #region Text formatting properties

    /// <summary>
    /// Enum specifying where a box should be positioned Vertically
    /// </summary>
    public enum BaselineAlignment
    {
        /// <summary>Align top toward top of container</summary>
        Top,

        /// <summary>Center vertically</summary>
        Center,
        
        /// <summary>Align bottom toward bottom of container</summary>
        Bottom,
        
        /// <summary>Align at baseline</summary>
        Baseline,
        
        /// <summary>Align toward text's top of container</summary>
        TextTop,
        
        /// <summary>Align toward text's bottom of container</summary>
        TextBottom,
        
        /// <summary>Align baseline to subscript position of container</summary>
        Subscript,
        
        /// <summary>Align baseline to superscript position of container</summary>
        Superscript,
    }



    /// <summary>
    /// This property describes how content of a block is aligned.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]
    public enum TextAlignment
    {
        /// <summary>
        /// In horizontal inline progression, the text is aligned on the left.
        /// </summary>
        Left,

        /// <summary>
        /// In horizontal inline progression, the text is aligned on the right.
        /// </summary>
        Right,

        /// <summary>
        /// The text is center aligned.
        /// </summary>
        Center,

        /// <summary>
        /// The text is justified.
        /// </summary>
        Justify,
    }



    /// <summary>
    /// The 'flow-direction' property specifies whether the primary text advance
    /// direction shall be left-to-right or right-to-left.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]    
    public enum FlowDirection
    {
        /// <internalonly>
        /// Sets the primary text advance direction to left-to-right, and the line
        /// progression direction to top-to-bottom as is common in most Roman-based
        /// documents. For most characters, the current text position is advanced
        /// from left to right after each glyph is rendered. The 'direction' property
        /// is set to 'ltr'.
        /// </internalonly>
        LeftToRight,

        /// <internalonly>
        /// Sets the primary text advance direction to right-to-left, and the line
        /// progression direction to top-to-bottom as is common in Arabic or Hebrew
        /// scripts. The direction property is set to 'rtl'.
        /// </internalonly>
        RightToLeft,
    }


    /// <summary>
    /// Breaking condition around inline object
    /// </summary>
    /// <remarks>
    ///                   | BreakDesired | BreakPossible | BreakRestrained | BreakAlways |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakDesired     |     TRUE     |     TRUE      |      FALSE      |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakPossible    |     TRUE     |     FALSE     |      FALSE      |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakRestrained  |     FALSE    |     FALSE     |      FALSE      |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    ///  BreakAlways      |     TRUE     |     TRUE      |      TRUE       |    TRUE     |
    /// ------------------+--------------+---------------+-----------------+-------------|
    /// </remarks>
    public enum LineBreakCondition
    {
        /// <summary>
        /// Break if not prohibited by other
        /// </summary>
        BreakDesired,

        /// <summary>
        /// Break if allowed by other
        /// </summary>
        BreakPossible,

        /// <summary>
        /// Break prohibited always
        /// </summary>
        BreakRestrained,

        /// <summary>
        /// Break allowed always
        /// </summary>
        BreakAlways
    }


    /// <summary>
    /// This property determines the appearance of the list item's marker
    /// </summary>
    public enum TextMarkerStyle
    {
        /// <summary>
        /// No marker
        /// </summary>
        None, 

        /// <summary>
        /// Solid disc circle
        /// </summary>
        Disc,

        /// <summary>
        /// Hallow disc circle
        /// </summary>
        Circle,

        /// <summary>
        /// Hallow square shape
        /// </summary>
        Square,

        /// <summary>
        /// Solid square shape
        /// </summary>
        Box,

        /// <summary>
        /// Lower roman letter e.g. i, ii, iii, iv, etc.
        /// </summary>
        LowerRoman, 

        /// <summary>
        /// Upper roman letter e.g. I, II, III, IV, etc.
        /// </summary>
        UpperRoman, 

        /// <summary>
        /// Lowercase ascii e.g. a, b, c, etc.
        /// </summary>
        LowerLatin, 

        /// <summary>
        /// Uppercase ascii e.g. A, B, C, etc.
        /// </summary>
        UpperLatin, 

        /// <summary>
        /// Decimal numbers, beginning with 1
        /// </summary>
        Decimal,
    }


    /// <summary>
    /// This property controls whether or not text wraps when it reaches the edge 
    /// of its containing box 
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]        
    public enum TextWrapping
    {
        /// <summary>
        /// Line-breaking occurs if the line overflows the available block width.
        /// However, a line may overflow the block width if the line breaking algorithm
        /// cannot determine a break opportunity, as in the case of a very long word.
        /// </summary>
        WrapWithOverflow,

        /// <summary>
        /// No line wrapping is performed. In the case when lines are longer than the 
        /// available block width, the overflow will be treated in accordance with the 
        /// 'overflow' property specified in the element.
        /// </summary>
        NoWrap,

        /// <summary>
        /// Line-breaking occurs if the line overflow the available block width, even 
        /// if the standard line breaking algorithm cannot determine any opportunity. 
        /// For example, this deals with the situation of very long words constrained in 
        /// a fixed-width container with no scrolling allowed.
        /// </summary>
        Wrap,
    }

    
    /// <summary>
    /// This property determines how text is trimmed when it overflows the edge of its
    /// containing box.
    /// </summary>
    [Localizability(LocalizationCategory.None, Readability = Readability.Unreadable)]    
    public enum TextTrimming
    {
        /// <summary>
        /// Default no trimming
        /// </summary>
        None,

        /// <summary>
        /// Text is trimmed at character boundary. Ellipsis is drawn in place of invisible part.
        /// </summary>
        CharacterEllipsis,

        /// <summary>
        /// Text is trimmed at word boundary. Ellipsis is drawn in place of invisible part.
        /// </summary>
        WordEllipsis,
    }

    #endregion  // Text properties


    #region Typography properties

    /// <summary>
    /// Font typographic variants
    /// </summary>
    public enum FontVariants
    { 
        /// <summary>
        /// Variant normal
        /// </summary>
        Normal, 
        /// <summary>
        /// Superscript
        /// </summary>
        Superscript, 
        /// <summary>
        /// Subscript
        /// </summary>
        Subscript, 
        /// <summary>
        /// Ordinal
        /// </summary>
        Ordinal, 
        /// <summary>
        /// Inferior
        /// </summary>
        Inferior, 
        /// <summary>
        /// Ruby
        /// </summary>
        Ruby 
    }


    /// <summary>
    /// Font typographic capital treatment
    /// </summary>
    public enum FontCapitals
    { 
        /// <summary>
        /// Capitals normal
        /// </summary>
        Normal, 
        /// <summary>
        /// Capitals all small caps
        /// </summary>
        AllSmallCaps, 
        /// <summary>
        /// Capitals small caps
        /// </summary>
        SmallCaps, 
        /// <summary>
        /// Capitals all petite caps
        /// </summary>
        AllPetiteCaps, 
        /// <summary>
        /// Capitals petite caps
        /// </summary>
        PetiteCaps, 
        /// <summary>
        /// Capitals unicase
        /// </summary>
        Unicase, 
        /// <summary>
        /// Capitals titling
        /// </summary>
        Titling 
    }

    /// <summary>
    /// Font typographic fraction style
    /// </summary>
    public enum FontFraction
    {
        /// <summary>
        /// Default
        /// </summary>
        Normal,
        /// <summary>
        /// Slashed fraction
        /// </summary>
        Slashed,
        /// <summary>
        /// Stacked fraction
        /// </summary>
        Stacked
    }

    /// <summary>
    /// Font typographic numeral style types
    /// </summary>
    public enum FontNumeralStyle
    { 
        /// <summary>
        /// Numeral style normal
        /// </summary>
        Normal, 
        /// <summary>
        /// Numeral style lining
        /// </summary>
        Lining, 
        /// <summary>
        /// Numeral style old style
        /// </summary>
        OldStyle 
    }


    /// <summary>
    /// Font typographic numeral alignment types
    /// </summary>
    public enum FontNumeralAlignment
    { 
        /// <summary>
        /// Numeral alignment normal
        /// </summary>
        Normal, 
        /// <summary>
        /// Numeral alignment proportional
        /// </summary>
        Proportional, 
        /// <summary>
        /// Numeral alignment tabulr
        /// </summary>
        Tabular 
    }


    /// <summary>
    /// Font East Asian width types
    /// </summary>
    public enum FontEastAsianWidths
    { 
        /// <summary>
        /// East Asian width normal
        /// </summary>
        Normal, 
        /// <summary>
        /// East Asian width proportional
        /// </summary>
        Proportional, 
        /// <summary>
        /// East Asian width full
        /// </summary>
        Full, 
        /// <summary>
        /// East Asian width one half of full
        /// </summary>
        Half, 
        /// <summary>
        /// East Asian width one third of full
        /// </summary>
        Third, 
        /// <summary>
        /// East Asian width one quarter of full
        /// </summary>
        Quarter 
    }


    /// <summary>
    /// Font East Asian language types
    /// </summary>
    public enum FontEastAsianLanguage
    { 
        /// <summary>
        /// East Asian language normal
        /// </summary>
        Normal, 
        /// <summary>
        /// East Asian language follows JIS-78
        /// </summary>
        Jis78, 
        /// <summary>
        /// East Asian language follows JIS-83
        /// </summary>
        Jis83, 
        /// <summary>
        /// East Asian language follows JIS-90
        /// </summary>
        Jis90,
        /// <summary>
        /// East Asian language follows JIS-04
        /// </summary>
        Jis04,
        /// <summary>
        /// East Asian language follows Hojo Kanji (JIS X 0212:1990)
        /// </summary>
        HojoKanji,
        /// <summary>
        /// East Asian language follows National Language Committee for Kanji
        /// </summary>
        NlcKanji, 
        /// <summary>
        /// East Asian language is simplified forms
        /// </summary>
        Simplified, 
        /// <summary>
        /// East Asian language is traditional forms
        /// </summary>
        Traditional, 
        /// <summary>
        /// East Asian language is traditional name forms
        /// </summary>
        TraditionalNames 
    }

    #endregion // Typography properties
}
