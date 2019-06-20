// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontFamily.h"
#include "DWriteTypeConverter.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    FontFamily::FontFamily(IDWriteFontFamily* fontFamily) : FontList(fontFamily)
    {
        _regularFont = nullptr;
    }

    __declspec(noinline) LocalizedStrings^ FontFamily::FamilyNames::get()
    {
        IDWriteLocalizedStrings* dwriteLocalizedStrings;
        HRESULT hr = ((IDWriteFontFamily*)FontListObject->Value)->GetFamilyNames(
                                                                                 &dwriteLocalizedStrings
                                                                                 );
        System::GC::KeepAlive(FontListObject);
        ConvertHresultToException(hr, "LocalizedStrings^ FontFamily::FamilyNames::get");      
        return gcnew LocalizedStrings(dwriteLocalizedStrings);
    }

    bool FontFamily::IsPhysical::get()
    {
        return true;
    }

    bool FontFamily::IsComposite::get()
    {
        return false;
    }

    System::String^ FontFamily::OrdinalName::get()
    {        
        if (FamilyNames->StringsCount > 0)
        {
            return FamilyNames->GetString(0);
        }
        return System::String::Empty;

    }

    FontMetrics^ FontFamily::Metrics::get()
    {
        if (_regularFont == nullptr)
        {
            _regularFont = GetFirstMatchingFont(FontWeight::Normal, FontStretch::Normal, FontStyle::Normal);
        }
        return _regularFont->Metrics;
    }

    FontMetrics^ FontFamily::DisplayMetrics(float emSize, float pixelsPerDip)
    {
        Font^ regularFont = GetFirstMatchingFont(FontWeight::Normal, FontStretch::Normal, FontStyle::Normal);     
        return regularFont->DisplayMetrics(emSize, pixelsPerDip);
    }

    __declspec(noinline) Font^ FontFamily::GetFirstMatchingFont(
                                                               FontWeight  weight,
                                                               FontStretch stretch,
                                                               FontStyle   style
                                                               )
    {
        IDWriteFont* dwriteFont;
        
        HRESULT hr = ((IDWriteFontFamily*)FontListObject->Value)->GetFirstMatchingFont(
                                                                                      DWriteTypeConverter::Convert(weight),
                                                                                      DWriteTypeConverter::Convert(stretch),
                                                                                      DWriteTypeConverter::Convert(style),
                                                                                      &dwriteFont
                                                                                      );
        System::GC::KeepAlive(FontListObject);
        ConvertHresultToException(hr, "Font^ FontFamily::GetFirstMatchingFont");
        return gcnew Font(dwriteFont);
    }

    __declspec(noinline) FontList^ FontFamily::GetMatchingFonts(
                                          FontWeight  weight,
                                          FontStretch stretch,
                                          FontStyle   style
                                          )
    {
        IDWriteFontList* dwriteFontList;      
        HRESULT hr = ((IDWriteFontFamily*)FontListObject->Value)->GetMatchingFonts(
                                                                                   DWriteTypeConverter::Convert(weight),
                                                                                   DWriteTypeConverter::Convert(stretch),
                                                                                   DWriteTypeConverter::Convert(style),
                                                                                   &dwriteFontList
                                                                                   );
        System::GC::KeepAlive(FontListObject);
        ConvertHresultToException(hr, "FontList^ FontFamily::GetMatchingFonts");
        return gcnew FontList(dwriteFontList);
    }
}}}}//MS::Internal::Text::TextInterface
