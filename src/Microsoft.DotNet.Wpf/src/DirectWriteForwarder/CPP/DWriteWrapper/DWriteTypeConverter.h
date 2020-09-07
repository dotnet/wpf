// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __DWRITETYPECONVERTER_H
#define __DWRITETYPECONVERTER_H

#include "Common.h"
#include "FactoryType.h"
#include "FontWeight.h"
#include "FontFaceType.h"
#include "FontFileType.h"
#include "FontSimulation.h"
#include "FontStretch.h"
#include "FontStyle.h"
#include "FontMetrics.h"
#include "GlyphMetrics.h"
#include "DWriteMatrix.h"
#include "DWriteGlyphOffset.h"
#include "InformationalStringID.h"

using namespace System::Windows::Media;
namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// This class is used to convert data types back anf forth between DWrite and DWriteWrapper.
    /// </summary>
    private ref class DWriteTypeConverter sealed
    {
        internal:

            static DWRITE_FACTORY_TYPE            Convert(FactoryType factoryType);
            static FontWeight                     Convert(DWRITE_FONT_WEIGHT fontWeight);
            static DWRITE_FONT_WEIGHT             Convert(FontWeight fontWeight);
            static FontFileType                   Convert(DWRITE_FONT_FILE_TYPE dwriteFontFileType);
            static FontSimulations                Convert(DWRITE_FONT_SIMULATIONS fontSimulations);
            static unsigned char                  Convert(FontSimulations fontSimulations);
            static DWRITE_FONT_FACE_TYPE          Convert(FontFaceType fontFaceType);
            static FontFaceType                   Convert(DWRITE_FONT_FACE_TYPE fontFaceType);
            static FontStretch                    Convert(DWRITE_FONT_STRETCH fontStrech);
            static DWRITE_FONT_STRETCH            Convert(FontStretch fontStrech);
            static FontStyle                      Convert(DWRITE_FONT_STYLE fontStyle);
            static DWRITE_FONT_STYLE              Convert(FontStyle fontStyle);
            static DWRITE_FONT_METRICS            Convert(FontMetrics^ fontMetrics);
            static FontMetrics^                   Convert(DWRITE_FONT_METRICS dwriteFontMetrics);            
            static DWRITE_MATRIX                  Convert(DWriteMatrix^ matrix);
            static DWriteMatrix^                  Convert(DWRITE_MATRIX dwriteMatrix);
            static System::Windows::Point         Convert(DWRITE_GLYPH_OFFSET dwriteGlyphOffset);
            static DWRITE_INFORMATIONAL_STRING_ID Convert(InformationalStringID informationStringID);
            static InformationalStringID          Convert(DWRITE_INFORMATIONAL_STRING_ID dwriteInformationStringID);
            static DWRITE_MEASURING_MODE          Convert(TextFormattingMode measuringMode);
            static TextFormattingMode             Convert(DWRITE_MEASURING_MODE dwriteMeasuringMode);
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__DWRITETYPECONVERTER_H
