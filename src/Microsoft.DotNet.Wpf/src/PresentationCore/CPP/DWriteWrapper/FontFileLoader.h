// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFILELOADER_H
#define __FONTFILELOADER_H

#include "Common.h"
#include "IFontSource.h"
#include "FontFileStream.h"

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    [ClassInterface(ClassInterfaceType::None), ComVisible(true)]
    private ref class FontFileLoader : public IDWriteFontFileLoaderMirror
    {
        IFontSourceFactory^         _fontSourceFactory;

    public:

        FontFileLoader() { Debug::Assert(false); }

        FontFileLoader(IFontSourceFactory^ fontSourceFactory);

        /// <summary>
        /// Creates a font file stream object that encapsulates an open file resource.
        /// The resource is closed when the last reference to fontFileStream is released.
        /// </summary>
        /// <param name="fontFileReferenceKey">Font file reference key that uniquely identifies the font file resource
        /// within the scope of the font loader being used.</param>
        /// <param name="fontFileReferenceKeySize">Size of font file reference key in bytes.</param>
        /// <param name="fontFileStream">Pointer to the newly created font file stream.</param>
        /// <returns>
        /// Standard HRESULT error code.
        /// </returns>
        [ComVisible(true)]
        virtual HRESULT CreateStreamFromKey(
                                      __in_bcount(fontFileReferenceKeySize) void const* fontFileReferenceKey,
                                      UINT32 fontFileReferenceKeySize,
                                      __out IntPtr* fontFileStream
                                      );
    };
    
}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFILELOADER_H
