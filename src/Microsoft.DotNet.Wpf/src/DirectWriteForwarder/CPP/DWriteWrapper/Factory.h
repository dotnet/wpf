// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FACTORY_H
#define __FACTORY_H

#include "Common.h"
#include "FactoryType.h"
#include "FontFile.h"
#include "FontFace.h"
#include "FontCollection.h"
#include "DWriteTypeConverter.h"
#include "FontFileLoader.h"
#include "TextAnalyzer.h"
#include "NativePointerWrapper.h"

using namespace MS::Internal::Text::TextInterface::Generics;
using namespace System::Windows::Threading;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    /// <summary>
    /// The root factory interface for all DWrite objects.
    /// </summary>
    private ref class InternalFactory abstract sealed
    {
        private:
            [ThreadStatic]
            static Dictionary<System::Uri^, System::Runtime::InteropServices::ComTypes::FILETIME>^ _timeStampCache;

            [ThreadStatic]
            static DispatcherOperation^ _timeStampCacheCleanupOp;

            static void CleanupTimeStampCache();
        internal:

            static bool IsLocalUri(System::Uri^ uri)
            {
                return (uri->IsFile && uri->IsLoopback && !uri->IsUnc);
            }

            /// <summary>
            /// Creates an IDWriteFontFile* from a URI, using either the DWrite built in local font file loader or our custom font 
            /// file loader implementation
            /// </summary>
            /// <param name="factory">The IDWriteFactory object<param>
            /// <param name="fontFileLoader">Reference to our previously created and registered custom font file loader</param>
            /// <param name="filePathUri">The URI</param>
            /// <param name="dwriteFontFile">The newly created IDWRiteFontFile representation</param>
            /// <returns>
            /// Standard HRESULT error code
            /// </returns>
            static HRESULT CreateFontFile(
                                         IDWriteFactory*         factory,
                                         FontFileLoader^         fontFileLoader,
                                         System::Uri^            filePathUri,
                                         __out IDWriteFontFile** dwriteFontFile
                                         );

            __declspec(noinline) static DWRITE_MATRIX GetIdentityTransform()
            {
                DWRITE_MATRIX transform;
                transform.m11=1;
                transform.m12=0;
                transform.m22=1;
                transform.m21=0;
                transform.dx =0;
                transform.dy =0;

                return transform;
            }
    };

}}}}//MS::Internal::Text::TextInterface

#endif //__FACTORY_H
