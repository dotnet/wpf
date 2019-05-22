// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontFace.h"
#include "Factory.h"
#include "DWriteTypeConverter.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <SecurityNote>
    /// Critical - Receives a native pointer and stores it internally.
    ///            This whole object is wrapped around the passed in pointer
    ///            So this ctor assumes safety of the passed in pointer.
    /// </SecurityNote>
    //[SecurityCritical] – tagged in header file
    FontFace::FontFace(IDWriteFontFace* fontFace)
    {
        _fontFace = gcnew NativeIUnknownWrapper<IDWriteFontFace>(fontFace);
    }

    /// <SecurityNote>
    /// Critical - Manipulates security critical member _fontCollection.
    /// Safe     - Just releases the interface.
    /// </SecurityNote>
    //[SecuritySafeCritical]
    __declspec(noinline) FontFace::~FontFace()
    {
        if (_fontFace != nullptr)
        {
            delete _fontFace;
            _fontFace = nullptr;
        }
    }

    /// WARNING: AFTER GETTING THIS NATIVE POINTER YOU ARE RESPONSIBLE FOR MAKING SURE THAT THE WRAPPING MANAGED
    /// OBJECT IS KEPT ALIVE BY THE GC OR ELSE YOU ARE RISKING THE POINTER GETTING RELEASED BEFORE YOU'D 
    /// WANT TO.
    ///
    /// <SecurityNote>
    /// Critical - Exposes the native pointer that this object wraps.
    /// </SecurityNote>
    [SecurityCritical]
    IDWriteFontFace* FontFace::DWriteFontFaceNoAddRef::get()
    {
        return _fontFace->Value;
    }

    /// <SecurityNote>
    /// Critical - Exposes the native pointer that this object wraps.
    /// </SecurityNote>
    [SecurityCritical]
    System::IntPtr FontFace::DWriteFontFaceAddRef::get()
    {
        _fontFace->Value->AddRef();
        return (System::IntPtr)_fontFace->Value;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) FontFaceType FontFace::Type::get()
    {
        DWRITE_FONT_FACE_TYPE dwriteFontFaceType = _fontFace->Value->GetType();
        System::GC::KeepAlive(_fontFace);
        return DWriteTypeConverter::Convert(dwriteFontFaceType);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) FontFile^ FontFace::GetFileZero()
    {
        unsigned int numberOfFiles = 0;
        IDWriteFontFile*  pfirstDWriteFontFile = NULL;
        IDWriteFontFile** ppDWriteFontFiles = NULL;

        // This first call is to retrieve the numberOfFiles.
        HRESULT hr = _fontFace->Value->GetFiles(
                                        &numberOfFiles,
                                        NULL
                                        );
        ConvertHresultToException(hr, "array<FontFile^>^ FontFace::Files::get");

        if (numberOfFiles > 0)
        {
            ppDWriteFontFiles = new IDWriteFontFile*[numberOfFiles];

            try
            {
                hr = _fontFace->Value->GetFiles(
                                        &numberOfFiles,
                                        ppDWriteFontFiles
                                        );
                ConvertHresultToException(hr, "array<FontFile^>^ FontFace::Files::get");

                pfirstDWriteFontFile = ppDWriteFontFiles[0];

                for(unsigned int i = 1; i < numberOfFiles; ++i)
                {
                    ppDWriteFontFiles[i]->Release();
                    ppDWriteFontFiles[i] = NULL;
                }
            }
            finally
            {
                delete[] ppDWriteFontFiles;
            }

        }

        System::GC::KeepAlive(_fontFace);

        return (numberOfFiles > 0) ? gcnew FontFile(pfirstDWriteFontFile) : nullptr;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) unsigned int FontFace::Index::get()
    {
        unsigned int index = _fontFace->Value->GetIndex();
        System::GC::KeepAlive(_fontFace);
        return index;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) FontSimulations FontFace::SimulationFlags::get()
    {
        DWRITE_FONT_SIMULATIONS dwriteFontSimulations = _fontFace->Value->GetSimulations();
        System::GC::KeepAlive(_fontFace);
        return DWriteTypeConverter::Convert(dwriteFontSimulations);
    }  

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) bool FontFace::IsSymbolFont::get()
    {
        BOOL isSymbolFont = _fontFace->Value->IsSymbolFont();
        System::GC::KeepAlive(_fontFace);
        return (!!isSymbolFont);
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) FontMetrics^ FontFace::Metrics::get()
    {
        if (_fontMetrics == nullptr)
        {
            DWRITE_FONT_METRICS fontMetrics;
            _fontFace->Value->GetMetrics(
                                  &fontMetrics
                                  );
            System::GC::KeepAlive(_fontFace);
            _fontMetrics = DWriteTypeConverter::Convert(fontMetrics);
        }
        return _fontMetrics;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) UINT16 FontFace::GlyphCount::get()
    {
        UINT16 glyphCount = _fontFace->Value->GetGlyphCount();
        System::GC::KeepAlive(_fontFace);
        return glyphCount;
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    ///            Receives a native pointer as an argument.
    ///            Exposes a native pointer to the caller.
    /// </SecurityNote>
    [SecurityCritical]
    void FontFace::GetDesignGlyphMetrics(
        __in_ecount(glyphCount) const UINT16 *pGlyphIndices,
        UINT32 glyphCount,
        __out_ecount(glyphCount) GlyphMetrics *pGlyphMetrics
        )
    {      
        HRESULT hr = _fontFace->Value->GetDesignGlyphMetrics(
                                                      pGlyphIndices,
                                                      glyphCount,
                                                      reinterpret_cast<DWRITE_GLYPH_METRICS *>(pGlyphMetrics)
                                                      );
              

        System::GC::KeepAlive(_fontFace);
        ConvertHresultToException(hr, "array<GlyphMetrics^>^ FontFace::GetDesignGlyphMetrics");
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    ///            Receives a native pointer as an argument.
    ///            Exposes a native pointer to the caller.
    /// </SecurityNote>
    [SecurityCritical]
    void FontFace::GetDisplayGlyphMetrics(
        __in_ecount(glyphCount) const UINT16 *pGlyphIndices,
        UINT32 glyphCount,
        __out_ecount(glyphCount) GlyphMetrics *pGlyphMetrics,
        FLOAT emSize,
        bool useDisplayNatural,
        bool isSideways,
        float pixelsPerDip
        )
    {
        HRESULT hr = _fontFace->Value->GetGdiCompatibleGlyphMetrics(
            emSize,
            pixelsPerDip, //FLOAT pixelsPerDip,
            NULL,
            useDisplayNatural, //BOOL useGdiNatural,
            pGlyphIndices,//__in_ecount(glyphCount) UINT16 const* glyphIndices,
            glyphCount, //UINT32 glyphCount,
            reinterpret_cast<DWRITE_GLYPH_METRICS *>(pGlyphMetrics), //__out_ecount(glyphCount) DWRITE_GLYPH_METRICS* glyphMetrics
            isSideways //BOOL isSideways,
            );         
        System::GC::KeepAlive(_fontFace);
        ConvertHresultToException(hr, "array<GlyphMetrics^>^ FontFace::GetDesignGlyphMetrics");
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    ///            Receives a native pointer as an argument.
    ///            Exposes a native pointer to the caller.
    /// </SecurityNote>
    [SecurityCritical]
    void FontFace::GetArrayOfGlyphIndices(
        __in_ecount(glyphCount) const UINT32* pCodePoints,
        UINT32 glyphCount,
        __out_ecount(glyphCount) UINT16* pGlyphIndices
        )
    {
        const pin_ptr<const UINT32> pCodePointsPinned = pCodePoints;
        pin_ptr<UINT16> pGlyphIndicesPinned = pGlyphIndices;
        
        HRESULT hr = _fontFace->Value->GetGlyphIndices(pCodePointsPinned,
                                                glyphCount,
                                                pGlyphIndicesPinned
                                                );
        
        System::GC::KeepAlive(_fontFace);
        ConvertHresultToException(hr, "array<UINT16>^ FontFace::GetArrayOfGlyphIndices");
    }

    /// <SecurityNote>
    /// Critical - Exposes the data from a font.
    /// </SecurityNote>
    [SecurityCritical]
    __declspec(noinline) bool FontFace::TryGetFontTable(
                                                                          OpenTypeTableTag openTypeTableTag,         
                                  [System::Runtime::InteropServices::Out] array<byte>^%    tableData
                                  )
    {
        void* tableDataDWrite;
        void* tableContext;
        UINT32 tableSizeDWrite = 0;
        BOOL exists = FALSE;
        tableData = nullptr;
        HRESULT hr = _fontFace->Value->TryGetFontTable(
                                               (UINT32)openTypeTableTag,
                                               (const void**)&tableDataDWrite,
                                               &tableSizeDWrite,
                                               &tableContext,
                                               &exists
                                               );
        ConvertHresultToException(hr, "bool FontFace::TryGetFontTable");

        if (exists)
        {
            tableData = gcnew array<byte>(tableSizeDWrite);
            for(unsigned int i = 0; i < tableSizeDWrite; ++i)
            {
                tableData[i] = ((byte*)tableDataDWrite)[i];
            }
            
            _fontFace->Value->ReleaseFontTable(
                                       tableContext
                                       );
        }
        System::GC::KeepAlive(_fontFace);
        return (!!exists);     
    }

    /// <SecurityNote>
    /// Critical - Uses security critical member _fontFace.
    /// Safe     - It does not expose the pointer it uses.
    /// </SecurityNote>
    [SecuritySafeCritical]
    __declspec(noinline) bool FontFace::ReadFontEmbeddingRights([System::Runtime::InteropServices::Out] unsigned short% fsType)
    {
        void* os2Table;
        void* tableContext;
        UINT32 tableSizeDWrite = 0;
        BOOL exists = FALSE;
        fsType = 0;
        HRESULT hr = _fontFace->Value->TryGetFontTable(
                                               (UINT32)DWRITE_MAKE_OPENTYPE_TAG('O','S','/','2'),
                                               (const void**)&os2Table,
                                               &tableSizeDWrite,
                                               &tableContext,
                                               &exists
                                               );
        ConvertHresultToException(hr, "bool FontFace::ReadFontEmbeddingRights");

        const int OFFSET_OS2_fsType = 8;
        bool success = false;
        if (exists)
        {
            if (tableSizeDWrite >= OFFSET_OS2_fsType + 1)
            {
                byte *readBuffer = (byte *)os2Table + OFFSET_OS2_fsType;
                fsType = (unsigned short)((readBuffer[0] << 8) + readBuffer[1]);
                success = true;
            }
            
            _fontFace->Value->ReleaseFontTable(tableContext);
        }
        
        System::GC::KeepAlive(_fontFace);
        
        return success; 
    }
}}}}//MS::Internal::Text::TextInterface
