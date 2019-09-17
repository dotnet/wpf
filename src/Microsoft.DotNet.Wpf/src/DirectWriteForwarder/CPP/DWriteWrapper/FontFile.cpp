// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontFile.h"
#include "DWriteTypeConverter.h"


namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// static ctor to initialize the GUID of IDWriteLocalFontFileLoader interface.
    /// </summary>
    static FontFile::FontFile()
    {
        System::Guid guid = System::Guid("b2d9f3ec-c9fe-4a11-a2ec-d86208f7c0a2");
        _GUID* pGuidForIDWriteLocalFontFileLoader = new _GUID();
        *pGuidForIDWriteLocalFontFileLoader = Native::Util::ToGUID(guid);
        _guidForIDWriteLocalFontFileLoader = gcnew NativePointerWrapper<_GUID>(pGuidForIDWriteLocalFontFileLoader);                
    }

    FontFile::FontFile(IDWriteFontFile* fontFile)
    {
        _fontFile = gcnew NativeIUnknownWrapper<IDWriteFontFile>(fontFile);
    }

    __declspec(noinline) FontFile::~FontFile()
    {
        if (_fontFile != nullptr)
        {
            delete _fontFile;
            _fontFile = nullptr;
        }
    }

    __declspec(noinline) bool FontFile::Analyze(
                          [System::Runtime::InteropServices::Out] DWRITE_FONT_FILE_TYPE%  fontFileType,
                          [System::Runtime::InteropServices::Out] DWRITE_FONT_FACE_TYPE%  fontFaceType,
                          [System::Runtime::InteropServices::Out] unsigned int%  numberOfFaces,
                                                                  HRESULT&       hr
                          )
    {
        BOOL isSupported = FALSE;
        UINT32 numberOfFacesDWrite = 0;
        DWRITE_FONT_FILE_TYPE dwriteFontFileType;
        DWRITE_FONT_FACE_TYPE dwriteFontFaceType;
        hr = _fontFile->Value->Analyze(
                                &isSupported,
                                &dwriteFontFileType,
                                &dwriteFontFaceType,
                                &numberOfFacesDWrite
                                );

        System::GC::KeepAlive(_fontFile);
        if (FAILED(hr))
        {
            return false;
        }

        fontFileType = dwriteFontFileType;
        fontFaceType = dwriteFontFaceType;
        numberOfFaces = numberOfFacesDWrite;
        return (!!isSupported);
    }

    /// WARNING: AFTER GETTING THIS NATIVE POINTER YOU ARE RESPONSIBLE FOR MAKING SURE THAT THE WRAPPING MANAGED
    /// OBJECT IS KEPT ALIVE BY THE GC OR ELSE YOU ARE RISKING THE POINTER GETTING RELEASED BEFORE YOU'D 
    /// WANT TO.
    ///
    IDWriteFontFile* FontFile::DWriteFontFileNoAddRef::get()
    {
        return _fontFile->Value;
    }

    System::String^ FontFile::GetUriPath()
    {
        void* fontFileReferenceKey;
        UINT32 sizeOfFontFileReferenceKey;

        IDWriteFontFileLoader* fontFileLoader = NULL;
        HRESULT hr = _fontFile->Value->GetLoader(&fontFileLoader);        
        ConvertHresultToException(hr, "System::String^ FontFile::GetUriPath");

        void* localFontFileLoaderInterfacePointer = NULL;
        hr = fontFileLoader->QueryInterface((REFIID)*(_guidForIDWriteLocalFontFileLoader->Value), &localFontFileLoaderInterfacePointer);
        if (hr == E_NOINTERFACE)
        {
            hr = _fontFile->Value->GetReferenceKey((const void**)&fontFileReferenceKey, &sizeOfFontFileReferenceKey);
            System::GC::KeepAlive(_fontFile);
            ConvertHresultToException(hr, "System::String^ FontFile::GetUriPath");
            return gcnew System::String((WCHAR*)fontFileReferenceKey);
        }
        else
        {
            IDWriteLocalFontFileLoader* localFontFileLoader = (IDWriteLocalFontFileLoader*)localFontFileLoaderInterfacePointer;
            WCHAR* fontFilePath = NULL;

            try 
            {
                hr = _fontFile->Value->GetReferenceKey((const void**)&fontFileReferenceKey, &sizeOfFontFileReferenceKey);
                System::GC::KeepAlive(_fontFile);
                ConvertHresultToException(hr, "System::String^ FontFile::GetUriPath");

                UINT32 sizeOfFilePath;
                hr = localFontFileLoader->GetFilePathLengthFromKey(
                                                               fontFileReferenceKey,
                                                               sizeOfFontFileReferenceKey,
                                                               &sizeOfFilePath
                                                               );
                ConvertHresultToException(hr, "System::String^ FontFile::GetUriPath");                

                MS::Internal::Invariant::Assert(sizeOfFilePath >= 0 && sizeOfFilePath < UINT_MAX);

                fontFilePath = new WCHAR[sizeOfFilePath + 1];
            
                hr = localFontFileLoader->GetFilePathFromKey(
                                                            fontFileReferenceKey,
                                                            sizeOfFontFileReferenceKey,
                                                            fontFilePath,
                                                            sizeOfFilePath + 1
                                                            );
                ConvertHresultToException(hr, "System::String^ FontFile::GetUriPath");
                return gcnew System::String((wchar_t*)fontFilePath);
            }
            finally
            {
                ReleaseInterface(&localFontFileLoader);
                if(fontFilePath != NULL)
                {
                    delete [] fontFilePath;
                }
            }
        }
    }

    /// <summary>
    /// This method is used to release an IDWriteLocalFontFileLoader. This method
    /// is created to be marked with proper security attributes because when
    /// the call to Release() was made inside GetUriPath() it was causing Jitting.
    /// </summary>
    __declspec(noinline) void FontFile::ReleaseInterface(IDWriteLocalFontFileLoader** ppInterface)
    {
        if (ppInterface && *ppInterface)
        {
            (*ppInterface)->Release();
            *ppInterface = NULL;
        }
    }
    
}}}}//MS::Internal::Text::TextInterface
