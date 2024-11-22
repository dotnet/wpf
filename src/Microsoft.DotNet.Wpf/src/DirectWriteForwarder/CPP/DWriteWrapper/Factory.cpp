// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "Factory.h"
#include <float.h>
#include "DWriteInterfaces.h"

using namespace System::Runtime::InteropServices;
using namespace MS::Internal::Text::TextInterface::Interfaces;
#ifndef _CLR_NETCORE
#endif
using namespace System::Threading;

typedef HRESULT (WINAPI *DWRITECREATEFACTORY)(DWRITE_FACTORY_TYPE factoryType, REFIID iid, IUnknown **factory);

extern void *GetDWriteCreateFactoryFunctionPointer();

namespace MS { namespace Internal { namespace Text { namespace TextInterface
{
    HRESULT InternalFactory::CreateFontFile(
                                   IDWriteFactory*         factory,
                                   FontFileLoader^         fontFileLoader,
                                   System::Uri^            filePathUri,
                                   __out IDWriteFontFile** dwriteFontFile
                                   )
    {
        bool isLocal = InternalFactory::IsLocalUri(filePathUri);
        HRESULT hr = E_FAIL;

        if (isLocal)
        {
            //Protect the pointer to the filepath from being moved around by the garbage collector.
            pin_ptr<const WCHAR> uriPathWChar = PtrToStringChars(filePathUri->LocalPath);

            // DWrite currently has a slow lookup for the last write time, which
            // introduced a noticable perf regression when we switched over.
            // To mitigate this scenario, we will fetch the timestamp ourselves
            // and cache it for future calls.
            //
            // Note: we only do this if a Dispatcher exists for the current
            // thread.  There is a seperate cache for each thread.
            ::FILETIME timeStamp;
            ::FILETIME * pTimeStamp = NULL; // If something fails, do nothing and let DWrite sort it out.
            System::Runtime::InteropServices::ComTypes::FILETIME cachedTimeStamp;
            Dispatcher^ currentDispatcher = Dispatcher::FromThread(Thread::CurrentThread);
            if(currentDispatcher != nullptr)
            {
                // One-time initialization per thread.
                if(_timeStampCache == nullptr)
                {
                    _timeStampCache = gcnew Dictionary<System::Uri^,System::Runtime::InteropServices::ComTypes::FILETIME>();                
                }
                
                if(_timeStampCache->TryGetValue(filePathUri, cachedTimeStamp))
                {
                    // Convert managed FILETIME to native FILETIME, pass to DWrite
                    timeStamp.dwLowDateTime = cachedTimeStamp.dwLowDateTime;
                    timeStamp.dwHighDateTime = cachedTimeStamp.dwHighDateTime;
                    pTimeStamp = &timeStamp;
                }
                else
                {
                    // Nothing was in the cache for this font URI, so try to open
                    // the file to fetch the timestamp.  We open the file, rather
                    // than calling APIs like GetFileAttributesEx, so that symbolic
                    // links will resolve and also to ensure the timestamp is
                    // accurate.
                    //
                    // As of 10/7/2011 these flags we the same as DWrite uses.
                    HANDLE hFile = ::CreateFile(
                        uriPathWChar,
                        GENERIC_READ,
                        FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
                        NULL,
                        OPEN_EXISTING,
                        FILE_FLAG_RANDOM_ACCESS, // hint to the OS that the file is accessed randomly
                        NULL);
                    if(hFile != INVALID_HANDLE_VALUE)
                    {
                        ::BY_HANDLE_FILE_INFORMATION fileInfo;
                        if(::GetFileInformationByHandle(hFile, &fileInfo))
                        {
                            // Convert native FILETIME to managed FILETIME and cache.
                            cachedTimeStamp.dwLowDateTime = fileInfo.ftLastWriteTime.dwLowDateTime;
                            cachedTimeStamp.dwHighDateTime = fileInfo.ftLastWriteTime.dwHighDateTime;
                            _timeStampCache->Add(filePathUri, cachedTimeStamp);
                            
                            // We don't want to hold this cached value for a long time since
                            // all font references will be tied to this timestamp, and any font
                            // update during the lifetime of the application will cause is to
                            // encounter errors.  So we use a dispatcher operation to clear
                            // the cache as soon as we get back to pumping messages.
                            if(_timeStampCacheCleanupOp == nullptr)
                            {
                                _timeStampCacheCleanupOp = currentDispatcher->BeginInvoke(gcnew Action(CleanupTimeStampCache));
                            }
                            
                            // Pass this write time to DWrite
                            timeStamp = fileInfo.ftLastWriteTime;
                            pTimeStamp = &timeStamp;
                        }

                        CloseHandle(hFile);
                        hFile = NULL;
                    }
                }
            }

            hr = factory->CreateFontFileReference(
                                                  uriPathWChar,
                                                  pTimeStamp,
                                                  dwriteFontFile
                                                  );
        }
        else
        {
            System::String^ filePath = filePathUri->AbsoluteUri;
            //Protect the pointer to the filepath from being moved around by the garbage collector.
            pin_ptr<const WCHAR> uriPathWChar = PtrToStringChars(filePath);

            IntPtr pIDWriteFontFileLoaderMirror = Marshal::GetComInterfaceForObject(
                                                    fontFileLoader,
                                                    IDWriteFontFileLoaderMirror::typeid);
            
            hr = factory->CreateCustomFontFileReference(
                                                        uriPathWChar,
                                                        (filePath->Length + 1) * sizeof(WCHAR),
                                                        (IDWriteFontFileLoader *)pIDWriteFontFileLoaderMirror.ToPointer(),
                                                        dwriteFontFile
                                                        );

            Marshal::Release(pIDWriteFontFileLoaderMirror);
        }
        return hr;
    }

    __declspec(noinline) void InternalFactory::CleanupTimeStampCache()
    {
        _timeStampCacheCleanupOp = nullptr;
        _timeStampCache->Clear();
    }

}}}}//MS::Internal::Text::TextInterface
