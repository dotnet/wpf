// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontCollectionLoader.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{

    FontCollectionLoader::FontCollectionLoader(
                                              IFontSourceCollectionFactory^ fontSourceCollectionFactory,
                                              FontFileLoader^        fontFileLoader
                                              )
    {
        _fontSourceCollectionFactory = fontSourceCollectionFactory;
        _fontFileLoader              = fontFileLoader;
    }

    [ComVisible(true)]    
    HRESULT FontCollectionLoader::CreateEnumeratorFromKey(
                                                          IntPtr factory,
                                                          __in_bcount(collectionKeySize) void const* collectionKey,
                                                          UINT32 collectionKeySize,
                                                          __out IntPtr* fontFileEnumerator
                                                          )
    {
        UINT32 numberOfCharacters = collectionKeySize / sizeof(WCHAR);
        if (   (fontFileEnumerator == NULL) 
            || (collectionKeySize % sizeof(WCHAR) != 0)                        // The collectionKeySize must be divisible by sizeof(WCHAR)
            || (numberOfCharacters <= 1)                                       // The collectionKey cannot be less than or equal 1 character as it has to contain the NULL character.
            || (((WCHAR*)collectionKey)[numberOfCharacters - 1] != '\0'))  // The collectionKey must end with the NULL character
        {
            return E_INVALIDARG;
        }

        *fontFileEnumerator = IntPtr::Zero;

        String^ uriString = gcnew String((WCHAR*)collectionKey);
        HRESULT hr = S_OK;

        try
        {
            IFontSourceCollection^ fontSourceCollection = _fontSourceCollectionFactory->Create(uriString);
            FontFileEnumerator^ fontFileEnum = gcnew FontFileEnumerator(
                                                      fontSourceCollection,
                                                      _fontFileLoader,
                                                      (IDWriteFactory *)factory.ToPointer()
                                                      );
            IntPtr pIDWriteFontFileEnumeratorMirror = Marshal::GetComInterfaceForObject(
                                                    fontFileEnum,
                                                    IDWriteFontFileEnumeratorMirror::typeid);

            *fontFileEnumerator = pIDWriteFontFileEnumeratorMirror;
        }
        catch(System::Exception^ exception)
        {
            hr = System::Runtime::InteropServices::Marshal::GetHRForException(exception);
        }

        return hr;
    }

}}}}//MS::Internal::Text::TextInterface
