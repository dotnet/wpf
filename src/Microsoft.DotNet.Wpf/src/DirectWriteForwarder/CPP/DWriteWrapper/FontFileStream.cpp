// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "FontFileStream.h"

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    FontFileStream::FontFileStream(IFontSource^ fontSource)
    {
        // Previously we were using fontSource->GetStream() which caused crashes in the XPS scenarios
        // as the stream was getting closed by some other object. In XPS scenarios GetStream() would return
        // MS::Internal::IO:Packaging::SynchronizingStream which is owned by the XPS docs and
        // is known to have issues regarding the lifetime management where by if the current XPS page is 
        // flipped then the stream will get disposed. Thus, we cannot rely on the stream directly and hence we now use
        // fontSource->GetUnmanagedStream() which returns a copy of the content of the stream. Special casing XPS will not
        // guarantee that this problem will be fixed so we will use the GetUnmanagedStream(). Note: This path will only 
        // be taken for embedded fonts among which XPS is a main scenario. For local fonts we use DWrite's APIs.
        _fontSourceStream = fontSource->GetUnmanagedStream();
        try
        {
            _lastWriteTime = fontSource->GetLastWriteTimeUtc().ToFileTimeUtc();
        }    
        catch(System::ArgumentOutOfRangeException^) //The resulting file time would represent a date and time before 12:00 midnight January 1, 1601 C.E. UTC. 
        {
            _lastWriteTime = -1;
        }        

        // Create lock to control access to font source stream.
        _fontSourceStreamLock = gcnew Object();
    }

    FontFileStream::~FontFileStream()
    {
        _fontSourceStream->Close();
    }

    [ComVisible(true)]
    HRESULT FontFileStream::ReadFileFragment(
        __deref_out_bcount(fragmentSize) const void ** fragmentStart,
        UINT64 fileOffset,
        UINT64 fragmentSize,
        __out void** fragmentContext
        )
    {
        HRESULT hr = S_OK;
        try
        {
            if(
                fragmentContext == NULL || fragmentStart == NULL
                ||
                fileOffset   > long::MaxValue                    // Cannot safely cast to long
                ||            
                fragmentSize > int::MaxValue                     // fragment size cannot be casted to int
                || 
                fileOffset > UINT64_MAX - fragmentSize           // make sure next sum doesn't overflow
                || 
                fileOffset + fragmentSize  > (UINT64)_fontSourceStream->Length // reading past the end of the Stream
              ) 
            {
                return E_INVALIDARG;
            }

            int fragmentSizeInt = (int)fragmentSize;
            array<byte>^ buffer = gcnew array<byte>(fragmentSizeInt);
            
            // DWrite may call this method from multiple threads. We need to ensure thread safety by making Seek and Read atomic.
            System::Threading::Monitor::Enter(_fontSourceStreamLock);
            try
            {
                _fontSourceStream->Seek(fileOffset, //long
                                        System::IO::SeekOrigin::Begin);

                _fontSourceStream->Read(buffer,         //byte[]
                                        0,              //int
                                        fragmentSizeInt //int
                                        );
            }
            finally 
            {
                System::Threading::Monitor::Exit(_fontSourceStreamLock);
            }

            GCHandle gcHandle = GCHandle::Alloc(buffer, GCHandleType::Pinned);

            *fragmentStart = (byte*)(gcHandle.AddrOfPinnedObject().ToPointer());
            
            *fragmentContext = GCHandle::ToIntPtr(gcHandle).ToPointer();
        }
        catch(System::Exception^ exception)
        {
            hr = System::Runtime::InteropServices::Marshal::GetHRForException(exception);
        }

        return hr;
    }

#ifndef _CLR_NETCORE
#endif
    [ComVisible(true)]
    void FontFileStream::ReleaseFileFragment(
        void* fragmentContext
        )
    {
        if (fragmentContext != NULL)
        {
            GCHandle gcHandle = GCHandle::FromIntPtr(IntPtr(fragmentContext));
            gcHandle.Free();
        }
    }

    [ComVisible(true)]
    HRESULT FontFileStream::GetFileSize(
        __out UINT64* fileSize
        )
    {
        if (fileSize == NULL)
        {
            return E_INVALIDARG;
        }

        HRESULT hr = S_OK;
        try
        {
            *fileSize = _fontSourceStream->Length;
        }
        catch(System::Exception^ exception)
        {
            hr = System::Runtime::InteropServices::Marshal::GetHRForException(exception);
        }

        return hr;
     }

    HRESULT FontFileStream::GetLastWriteTime(
        __out UINT64* lastWriteTime
        )
    {
        if (_lastWriteTime < 0) //The resulting file time would represent a date and time before 12:00 midnight January 1, 1601 C.E. UTC.
        {
            return E_FAIL;
        }
        if (lastWriteTime == NULL)
        {
            return E_INVALIDARG;
        }        
        *lastWriteTime = _lastWriteTime;
        return S_OK;
    }
}}}}//MS::Internal::Text::TextInterface
