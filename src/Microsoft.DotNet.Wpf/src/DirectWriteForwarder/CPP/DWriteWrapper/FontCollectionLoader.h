// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTCOLLECTIONLOADER_H
#define __FONTCOLLECTIONLOADER_H

#include "IFontSourceCollection.h"
#include "FontFileEnumerator.h"
#include "Common.h"
#include "DWriteInterfaces.h"

using namespace System;
using namespace System::Diagnostics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    [ClassInterface(ClassInterfaceType::None), ComVisible(true)]
    private ref class FontCollectionLoader : public IDWriteFontCollectionLoaderMirror
    {
        private:

            IFontSourceCollectionFactory^         _fontSourceCollectionFactory;
            FontFileLoader^                       _fontFileLoader;

        public:
   
            FontCollectionLoader() { Debug::Assert(false); }
            
            FontCollectionLoader(
                                IFontSourceCollectionFactory^ fontSourceCollectionFactory,
                                FontFileLoader^               fontFileLoader
                                );

            /// <summary>
            /// Creates a font file enumerator object that encapsulates a collection of font files.
            /// The font system calls back to this interface to create a font collection.
            /// </summary>
            /// <param name="collectionKey">Font collection key that uniquely identifies the collection of font files within
            /// the scope of the font collection loader being used.</param>
            /// <param name="collectionKeySize">Size of the font collection key in bytes.</param>
            /// <param name="fontFileEnumerator">Pointer to the newly created font file enumerator.</param>
            /// <returns>
            /// Standard HRESULT error code.
            /// </returns>
            [ComVisible(true)]
            virtual HRESULT CreateEnumeratorFromKey(
                          IntPtr factory,
                          __in_bcount(collectionKeySize) void const* collectionKey,
                          UINT32 collectionKeySize,
                          __out IntPtr* fontFileEnumerator
                          );
    };
}}}}//MS::Internal::Text::TextInterface

#endif //__FONTCOLLECTIONLOADER_H
