// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __FONTFILESTREAM_H
#define __FONTFILESTREAM_H

#include "IFontSource.h"
#include "Common.h"
#include "DWriteInterfaces.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Diagnostics;

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    [ClassInterface(ClassInterfaceType::None), ComVisible(true)]
    private ref class FontFileStream : public IDWriteFontFileStreamMirror
    {  
        private:
            Stream^                            _fontSourceStream;
            INT64                              _lastWriteTime;
            Object^                            _fontSourceStreamLock;

        public:

            /// Asserts false because COM convention requires us to have a default constructor
            /// but we are the only entity that can construct these objects, and we use the
            /// other constructor.            
            FontFileStream() { Debug::Assert(false); }
            
            /// <summary>
            /// ctor.
            /// </summary>
            FontFileStream(IFontSource^ fontSource);

            /// <summary>
            /// destructor. Closes the managed Input Stream.
            /// </summary>
            ~FontFileStream();

            /// <summary>
            /// Reads a fragment from a file.
            /// </summary>
            /// <param name="fragmentStart">Receives the pointer to the start of the font file fragment.</param>
            /// <param name="fileOffset">Offset of the fragment from the beginning of the font file.</param>
            /// <param name="fragmentSize">Size of the fragment in bytes.</param>
            /// <param name="fragmentContext">The client defined context to be passed to the ReleaseFileFragment.</param>
            /// <returns>
            /// Standard HRESULT error code.
            /// </returns>
            /// <remarks>
            /// IMPORTANT: ReadFileFragment() implementations must check whether the requested file fragment
            /// is within the file bounds. Otherwise, an error should be returned from ReadFileFragment.
            /// </remarks>>
            [ComVisible(true)]
            virtual HRESULT ReadFileFragment(
                __deref_out_bcount(fragmentSize) const void** fragmentStart,
                UINT64 fileOffset,
                UINT64 fragmentSize,
                __out void** fragmentContext
                );

            /// <summary>
            /// Releases a fragment from a file.
            /// </summary>
            /// <param name="fragmentContext">The client defined context of a font fragment returned from ReadFileFragment.</param>
            [ComVisible(true)]
            virtual void ReleaseFileFragment(
                void* fragmentContext
                );

            /// <summary>
            /// Obtains the total size of a file.
            /// </summary>
            /// <param name="fileSize">Receives the total size of the file.</param>
            /// <returns>
            /// Standard HRESULT error code.
            /// </returns>
            /// <remarks>
            /// Implementing GetFileSize() for asynchronously loaded font files may require
            /// downloading the complete file contents, therefore this method should only be used for operations that
            /// either require complete font file to be loaded (e.g., copying a font file) or need to make
            /// decisions based on the value of the file size (e.g., validation against a persisted file size).
            /// </remarks>
            [ComVisible(true)]
            virtual HRESULT GetFileSize(
                __out UINT64* fileSize
                );

            /// <summary>
            /// Obtains the last modified time of the file. The last modified time is used by DirectWrite font selection algorithms
            /// to determine whether one font resource is more up to date than another one.
            /// </summary>
            /// <param name="lastWriteTime">Receives the last modifed time of the file in the format that represents
            /// the number of 100-nanosecond intervals since January 1, 1601 (UTC).</param>
            /// <returns>
            /// Standard HRESULT error code. For resources that don't have a concept of the last modified time, the implementation of
            /// GetLastWriteTime should return E_NOTIMPL.
            /// </returns>
            [ComVisible(true)]
            virtual HRESULT GetLastWriteTime(
                __out UINT64* lastWriteTime
                );

    };
}}}}//MS::Internal::Text::TextInterface


#endif //__FONTFILESTREAM_H
