// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFILEENUMERATOR_H
#define __FONTFILEENUMERATOR_H

#include "IFontSource.h"
#include "Common.h"
#include "FontFileLoader.h"
#include "DWriteInterfaces.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Diagnostics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    [ClassInterface(ClassInterfaceType::None), ComVisible(true)]
    private ref class FontFileEnumerator : public IDWriteFontFileEnumeratorMirror
    {     
        private:

            IEnumerator<IFontSource^>^    _fontSourceCollectionEnumerator;
            FontFileLoader^               _fontFileLoader;
            IDWriteFactory*               _factory;

        public:

            FontFileEnumerator() { Debug::Assert(false); }

            FontFileEnumerator(
                              IEnumerable<IFontSource^>^ fontSourceCollection,
                              FontFileLoader^            fontFileLoader,
                              IDWriteFactory*            factory
                              );

            /// <summary>
            /// Advances to the next font file in the collection. When it is first created, the enumerator is positioned
            /// before the first element of the collection and the first call to MoveNext advances to the first file.
            /// </summary>
            /// <param name="hasCurrentFile">Receives the value TRUE if the enumerator advances to a file, or FALSE if
            /// the enumerator advanced past the last file in the collection.</param>
            /// <returns>
            /// Standard HRESULT error code.
            /// </returns>
            [ComVisible(true)]
            virtual HRESULT MoveNext(
                               __out bool% hasCurrentFile
                               );

            /// <summary>
            /// Gets a reference to the current font file.
            /// </summary>
            /// <param name="fontFile">Pointer to the newly created font file object.</param>
            /// <returns>
            /// Standard HRESULT error code.
            /// </returns>
            [ComVisible(true)]
            virtual HRESULT GetCurrentFontFile(
                                         __out IDWriteFontFile** fontFile
                                         );
    };
}}}}//MS::Internal::Text::TextInterface

#endif //__FONTFILEENUMERATOR_H
