// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontFileLoader.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    FontFileLoader::FontFileLoader(IFontSourceFactory^ fontSourceFactory)
    {
        _fontSourceFactory = fontSourceFactory;
    }

    [ComVisible(true)]
    HRESULT FontFileLoader::CreateStreamFromKey(
                                                __in_bcount(fontFileReferenceKeySize) void const* fontFileReferenceKey,
                                                UINT32 fontFileReferenceKeySize,
                                                __out IntPtr* fontFileStream
                                                )
    {
        UINT32 numberOfCharacters = fontFileReferenceKeySize / sizeof(WCHAR);
        if (   (fontFileStream == nullptr) 
            || (fontFileReferenceKeySize % sizeof(WCHAR) != 0)                      // The fontFileReferenceKeySize must be divisible by sizeof(WCHAR)
            || (numberOfCharacters <= 1)                                            // The fontFileReferenceKey cannot be less than or equal 1 character as it has to contain the NULL character.
            || (((WCHAR*)fontFileReferenceKey)[numberOfCharacters - 1] != '\0'))    // The fontFileReferenceKey must end with the NULL character
        {
            return E_INVALIDARG;
        }

        *fontFileStream = IntPtr::Zero;
       
        String^ uriString = gcnew String((WCHAR*)fontFileReferenceKey);

        HRESULT hr = S_OK;

        try
        {
            IFontSource^ fontSource = _fontSourceFactory->Create(uriString);        
            FontFileStream^ customFontFileStream = gcnew FontFileStream(fontSource);

            IntPtr pIDWriteFontFileStreamMirror = Marshal::GetComInterfaceForObject(
                                                    customFontFileStream,
                                                    (IDWriteFontFileStreamMirror::typeid));
            
            
            *fontFileStream = pIDWriteFontFileStreamMirror;
        }
        catch(System::Exception^ exception)
        {
            hr = System::Runtime::InteropServices::Marshal::GetHRForException(exception);
        }

        return hr;
    }
}}}}//MS::Internal::Text::TextInterface
