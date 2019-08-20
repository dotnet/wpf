// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//-----------------------------------------------------------------------
//
//
//  File:      TextAnalyzer.h
//
//  Contents:  This class is the entry point to itemization and shaping
//
//
//------------------------------------------------------------------------

#ifndef __TEXT_ANALYZER_H
#define __TEXT_ANALYZER_H

#include "Common.h"
#include "TextItemizer.h"
#include "FontFace.h"
#include "DWriteFontFeature.h"
#include "GlyphOffset.h"
#include "ItemSpan.h"
#include "ItemProps.h"
#include "IClassification.h"
#include "NativePointerWrapper.h"
#include "CharAttribute.h"

using namespace System::Windows::Media;
using namespace System::Runtime::InteropServices;
using namespace MS::Internal::Text::TextInterface::Generics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /*******************************************************************************************************************************/
    //Forward declaration of Factory since there was a circular reference between "TextAnalyzer" & "Factory"
    ref class Factory;
    /*******************************************************************************************************************************/  
    

    // The 4 delegates below are used to introduce a level of indirection so we can define
    // the external methods that reference PresentationNative*.dll in PresenationCore.dll.
    // The reason we define the methods in PresentationCore.dll is that the string values for the
    // current release version suffix and the dll name of PresentationNative are defined in managed code.
    // Hence we wanted to avoid redefining these values in MC++ so as not to increase the maintenance cost 
    // of the code. Moreover, using delegates does not impact perf to justify not using it in this case.
    private delegate int CreateTextAnalysisSource(
                                                  WCHAR const*    text,
                                                  UINT32          length,
                                                  WCHAR const*    culture,
                                                  void*           factory,
                                                  bool            isRightToLeft,
                                                  WCHAR const*    numberCulture,
                                                  bool            ignoreUserOverride,
                                                  UINT32          numberSubstitutionMethod,
                                                  void**          ppTextAnalysisSource);

    private delegate void* CreateTextAnalysisSink();

    private delegate void* GetScriptAnalysisList(void*);

    private delegate void* GetNumberSubstitutionList(void*);
    /// <summary>
    /// This class is responsible for Text Analysis and Shaping.
    /// For the most part it mirrors the DWrite IDWriteTextAnalyzer interface
    /// </summary>
    private ref class TextAnalyzer sealed
    {
        private:
        
            NativeIUnknownWrapper<IDWriteTextAnalyzer>^ _textAnalyzer;

            void GetBlankGlyphsForControlCharacters(
                __in_ecount(textLength) const WCHAR* pTextString,
                UINT32 textLength,
                FontFace^ fontFace,
                UINT16 blankGlyphIndex,
                UINT32 maxGlyphCount,
                __out_ecount(textLength) UINT16* clusterMap,
                __out_ecount(maxGlyphCount) UINT16* glyphIndices,
                __out_ecount(textLength) int* pfCanGlyphAlone,
                [System::Runtime::InteropServices::Out] UINT32% actualGlyphCount
            );

            void GetGlyphPlacementsForControlCharacters(
                __in_ecount(textLength) const WCHAR* pTextString,
                UINT32 textLength,
                Font^ font,
                TextFormattingMode textFormattingMode,
                double fontEmSize,
                double scalingFactor,
                bool isSideways,
                float pixelsPerDip,
                UINT32 glyphCount,
                __in_ecount(glyphCount) UINT16 const* pGlyphIndices,
                __out_ecount(glyphCount) int* glyphAdvances,
                [System::Runtime::InteropServices::Out] array<GlyphOffset>^% glyphOffsets
                );

            static void ReleaseItemizationNativeResources(
                IDWriteFactory**            ppFactory,
                IDWriteTextAnalyzer**       ppTextAnalyzer,
                IDWriteTextAnalysisSource** ppTextAnalysisSource,
                IDWriteTextAnalysisSink**   ppTextAnalysisSink
                )
            {
                if(ppFactory != NULL && (*ppFactory)!= NULL)
                {
                    (*ppFactory)->Release();
                    (*ppFactory) = NULL;
                }
                if(ppTextAnalyzer!= NULL && (*ppTextAnalyzer)!= NULL)
                {
                    (*ppTextAnalyzer)->Release();
                    (*ppTextAnalyzer) = NULL;
                }
                if(ppTextAnalysisSource != NULL && (*ppTextAnalysisSource)!= NULL)
                {
                    (*ppTextAnalysisSource)->Release();
                    (*ppTextAnalysisSource) = NULL;
                }
                if (ppTextAnalysisSink != NULL && (*ppTextAnalysisSink)!= NULL)
                {
                    (*ppTextAnalysisSink)->Release();
                    (*ppTextAnalysisSink) = NULL;
                }
            }

            static IList<Span^>^ AnalyzeExtendedAndItemize(
                TextItemizer^ textItemizer, 
                __in_ecount(length) const WCHAR *text, 
                UINT32 length, 
                CultureInfo^ numberCulture, 
                IClassification^ classification
                );

            // We would prefer to wrap the member access as a getter on ItemProps
            // but exposing DWRITE_SCRIPT_SHAPES on any ItemProps API signature causes asmmeta generation errors.
            static DWRITE_SCRIPT_SHAPES GetScriptShapes(ItemProps^ itemProps);

        internal:

            // This constant is used in
            // Core\CSharp\System\Windows\Media\textformatting\TextFormatterContext.cs
            // It is passed to LS to replace soft hyphens when needed.
            static const System::Char CharHyphen = '\x002d';
            
            /// <summary>
            /// Contructs a Font object.
            /// </summary>
            /// <param name="font">The DWrite font object that this class wraps.</param>
            TextAnalyzer(
                IDWriteTextAnalyzer* textAnalyzer
                );

            static IList<Span^>^ Itemize(
                __in_ecount(length) const WCHAR* text,
                UINT32                     length,
                CultureInfo^               culture,
                Factory^                   factory,
                bool                       isRightToLeftParagraph,
                CultureInfo^               numberCulture,
                bool                       ignoreUserOverride,
                UINT32                     numberSubstitutionMethod,
                IClassification^           classificationUtility,
                CreateTextAnalysisSink^    pfnCreateTextAnalysisSink,
                GetScriptAnalysisList^     pfnGetScriptAnalysisList,
                GetNumberSubstitutionList^ pfnGetNumberSubstitutionList,
                CreateTextAnalysisSource^  pfnCreateTextAnalysisSource
                );

            static void AnalyzeExtendedCharactersAndDigits(
                __in_ecount(length) const WCHAR* text,
                UINT32            length,
                TextItemizer^     textItemizer,
                __out_ecount(length) CharAttributeType* pCharAttribute,
                CultureInfo^      numberCulture,
                IClassification^  classificationUtility
                );

            void GetGlyphsAndTheirPlacements(
                __in_ecount(textLength) const WCHAR* textString,
                UINT32 textLength,
                Font^ font,
                UINT16 blankGlyphIndex,
                bool isSideways,
                bool isRightToLeft,
                CultureInfo^ cultureInfo,
                array<array<DWriteFontFeature>^>^ features,
                array<UINT32>^ featureRangeLengths,
                double fontEmSize,
                double scalingFactor,
                float pixelsPerDip,
                TextFormattingMode textFormattingMode,
                ItemProps^ itemProps,
                [System::Runtime::InteropServices::Out] array<unsigned short>^% clusterMap,
                [System::Runtime::InteropServices::Out] array<unsigned short>^% glyphIndices,
                [System::Runtime::InteropServices::Out] array<int>           ^% glyphAdvances,
                [System::Runtime::InteropServices::Out] array<GlyphOffset>   ^% glyphOffsets
            );

            void GetGlyphs(
                __in_ecount(textLength) const WCHAR* textString,
                UINT32 textLength,
                Font^ font,
                UINT16 blankGlyphIndex,
                bool isSideways,
                bool isRightToLeft,
                CultureInfo^ cultureInfo,
                array<array<DWriteFontFeature>^>^ features,
                array<UINT32>^ featureRangeLengths,
                UINT32 maxGlyphCount,
                TextFormattingMode textFormattingMode,
                ItemProps^ itemProps,
                __out_ecount(textLength) UINT16* clusterMap,
                __out_ecount(textLength) UINT16* textProps,
                __out_ecount(maxGlyphCount) UINT16* glyphIndices,
                __out_ecount(maxGlyphCount) UINT32* glyphProps,
                __out_ecount(textLength) int* pfCanGlyphAlone,
                [System::Runtime::InteropServices::Out] UINT32% actualGlyphCount
            );

            void GetGlyphPlacements(
                __in_ecount(textLength) const WCHAR* textString,
                __in_ecount(textLength) UINT16 const* clusterMap,
                __in_ecount(textLength) UINT16* textProps,
                UINT32 textLength,
                __in_ecount(glyphCount) UINT16 const* glyphIndices,
                __in_ecount(glyphCount) UINT32* glyphProps,
                UINT32 glyphCount,
                Font^ font,
                double fontEmSize,
                double scalingFactor,
                bool isSideways,
                bool isRightToLeft,
                CultureInfo^ cultureInfo,
                array<array<DWriteFontFeature>^>^ features,
                array<UINT32>^ featureRangeLengths,
                TextFormattingMode textFormattingMode,
                ItemProps^ itemProps,
                float pixelsPerDip,
                __out_ecount(glyphCount) int* glyphAdvances,
                [System::Runtime::InteropServices::Out] array<GlyphOffset>^% glyphOffsets
                );
        };           
}}}}//MS::Internal::Text::TextInterface

#endif //__TEXT_ANALYZER_H
