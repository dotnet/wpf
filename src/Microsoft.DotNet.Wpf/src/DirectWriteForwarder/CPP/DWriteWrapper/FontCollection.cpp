// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontCollection.h"
#include <wpfvcclr.h>

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <SecurityNote>
    /// Critical - Receives a native pointer and stores it internally.
    ///            This whole object is wrapped around the passed in pointer
    ///            So this ctor assumes safety of the passed in pointer.
    /// </SecurityNote>
    FontCollection::FontCollection(IDWriteFontCollection* fontCollection)
    {
        _fontCollection = gcnew NativeIUnknownWrapper<IDWriteFontCollection>(fontCollection);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontCollection.
    ///          - Calls security critical Util::GetPtrToStringChars.
    /// Safe     - Does not expose any security critical info.
    /// </SecurityNote>
    __declspec(noinline) bool FontCollection::FindFamilyName(
                                                                               System::String^ familyName,
                                       [System::Runtime::InteropServices::Out] unsigned int%   index
                                       )
    {
        pin_ptr<const WCHAR> familyNameWchar = Native::Util::GetPtrToStringChars(familyName);
        BOOL exists        = FALSE;
        UINT32 familyIndex = 0;
        HRESULT hr = _fontCollection->Value->FindFamilyName(
                                                    familyNameWchar,
                                                    &familyIndex,
                                                    &exists
                                                    );
        System::GC::KeepAlive(_fontCollection);
        //This is a MACRO
        ConvertHresultToException(hr, "bool FontCollection::FindFamilyName");
        index = familyIndex;
        return (!!exists);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontCollection and fontFace->DWriteFontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    __declspec(noinline) Font^ FontCollection::GetFontFromFontFace(FontFace^ fontFace)
    {
        IDWriteFontFace* dwriteFontFace = fontFace->DWriteFontFaceNoAddRef;
        IDWriteFont* dwriteFont = NULL;
        HRESULT hr = _fontCollection->Value->GetFontFromFontFace(
                                                         dwriteFontFace,
                                                         &dwriteFont
                                                         );
        System::GC::KeepAlive(fontFace);
        System::GC::KeepAlive(_fontCollection);
        if (DWRITE_E_NOFONT == hr)
        {
            return nullptr;
        }
        ConvertHresultToException(hr, "Font^ FontCollection::GetFontFromFontFace");
        return gcnew Font(dwriteFont);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontCollection and calls security critical ctor FontFamily.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    __declspec(noinline) FontFamily^ FontCollection::default::get(unsigned int familyIndex)
    {
        IDWriteFontFamily* dwriteFontFamily = NULL;

        HRESULT hr = _fontCollection->Value->GetFontFamily(
                                                   familyIndex,
                                                   &dwriteFontFamily
                                                   );
        System::GC::KeepAlive(_fontCollection);
        ConvertHresultToException(hr, "FontFamily^ FontCollection::default::get");

        return gcnew FontFamily(dwriteFontFamily);
    }

    FontFamily^ FontCollection::default::get(System::String^ familyName)
    {
        unsigned int index;
        bool exists = this->FindFamilyName(
                                          familyName,
                                          index
                                          );
        if (exists)
        {
            return this[index];
        }
        return nullptr;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontCollection.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    __declspec(noinline) unsigned int FontCollection::FamilyCount::get()
    {    
        UINT32 familyCount = _fontCollection->Value->GetFontFamilyCount();
        System::GC::KeepAlive(_fontCollection);
        return familyCount;     
    }  
     
}}}}//MS::Internal::Text::TextInterface
