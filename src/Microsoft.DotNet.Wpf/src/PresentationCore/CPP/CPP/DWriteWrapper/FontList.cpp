// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontList.h"
#include "FontCollection.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    FontList::FontList(IDWriteFontList* fontList)
    {
        _fontList = gcnew NativeIUnknownWrapper<IDWriteFontList>(fontList);
    }
    
    Font^ FontList::default::get(UINT32 index)
    {
        IDWriteFont* dwriteFont;
        HRESULT hr = _fontList->Value->GetFont(
                                       index,
                                       &dwriteFont
                                       );
        System::GC::KeepAlive(_fontList);
        ConvertHresultToException(hr, "Font^ FontList::default::get");
        return gcnew Font(dwriteFont);
    }

    UINT32 FontList::Count::get()
    {
        UINT32 count = _fontList->Value->GetFontCount();
        System::GC::KeepAlive(_fontList);
        return count;
    }
    
    FontCollection^ FontList::FontsCollection::get()
    {
        IDWriteFontCollection* dwriteFontCollection;
        HRESULT hr = _fontList->Value->GetFontCollection(
                                                 &dwriteFontCollection
                                                 );
        System::GC::KeepAlive(_fontList);
        ConvertHresultToException(hr, "FontCollection^ FontList::FontsCollection::get");
        return gcnew FontCollection(dwriteFontCollection);
    }

    NativeIUnknownWrapper<IDWriteFontList>^ FontList::FontListObject::get()
    {
        return _fontList;
    }
    
}}}}//MS::Internal::Text::TextInterface