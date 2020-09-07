// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "Font.h"
#include "FontFamily.h"
#include "DWriteTypeConverter.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    enum
    {
        Flags_VersionInitialized            = 0x0001,
        Flags_IsSymbolFontInitialized       = 0x0002,
        Flags_IsSymbolFontValue             = 0x0004,
    };

    Font::Font(
      IDWriteFont* font
      )
    {
        _font = gcnew NativeIUnknownWrapper<IDWriteFont>(font);
        _version = System::Double::MinValue;
        _flags = 0;
    }

    FontFace^ Font::AddFontFaceToCache()
    {
        FontFace^ fontFace = CreateFontFace();
        FontFace^ bumpedFontFace = nullptr;

        // NB: if the cache is busy, we simply return the new FontFace
        // without bothering to cache it.
        if (System::Threading::Interlocked::Increment(_mutex) == 1)
        {
            if (nullptr == _fontFaceCache)
            {
                _fontFaceCache = gcnew array<FontFaceCacheEntry>(_fontFaceCacheSize);
            }
            
            // Default to a slot that is not the MRU.
            _fontFaceCacheMRU = (_fontFaceCacheMRU + 1) % _fontFaceCacheSize;

            // Look for an empty slot.
            for (int i=0; i < _fontFaceCacheSize; i++)
            {
                if (_fontFaceCache[i].font == nullptr)
                {
                    _fontFaceCacheMRU = i;
                    break;
                }
            }

            // Keep a reference to any discarded entry, clean it up after releasing
            // the mutex.
            bumpedFontFace = _fontFaceCache[_fontFaceCacheMRU].fontFace;

            // Record the new entry.
            _fontFaceCache[_fontFaceCacheMRU].font = this;
            _fontFaceCache[_fontFaceCacheMRU].fontFace = fontFace;
            fontFace->AddRef();
        }
        System::Threading::Interlocked::Decrement(_mutex);

        // If the cache was full and we replaced an unreferenced entry, release it now.
        if (bumpedFontFace != nullptr)
        {
            bumpedFontFace->Release();
        }

        return fontFace;
    }

    FontFace^ Font::LookupFontFaceSlow()
    {
        FontFace^ fontFace = nullptr;

        for (int i=0; i < _fontFaceCacheSize; i++)
        {
            if (_fontFaceCache[i].font == this)
            {
                fontFace = _fontFaceCache[i].fontFace;
                fontFace->AddRef();
                _fontFaceCacheMRU = i;
                break;
            }
        }

        return fontFace;
    }

    void Font::ResetFontFaceCache()
    {
        array<FontFaceCacheEntry>^ fontFaceCache = nullptr;

        // NB: If the cache is busy, we do nothing.
        if (System::Threading::Interlocked::Increment(_mutex) == 1)
        {
            fontFaceCache = _fontFaceCache;
            _fontFaceCache = nullptr;
        }
        System::Threading::Interlocked::Decrement(_mutex);

        if (fontFaceCache != nullptr)
        {
            for (int i=0; i < _fontFaceCacheSize; i++)
            {
                if (fontFaceCache[i].fontFace != nullptr)
                {
                    fontFaceCache[i].fontFace->Release();
                }
            }
        }
    }

    FontFace^ Font::GetFontFace()
    {
        FontFace^ fontFace = nullptr;

        if (System::Threading::Interlocked::Increment(_mutex) == 1)
        {
            if (_fontFaceCache != nullptr)
            {
                FontFaceCacheEntry entry;
                // Try the fast path first -- is caller accessing exactly the mru entry?
                if ((entry = _fontFaceCache[_fontFaceCacheMRU]).font == this)
                {
                    entry.fontFace->AddRef();
                    fontFace = entry.fontFace;
                }
                else
                {
                    // No luck, do a search through the cache.
                    fontFace = LookupFontFaceSlow();
                }
            }
        }
        System::Threading::Interlocked::Decrement(_mutex);

        // If the cache was busy or did not contain this Font, create a new FontFace.
        if (nullptr == fontFace)
        {
            fontFace = AddFontFaceToCache();
        }

        return fontFace;
    }

    System::IntPtr Font::DWriteFontAddRef::get()
    {
        _font->Value->AddRef();
        return (System::IntPtr)_font->Value;
    }

    __declspec(noinline) FontFamily^ Font::Family::get()
    {
        IDWriteFontFamily* dwriteFontFamily;
        HRESULT hr = _font->Value->GetFontFamily(
                                         &dwriteFontFamily
                                         );
        System::GC::KeepAlive(_font);
        ConvertHresultToException(hr, "FontFamily^ Font::Family::get");
        return gcnew FontFamily(dwriteFontFamily);
    }

    __declspec(noinline) FontWeight Font::Weight::get()
    {
        DWRITE_FONT_WEIGHT dwriteFontWeight = _font->Value->GetWeight();
        System::GC::KeepAlive(_font);
        return DWriteTypeConverter::Convert(dwriteFontWeight);
    }

    __declspec(noinline) FontStretch Font::Stretch::get()
    {
        DWRITE_FONT_STRETCH dwriteFontStretch = _font->Value->GetStretch();
        System::GC::KeepAlive(_font);
        return DWriteTypeConverter::Convert(dwriteFontStretch);
    }

    __declspec(noinline) FontStyle Font::Style::get()
    {
        DWRITE_FONT_STYLE dwriteFontStyle = _font->Value->GetStyle();
        System::GC::KeepAlive(_font);
        return DWriteTypeConverter::Convert(dwriteFontStyle);
    }

    __declspec(noinline) bool Font::IsSymbolFont::get()
    {
        if ((_flags & Flags_IsSymbolFontInitialized) != Flags_IsSymbolFontInitialized)
        {
            BOOL isSymbolFont = _font->Value->IsSymbolFont();
            System::GC::KeepAlive(_font);
            _flags |= Flags_IsSymbolFontInitialized;
            if (isSymbolFont)
            {
                _flags |= Flags_IsSymbolFontValue;
            }
        }
        return ((_flags & Flags_IsSymbolFontValue) == Flags_IsSymbolFontValue);
    }

    __declspec(noinline) LocalizedStrings^ Font::FaceNames::get()
    {
        IDWriteLocalizedStrings* dwriteLocalizedStrings;
        HRESULT hr = _font->Value->GetFaceNames(
                                        &dwriteLocalizedStrings
                                        );
        System::GC::KeepAlive(_font);
        ConvertHresultToException(hr, "LocalizedStrings^ Font::FaceNames::get");        
        return gcnew LocalizedStrings(dwriteLocalizedStrings);
    }

    __declspec(noinline) bool Font::GetInformationalStrings(
                                                                              InformationalStringID informationalStringID,
                                      [System::Runtime::InteropServices::Out] LocalizedStrings^%    informationalStrings
                                      )
    {
        IDWriteLocalizedStrings* dwriteInformationalStrings;
        BOOL exists = FALSE;
        HRESULT hr  = _font->Value->GetInformationalStrings(
                                                   DWriteTypeConverter::Convert(informationalStringID),
                                                   &dwriteInformationalStrings,
                                                   &exists
                                                   );
        System::GC::KeepAlive(_font);
        ConvertHresultToException(hr, "bool Font::GetInformationalStrings");
        informationalStrings = gcnew LocalizedStrings(dwriteInformationalStrings);
        return (!!exists);
    }

    __declspec(noinline) FontSimulations Font::SimulationFlags::get()
    {
        DWRITE_FONT_SIMULATIONS dwriteFontSimulations = _font->Value->GetSimulations();
        System::GC::KeepAlive(_font);
        return DWriteTypeConverter::Convert(dwriteFontSimulations);
    }

    __declspec(noinline) FontMetrics^ Font::Metrics::get()
    {
        if (_fontMetrics == nullptr)
        {
            DWRITE_FONT_METRICS fontMetrics;
            _font->Value->GetMetrics(
                             &fontMetrics
                             );
            System::GC::KeepAlive(_font);
            _fontMetrics = DWriteTypeConverter::Convert(fontMetrics);
        }
        return _fontMetrics;
    }

    __declspec(noinline) bool Font::HasCharacter(
                           UINT32 unicodeValue
                           )
    {
        BOOL exists = FALSE;
        HRESULT hr  = _font->Value->HasCharacter(
                                        unicodeValue,
                                        &exists
                                        );
        System::GC::KeepAlive(_font);
        ConvertHresultToException(hr, "bool Font::HasCharacter");
        return (!!exists);
    }

    __declspec(noinline) FontFace^ Font::CreateFontFace()
    {
        IDWriteFontFace* dwriteFontFace;
        HRESULT hr = _font->Value->CreateFontFace(
                                          &dwriteFontFace
                                          );
        System::GC::KeepAlive(_font);
        ConvertHresultToException(hr, "FontFace^ Font::CreateFontFace");
        return gcnew FontFace(dwriteFontFace);
    }

    double Font::Version::get()
    {
        if ((_flags & Flags_VersionInitialized) != Flags_VersionInitialized)
        {
            LocalizedStrings^ versionNumbers;
            double version = 0.0;
            if (GetInformationalStrings(InformationalStringID::VersionStrings, versionNumbers))
            {
                String^ versionString = versionNumbers->GetString(0);

                // The following logic assumes that the version string is always formatted in this way: "Version X.XX"
                if(versionString->Length > 1)
                {
                    versionString = versionString->Substring(versionString->LastIndexOf(' ') + 1);           
                    if (!System::Double::TryParse(versionString, System::Globalization::NumberStyles::Float, System::Globalization::CultureInfo::InvariantCulture, version))
                    {
                        version = 0.0;
                    }
                }
            }
            _flags |= Flags_VersionInitialized;
            _version = version;
        }
        return _version;
    }

    __declspec(noinline) FontMetrics^ Font::DisplayMetrics(FLOAT emSize, FLOAT pixelsPerDip)
    {
        DWRITE_FONT_METRICS fontMetrics;
        IDWriteFontFace* fontFace = NULL;
        HRESULT hr = _font->Value->CreateFontFace(&fontFace);
        ConvertHresultToException(hr, "FontMetrics^ Font::DisplayMetrics");
        DWRITE_MATRIX transform = Factory::GetIdentityTransform();
        hr = fontFace->GetGdiCompatibleMetrics(
                                    emSize,
                                    pixelsPerDip,
                                    &transform,
                                    &fontMetrics
                                    ); 
        if (fontFace != NULL)
        {
            fontFace->Release();
        }

        ConvertHresultToException(hr, "FontMetrics^ Font::DisplayMetrics");
        System::GC::KeepAlive(_font);
        return DWriteTypeConverter::Convert(fontMetrics);
    }

}}}}//MS::Internal::Text::TextInterface
