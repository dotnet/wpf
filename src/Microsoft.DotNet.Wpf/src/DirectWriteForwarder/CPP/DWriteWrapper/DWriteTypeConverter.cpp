// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "DWriteTypeConverter.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    DWRITE_FACTORY_TYPE DWriteTypeConverter::Convert(FactoryType factoryType)
    {
        switch(factoryType)
        {
            case FactoryType::Shared   : return DWRITE_FACTORY_TYPE_SHARED;
            case FactoryType::Isolated : return DWRITE_FACTORY_TYPE_ISOLATED;
            default                    : throw gcnew System::InvalidOperationException();
        }
    }

    FontWeight DWriteTypeConverter::Convert(DWRITE_FONT_WEIGHT fontWeight)
    {
        // The commented cases are here only for completeness so that the code captures all the possible enum values.
        // However, these enum values are commented out since there are several enums having the same value.
        // For example, both Normal and Regular have a value of 400.
        switch(fontWeight)
        {
            case DWRITE_FONT_WEIGHT_THIN        : return FontWeight::Thin;
            case DWRITE_FONT_WEIGHT_EXTRA_LIGHT : return FontWeight::ExtraLight;
          //case DWRITE_FONT_WEIGHT_ULTRA_LIGHT : return FontWeight::UltraLight;
            case DWRITE_FONT_WEIGHT_LIGHT       : return FontWeight::Light;
            case DWRITE_FONT_WEIGHT_NORMAL      : return FontWeight::Normal;
          //case DWRITE_FONT_WEIGHT_REGULAR     : return FontWeight::Regular;
            case DWRITE_FONT_WEIGHT_MEDIUM      : return FontWeight::Medium;
          //case DWRITE_FONT_WEIGHT_DEMI_BOLD   : return FontWeight::DemiBold;
            case DWRITE_FONT_WEIGHT_SEMI_BOLD   : return FontWeight::SemiBOLD;
            case DWRITE_FONT_WEIGHT_BOLD        : return FontWeight::Bold;
            case DWRITE_FONT_WEIGHT_EXTRA_BOLD  : return FontWeight::ExtraBold;
          //case DWRITE_FONT_WEIGHT_ULTRA_BOLD  : return FontWeight::UltraBold;
            case DWRITE_FONT_WEIGHT_BLACK       : return FontWeight::Black;
          //case DWRITE_FONT_WEIGHT_HEAVY       : return FontWeight::Heavy;
            case DWRITE_FONT_WEIGHT_EXTRA_BLACK : return FontWeight::ExtraBlack;
          //case DWRITE_FONT_WEIGHT_ULTRA_BLACK : return FontWeight::UltraBlack;

			case DWRITE_FONT_WEIGHT_SEMI_LIGHT :	// let it fall through - because I don't know what else to do with it
            default                             : int weight = (int)fontWeight;
                                                  if ( weight >= 1 && weight <= 999)
                                                  {
                                                    return (FontWeight) fontWeight;
                                                  }
                                                  else
                                                  {
                                                    throw gcnew System::InvalidOperationException();
                                                  }
        }
    }

    DWRITE_FONT_WEIGHT DWriteTypeConverter::Convert(FontWeight fontWeight)
    {
        // The commented cases are here only for completeness so that the code captures all the possible enum values.
        // However, these enum values are commented out since there are several enums having the same value.
        // For example, both Normal and Regular have a value of 400.
        switch(fontWeight)
        {
            case FontWeight::Thin       : return DWRITE_FONT_WEIGHT_THIN;
            case FontWeight::ExtraLight : return DWRITE_FONT_WEIGHT_EXTRA_LIGHT;
          //case FontWeight::UltraLight : return DWRITE_FONT_WEIGHT_ULTRA_LIGHT;
            case FontWeight::Light      : return DWRITE_FONT_WEIGHT_LIGHT;
            case FontWeight::Normal     : return DWRITE_FONT_WEIGHT_NORMAL;
          //case FontWeight::Regular    : return DWRITE_FONT_WEIGHT_REGULAR;
            case FontWeight::Medium     : return DWRITE_FONT_WEIGHT_MEDIUM;
          //case FontWeight::DemiBold   : return DWRITE_FONT_WEIGHT_DEMI_BOLD;
            case FontWeight::SemiBOLD   : return DWRITE_FONT_WEIGHT_SEMI_BOLD;
            case FontWeight::Bold       : return DWRITE_FONT_WEIGHT_BOLD;
            case FontWeight::ExtraBold  : return DWRITE_FONT_WEIGHT_EXTRA_BOLD;
          //case FontWeight::UltraBold  : return DWRITE_FONT_WEIGHT_ULTRA_BOLD;
            case FontWeight::Black      : return DWRITE_FONT_WEIGHT_BLACK;
          //case FontWeight::Heavy      : return DWRITE_FONT_WEIGHT_HEAVY;
            case FontWeight::ExtraBlack : return DWRITE_FONT_WEIGHT_EXTRA_BLACK;
          //case FontWeight::UltraBlack : return DWRITE_FONT_WEIGHT_ULTRA_BLACK;
            default         : int weight = (int)fontWeight;
                              if ( weight >= 1 && weight <= 999)
                              {
                                return (DWRITE_FONT_WEIGHT) fontWeight;
                              }
                              else
                              {
                                throw gcnew System::InvalidOperationException();
                              }
        }
    }

    FontSimulations DWriteTypeConverter::Convert(DWRITE_FONT_SIMULATIONS fontSimulations)
    {
        //Converting to char because the compiler was complaining about it.
        char fontSim = (char)fontSimulations;
        switch(fontSim)
        {
            case  DWRITE_FONT_SIMULATIONS_BOLD                                    : return FontSimulations::Bold;
            case  DWRITE_FONT_SIMULATIONS_OBLIQUE                                 : return FontSimulations::Oblique;
            case  DWRITE_FONT_SIMULATIONS_NONE                                    : return FontSimulations::None;
            // We did have (DWRITE_FONT_SIMULATIONS_BOLD | DWRITE_FONT_SIMULATIONS_OBLIQUE) as a switch case, but the compiler
            // started throwing C2051 on this when I ported from WPFText to WPFDWrite. Probably some compiler or build setting 
            // change caused this. Just moved it to the default case instead.
            default                                                               : if (fontSim == (DWRITE_FONT_SIMULATIONS_BOLD | DWRITE_FONT_SIMULATIONS_OBLIQUE)) return (FontSimulations::Bold | FontSimulations::Oblique);
                                                                                    else throw gcnew System::InvalidOperationException();
        }
    }

    unsigned char DWriteTypeConverter::Convert(FontSimulations fontSimulations)
    {
        //Converting to char because the compiler was complaining about it.
        char fontSim = (char)fontSimulations;
        switch(fontSim)
        {
            case  FontSimulations::Bold                             :    return DWRITE_FONT_SIMULATIONS_BOLD;
            case  FontSimulations::Oblique                          :    return DWRITE_FONT_SIMULATIONS_OBLIQUE;
            case (FontSimulations::Bold | FontSimulations::Oblique) :    return (unsigned char)(DWRITE_FONT_SIMULATIONS_BOLD | DWRITE_FONT_SIMULATIONS_OBLIQUE);
            case  FontSimulations::None                             :    return DWRITE_FONT_SIMULATIONS_NONE;
            default                                                 :    throw gcnew System::InvalidOperationException();
        }
    }

    DWRITE_FONT_FACE_TYPE DWriteTypeConverter::Convert(FontFaceType fontFaceType)
    {
        switch (fontFaceType)
        {
            case FontFaceType::Bitmap             : return DWRITE_FONT_FACE_TYPE_BITMAP;
            case FontFaceType::CFF                : return DWRITE_FONT_FACE_TYPE_CFF;
            case FontFaceType::TrueType           : return DWRITE_FONT_FACE_TYPE_TRUETYPE;
            case FontFaceType::TrueTypeCollection : return DWRITE_FONT_FACE_TYPE_TRUETYPE_COLLECTION;
            case FontFaceType::Type1              : return DWRITE_FONT_FACE_TYPE_TYPE1;
            case FontFaceType::Vector             : return DWRITE_FONT_FACE_TYPE_VECTOR;
            case FontFaceType::Unknown            : return DWRITE_FONT_FACE_TYPE_UNKNOWN;
            
            // The following were added to DWrite.h in the Win8 SDK, but are not currently supported by WPF.
            // case FontFaceType::RawCFF             : return DWRITE_FONT_FACE_TYPE_RAW_CFF;
                        
            default                               : throw gcnew System::InvalidOperationException();
        }
    }

    FontFaceType DWriteTypeConverter::Convert(DWRITE_FONT_FACE_TYPE fontFaceType)
    {
        switch (fontFaceType)
        {
            case DWRITE_FONT_FACE_TYPE_BITMAP              : return FontFaceType::Bitmap;
            case DWRITE_FONT_FACE_TYPE_CFF                 : return FontFaceType::CFF;
            case DWRITE_FONT_FACE_TYPE_TRUETYPE            : return FontFaceType::TrueType;
            case DWRITE_FONT_FACE_TYPE_TRUETYPE_COLLECTION : return FontFaceType::TrueTypeCollection;
            case DWRITE_FONT_FACE_TYPE_TYPE1               : return FontFaceType::Type1;
            case DWRITE_FONT_FACE_TYPE_VECTOR              : return FontFaceType::Vector;
            case DWRITE_FONT_FACE_TYPE_UNKNOWN             : return FontFaceType::Unknown;

            // The following were added to DWrite.h in the Win8 SDK, but are not currently supported by WPF.
            case DWRITE_FONT_FACE_TYPE_RAW_CFF             : // return FontFaceType::RawCFF;
            
            default                                        : throw gcnew System::InvalidOperationException();
        }
    }

    FontFileType DWriteTypeConverter::Convert(DWRITE_FONT_FILE_TYPE dwriteFontFileType)
    {
        switch(dwriteFontFileType)
        {
            case DWRITE_FONT_FILE_TYPE_UNKNOWN             : return FontFileType::Unknown;
            case DWRITE_FONT_FILE_TYPE_CFF                 : return FontFileType::CFF;
            case DWRITE_FONT_FILE_TYPE_TRUETYPE            : return FontFileType::TrueType;
            case DWRITE_FONT_FILE_TYPE_TRUETYPE_COLLECTION : return FontFileType::TrueTypeCollection;
            case DWRITE_FONT_FILE_TYPE_TYPE1_PFM           : return FontFileType::Type1PFM;
            case DWRITE_FONT_FILE_TYPE_TYPE1_PFB           : return FontFileType::Type1PFB;
            case DWRITE_FONT_FILE_TYPE_VECTOR              : return FontFileType::Vector;
            case DWRITE_FONT_FILE_TYPE_BITMAP              : return FontFileType::Bitmap;
            default                                        : throw gcnew System::InvalidOperationException();
        }                       
    }

    FontStretch DWriteTypeConverter::Convert(DWRITE_FONT_STRETCH fontStretch)
    {
        // The commented cases are here only for completeness so that the code captures all the possible enum values.
        // However, these enum values are commented out since there are several enums having the same value.
        // For example, both Normal and Medium have a value of 5.
        switch(fontStretch)
        {
            case DWRITE_FONT_STRETCH_UNDEFINED       : return FontStretch::Undefined;
            case DWRITE_FONT_STRETCH_ULTRA_CONDENSED : return FontStretch::UltraCondensed;
            case DWRITE_FONT_STRETCH_EXTRA_CONDENSED : return FontStretch::ExtraCondensed;
            case DWRITE_FONT_STRETCH_CONDENSED       : return FontStretch::Condensed;
            case DWRITE_FONT_STRETCH_SEMI_CONDENSED  : return FontStretch::SemiCondensed;
            case DWRITE_FONT_STRETCH_NORMAL          : return FontStretch::Normal;
          //case DWRITE_FONT_STRETCH_MEDIUM          : return FontStretch::Medium;
            case DWRITE_FONT_STRETCH_SEMI_EXPANDED   : return FontStretch::SemiExpanded;
            case DWRITE_FONT_STRETCH_EXPANDED        : return FontStretch::Expanded;
            case DWRITE_FONT_STRETCH_EXTRA_EXPANDED  : return FontStretch::ExtraExpanded;
            case DWRITE_FONT_STRETCH_ULTRA_EXPANDED  : return FontStretch::UltraExpanded;
            default                                  : throw gcnew System::InvalidOperationException();
        } 
    }

    DWRITE_FONT_STRETCH DWriteTypeConverter::Convert(FontStretch fontStretch)
    {
        // The commented cases are here only for completeness so that the code captures all the possible enum values.
        // However, these enum values are commented out since there are several enums having the same value.
        // For example, both Normal and Medium have a value of 5.
        switch(fontStretch)
        {
            case FontStretch::Undefined      : return DWRITE_FONT_STRETCH_UNDEFINED;
            case FontStretch::UltraCondensed : return DWRITE_FONT_STRETCH_ULTRA_CONDENSED;
            case FontStretch::ExtraCondensed : return DWRITE_FONT_STRETCH_EXTRA_CONDENSED;
            case FontStretch::Condensed      : return DWRITE_FONT_STRETCH_CONDENSED;
            case FontStretch::SemiCondensed  : return DWRITE_FONT_STRETCH_SEMI_CONDENSED;
            case FontStretch::Normal         : return DWRITE_FONT_STRETCH_NORMAL;
          //case FontStretch::Medium         : return DWRITE_FONT_STRETCH_MEDIUM;
            case FontStretch::SemiExpanded   : return DWRITE_FONT_STRETCH_SEMI_EXPANDED;
            case FontStretch::Expanded       : return DWRITE_FONT_STRETCH_EXPANDED;
            case FontStretch::ExtraExpanded  : return DWRITE_FONT_STRETCH_EXTRA_EXPANDED;
            case FontStretch::UltraExpanded  : return DWRITE_FONT_STRETCH_ULTRA_EXPANDED;
            default                          : throw gcnew System::InvalidOperationException();
        } 
    }

    DWRITE_FONT_STYLE DWriteTypeConverter::Convert(FontStyle fontStyle)
    {
        switch(fontStyle)
        {
            case FontStyle::Normal       : return DWRITE_FONT_STYLE_NORMAL;
            case FontStyle::Italic       : return DWRITE_FONT_STYLE_ITALIC;
            case FontStyle::Oblique      : return DWRITE_FONT_STYLE_OBLIQUE;
            default                      : throw gcnew System::InvalidOperationException();
        } 
    }

    FontStyle DWriteTypeConverter::Convert(DWRITE_FONT_STYLE fontStyle)
    {
        switch(fontStyle)
        {
            case DWRITE_FONT_STYLE_NORMAL   : return FontStyle::Normal;
            case DWRITE_FONT_STYLE_ITALIC   : return FontStyle::Italic;
            case DWRITE_FONT_STYLE_OBLIQUE  : return FontStyle::Oblique;
            default                         : throw gcnew System::InvalidOperationException();
        } 
    }

    FontMetrics^ DWriteTypeConverter::Convert(DWRITE_FONT_METRICS dwriteFontMetrics)
    {
        FontMetrics^ fontMetrics = gcnew FontMetrics();

        fontMetrics->Ascent                 = dwriteFontMetrics.ascent;
        fontMetrics->CapHeight              = dwriteFontMetrics.capHeight;
        fontMetrics->Descent                = dwriteFontMetrics.descent;
        fontMetrics->DesignUnitsPerEm       = dwriteFontMetrics.designUnitsPerEm;
        fontMetrics->LineGap                = dwriteFontMetrics.lineGap;
        fontMetrics->StrikethroughPosition  = dwriteFontMetrics.strikethroughPosition;
        fontMetrics->StrikethroughThickness = dwriteFontMetrics.strikethroughThickness;
        fontMetrics->UnderlinePosition      = dwriteFontMetrics.underlinePosition;
        fontMetrics->UnderlineThickness     = dwriteFontMetrics.underlineThickness;
        fontMetrics->XHeight                = dwriteFontMetrics.xHeight;

        return fontMetrics;
    }

    DWRITE_FONT_METRICS DWriteTypeConverter::Convert(FontMetrics^ fontMetrics)
    {
        DWRITE_FONT_METRICS dwriteFontMetrics;

        dwriteFontMetrics.ascent                 = fontMetrics->Ascent;
        dwriteFontMetrics.capHeight              = fontMetrics->CapHeight;
        dwriteFontMetrics.descent                = fontMetrics->Descent;
        dwriteFontMetrics.designUnitsPerEm       = fontMetrics->DesignUnitsPerEm;
        dwriteFontMetrics.lineGap                = fontMetrics->LineGap;
        dwriteFontMetrics.strikethroughPosition  = fontMetrics->StrikethroughPosition; 
        dwriteFontMetrics.strikethroughThickness = fontMetrics->StrikethroughThickness;
        dwriteFontMetrics.underlinePosition      = fontMetrics->UnderlinePosition;
        dwriteFontMetrics.underlineThickness     = fontMetrics->UnderlineThickness;
        dwriteFontMetrics.xHeight                = fontMetrics->XHeight;

        return dwriteFontMetrics;
    }

    DWRITE_MATRIX DWriteTypeConverter::Convert(DWriteMatrix^ matrix)
    {
        DWRITE_MATRIX dwriteMatrix;

        dwriteMatrix.dx  = matrix->Dx;
        dwriteMatrix.dy  = matrix->Dy;
        dwriteMatrix.m11 = matrix->M11;
        dwriteMatrix.m12 = matrix->M12;
        dwriteMatrix.m21 = matrix->M21;
        dwriteMatrix.m22 = matrix->M22;

        return dwriteMatrix;
    }

    DWriteMatrix^ DWriteTypeConverter::Convert(DWRITE_MATRIX  dwriteMatrix)
    {
        DWriteMatrix^ matrix = gcnew DWriteMatrix;

        matrix->Dx  = dwriteMatrix.dx;
        matrix->Dy  = dwriteMatrix.dy;
        matrix->M11 = dwriteMatrix.m11;
        matrix->M12 = dwriteMatrix.m12;
        matrix->M21 = dwriteMatrix.m21;
        matrix->M22 = dwriteMatrix.m22;

        return matrix;
    }

    System::Windows::Point DWriteTypeConverter::Convert(DWRITE_GLYPH_OFFSET dwriteGlyphOffset)
    {
        System::Windows::Point glyphOffset;

        glyphOffset.X  = dwriteGlyphOffset.advanceOffset;
        glyphOffset.Y  = dwriteGlyphOffset.ascenderOffset;

        return glyphOffset;
    }

    DWRITE_INFORMATIONAL_STRING_ID DWriteTypeConverter::Convert(InformationalStringID informationStringID)
    {
        switch(informationStringID)
        {            
            case InformationalStringID::None                    : return DWRITE_INFORMATIONAL_STRING_NONE;
            case InformationalStringID::CopyrightNotice         : return DWRITE_INFORMATIONAL_STRING_COPYRIGHT_NOTICE;
            case InformationalStringID::VersionStrings          : return DWRITE_INFORMATIONAL_STRING_VERSION_STRINGS;
            case InformationalStringID::Trademark               : return DWRITE_INFORMATIONAL_STRING_TRADEMARK;
            case InformationalStringID::Manufacturer            : return DWRITE_INFORMATIONAL_STRING_MANUFACTURER;
            case InformationalStringID::Designer                : return DWRITE_INFORMATIONAL_STRING_DESIGNER;
            case InformationalStringID::DesignerURL             : return DWRITE_INFORMATIONAL_STRING_DESIGNER_URL;
            case InformationalStringID::Description             : return DWRITE_INFORMATIONAL_STRING_DESCRIPTION;
            case InformationalStringID::FontVendorURL           : return DWRITE_INFORMATIONAL_STRING_FONT_VENDOR_URL;
            case InformationalStringID::LicenseDescription      : return DWRITE_INFORMATIONAL_STRING_LICENSE_DESCRIPTION;
            case InformationalStringID::LicenseInfoURL          : return DWRITE_INFORMATIONAL_STRING_LICENSE_INFO_URL;
            case InformationalStringID::WIN32FamilyNames        : return DWRITE_INFORMATIONAL_STRING_WIN32_FAMILY_NAMES;
            case InformationalStringID::Win32SubFamilyNames     : return DWRITE_INFORMATIONAL_STRING_WIN32_SUBFAMILY_NAMES;
            case InformationalStringID::PreferredFamilyNames    : return DWRITE_INFORMATIONAL_STRING_PREFERRED_FAMILY_NAMES;
            case InformationalStringID::PreferredSubFamilyNames : return DWRITE_INFORMATIONAL_STRING_PREFERRED_SUBFAMILY_NAMES;
            case InformationalStringID::SampleText              : return DWRITE_INFORMATIONAL_STRING_SAMPLE_TEXT;

            // The following were added to DWrite.h in the Win8 SDK, but are not currently supported by WPF.
            // case InformationalStringID::PostscriptCidName       : return DWRITE_INFORMATIONAL_STRING_POSTSCRIPT_CID_NAME;
            // case InformationalStringID::PostscriptName          : return DWRITE_INFORMATIONAL_STRING_POSTSCRIPT_NAME;
            // case InformationalStringID::FullName                : return DWRITE_INFORMATIONAL_STRING_FULL_NAME;
            
            default                                             : throw gcnew System::InvalidOperationException();
        }
    }

    InformationalStringID DWriteTypeConverter::Convert(DWRITE_INFORMATIONAL_STRING_ID dwriteInformationStringID)
    {
        switch(dwriteInformationStringID)
        {            
            case DWRITE_INFORMATIONAL_STRING_NONE                      : return InformationalStringID::None;
            case DWRITE_INFORMATIONAL_STRING_COPYRIGHT_NOTICE          : return InformationalStringID::CopyrightNotice;
            case DWRITE_INFORMATIONAL_STRING_VERSION_STRINGS           : return InformationalStringID::VersionStrings;
            case DWRITE_INFORMATIONAL_STRING_TRADEMARK                 : return InformationalStringID::Trademark;
            case DWRITE_INFORMATIONAL_STRING_MANUFACTURER              : return InformationalStringID::Manufacturer;
            case DWRITE_INFORMATIONAL_STRING_DESIGNER                  : return InformationalStringID::Designer;
            case DWRITE_INFORMATIONAL_STRING_DESIGNER_URL              : return InformationalStringID::DesignerURL;
            case DWRITE_INFORMATIONAL_STRING_DESCRIPTION               : return InformationalStringID::Description;
            case DWRITE_INFORMATIONAL_STRING_FONT_VENDOR_URL           : return InformationalStringID::FontVendorURL;
            case DWRITE_INFORMATIONAL_STRING_LICENSE_DESCRIPTION       : return InformationalStringID::LicenseDescription;
            case DWRITE_INFORMATIONAL_STRING_LICENSE_INFO_URL          : return InformationalStringID::LicenseInfoURL;
            case DWRITE_INFORMATIONAL_STRING_WIN32_FAMILY_NAMES        : return InformationalStringID::WIN32FamilyNames;
            case DWRITE_INFORMATIONAL_STRING_WIN32_SUBFAMILY_NAMES     : return InformationalStringID::Win32SubFamilyNames;
            case DWRITE_INFORMATIONAL_STRING_PREFERRED_FAMILY_NAMES    : return InformationalStringID::PreferredFamilyNames;
            case DWRITE_INFORMATIONAL_STRING_PREFERRED_SUBFAMILY_NAMES : return InformationalStringID::PreferredSubFamilyNames;
            case DWRITE_INFORMATIONAL_STRING_SAMPLE_TEXT               : return InformationalStringID::SampleText;

            // The following were added to DWrite.h in the Win8 SDK, but are not currently supported by WPF.
            case DWRITE_INFORMATIONAL_STRING_POSTSCRIPT_CID_NAME       : // return InformationalStringID::PostscriptCidName;
            case DWRITE_INFORMATIONAL_STRING_POSTSCRIPT_NAME           : // return InformationalStringID::PostscriptName;
            case DWRITE_INFORMATIONAL_STRING_FULL_NAME                 : // return InformationalStringID::FullName;
            
            default                                                    : throw gcnew System::InvalidOperationException();
        }
    }

    DWRITE_MEASURING_MODE DWriteTypeConverter::Convert(TextFormattingMode measuringMode)
    {
        switch(measuringMode)
        {
            case TextFormattingMode::Ideal   : return DWRITE_MEASURING_MODE_NATURAL;
            case TextFormattingMode::Display : return DWRITE_MEASURING_MODE_GDI_CLASSIC;    
            // We do not support Natural Metrics mode in WPF
            default                                      : throw gcnew System::InvalidOperationException();
        }
    }

    TextFormattingMode DWriteTypeConverter::Convert(DWRITE_MEASURING_MODE dwriteMeasuringMode)
    {
        switch(dwriteMeasuringMode)
        {
            case DWRITE_MEASURING_MODE_NATURAL             : return TextFormattingMode::Ideal;
            case DWRITE_MEASURING_MODE_GDI_CLASSIC         : return TextFormattingMode::Display;
            // We do not support Natural Metrics mode in WPF
            // However, the build system complained about not having an explicit case 
            // for DWRITE_TEXT_MEASURING_METHOD_USE_DISPLAY_NATURAL_METRICS
            case DWRITE_MEASURING_MODE_GDI_NATURAL         : throw gcnew System::InvalidOperationException();
            default                                        : throw gcnew System::InvalidOperationException();
        }
    }

}}}}//MS::Internal::Text::TextInterface
