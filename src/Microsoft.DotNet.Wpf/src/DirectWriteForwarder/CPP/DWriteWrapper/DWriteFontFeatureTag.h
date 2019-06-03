// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFEATURETAG_H
#define __FONTFEATURETAG_H

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    
    /// <summary>
    /// Typographic feature of text supplied by the font.
    /// </summary>
    private enum class DWriteFontFeatureTag
    {
        AlternativeFractions                    = 0x63726661, // 'afrc'
        PetiteCapitalsFromCapitals              = 0x63703263, // 'c2pc'
        SmallCapitalsFromCapitals               = 0x63733263, // 'c2sc'
        ContextualAlternates                    = 0x746c6163, // 'calt'
        CaseSensitiveForms                      = 0x65736163, // 'case'
        GlyphCompositionDecomposition           = 0x706d6363, // 'ccmp'
        ContextualLigatures                     = 0x67696c63, // 'clig'
        CapitalSpacing                          = 0x70737063, // 'cpsp'
        ContextualSwash                         = 0x68777363, // 'cswh'
        CursivePositioning                      = 0x73727563, // 'curs'
        Default                                 = 0x746c6664, // 'dflt'
        DiscretionaryLigatures                  = 0x67696c64, // 'dlig'
        ExpertForms                             = 0x74707865, // 'expt'
        Fractions                               = 0x63617266, // 'frac'
        FullWidth                               = 0x64697766, // 'fwid'
        HalfForms                               = 0x666c6168, // 'half'
        HalantForms                             = 0x6e6c6168, // 'haln'
        AlternateHalfWidth                      = 0x746c6168, // 'halt'
        HistoricalForms                         = 0x74736968, // 'hist'
        HorizontalKanaAlternates                = 0x616e6b68, // 'hkna'
        HistoricalLigatures                     = 0x67696c68, // 'hlig'
        HalfWidth                               = 0x64697768, // 'hwid'
        HojoKanjiForms                          = 0x6f6a6f68, // 'hojo'
        JIS04Forms                              = 0x3430706a, // 'jp04'
        JIS78Forms                              = 0x3837706a, // 'jp78'
        JIS83Forms                              = 0x3338706a, // 'jp83'
        JIS90Forms                              = 0x3039706a, // 'jp90'
        Kerning                                 = 0x6e72656b, // 'kern'
        StandardLigatures                       = 0x6167696c, // 'liga'
        LiningFigures                           = 0x6d756e6c, // 'lnum'
        LocalizedForms                          = 0x6c636f6c, // 'locl'
        MarkPositioning                         = 0x6b72616d, // 'mark'
        MathematicalGreek                       = 0x6b72676d, // 'mgrk'
        MarkToMarkPositioning                   = 0x6b6d6b6d, // 'mkmk'
        AlternateAnnotationForms                = 0x746c616e, // 'nalt'
        NLCKanjiForms                           = 0x6b636c6e, // 'nlck'
        OldStyleFigures                         = 0x6d756e6f, // 'onum'
        Ordinals                                = 0x6e64726f, // 'ordn'
        ProportionalAlternateWidth              = 0x746c6170, // 'palt'
        PetiteCapitals                          = 0x70616370, // 'pcap'
        ProportionalFigures                     = 0x6d756e70, // 'pnum'
        ProportionalWidths                      = 0x64697770, // 'pwid'
        QuarterWidths                           = 0x64697771, // 'qwid'
        RequiredLigatures                       = 0x67696c72, // 'rlig'
        RubyNotationForms                       = 0x79627572, // 'ruby'
        StylisticAlternates                     = 0x746c6173, // 'salt'
        ScientificInferiors                     = 0x666e6973, // 'sinf'
        SmallCapitals                           = 0x70636d73, // 'smcp'
        SimplifiedForms                         = 0x6c706d73, // 'smpl'
        StylisticSet1                           = 0x31307373, // 'ss01'
        StylisticSet2                           = 0x32307373, // 'ss02'
        StylisticSet3                           = 0x33307373, // 'ss03'
        StylisticSet4                           = 0x34307373, // 'ss04'
        StylisticSet5                           = 0x35307373, // 'ss05'
        StylisticSet6                           = 0x36307373, // 'ss06'
        StylisticSet7                           = 0x37307373, // 'ss07'
        StylisticSet8                           = 0x38307373, // 'ss08'
        StylisticSet9                           = 0x39307373, // 'ss09'
        StylisticSet10                          = 0x30317373, // 'ss10'
        StylisticSet11                          = 0x31317373, // 'ss11'
        StylisticSet12                          = 0x32317373, // 'ss12'
        StylisticSet13                          = 0x33317373, // 'ss13'
        StylisticSet14                          = 0x34317373, // 'ss14'
        StylisticSet15                          = 0x35317373, // 'ss15'
        StylisticSet16                          = 0x36317373, // 'ss16'
        StylisticSet17                          = 0x37317373, // 'ss17'
        StylisticSet18                          = 0x38317373, // 'ss18'
        StylisticSet19                          = 0x39317373, // 'ss19'
        StylisticSet20                          = 0x30327373, // 'ss20'
        Subscript                               = 0x73627573, // 'subs'
        Superscript                             = 0x73707573, // 'sups'
        Swash                                   = 0x68737773, // 'swsh'
        Titling                                 = 0x6c746974, // 'titl'
        TraditionalNameForms                    = 0x6d616e74, // 'tnam'
        TabularFigures                          = 0x6d756e74, // 'tnum'
        TraditionalForms                        = 0x64617274, // 'trad'
        ThirdWidths                             = 0x64697774, // 'twid'
        Unicase                                 = 0x63696e75, // 'unic'
        SlashedZero                             = 0x6f72657a, // 'zero'
    };
}}}}

#endif //__FONTFEATURETAG_H