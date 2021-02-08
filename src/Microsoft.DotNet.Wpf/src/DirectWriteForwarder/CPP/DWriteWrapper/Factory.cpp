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
    Factory^ Factory::Create(
                            FactoryType                   factoryType,
                            IFontSourceCollectionFactory^ fontSourceCollectionFactory,
                            IFontSourceFactory^           fontSourceFactory
                            )
    {
        return gcnew Factory(factoryType, fontSourceCollectionFactory, fontSourceFactory);
    }

    Factory::Factory(
                    FactoryType                   factoryType,
                    IFontSourceCollectionFactory^ fontSourceCollectionFactory,
                    IFontSourceFactory^           fontSourceFactory
                    ) : CriticalHandle(IntPtr::Zero)
    {
        Initialize(factoryType);
        _wpfFontFileLoader       = gcnew FontFileLoader(fontSourceFactory);
        _wpfFontCollectionLoader = gcnew FontCollectionLoader(
                                                           fontSourceCollectionFactory,
                                                           _wpfFontFileLoader
                                                           );

        _fontSourceFactory = fontSourceFactory;

        IntPtr pIDWriteFontFileLoaderMirror = Marshal::GetComInterfaceForObject(
                                                _wpfFontFileLoader,
                                                IDWriteFontFileLoaderMirror::typeid);
        
        // Future improvement note: 
        // This seems a bit hacky, but unclear at this time how to implement this any better. 
        // When we attempt to unregister these, do we need to keep around the same IntPtr
        // representing the result of GetComInterfaceForObject to free it ? Or will it 
        // be the same if we call it again?



        HRESULT hr = _pFactory->RegisterFontFileLoader(
                                    (IDWriteFontFileLoader *)pIDWriteFontFileLoaderMirror.ToPointer()
                                    );

        Marshal::Release(pIDWriteFontFileLoaderMirror);

        ConvertHresultToException(hr, "Factory::Factory");

        IntPtr pIDWriteFontCollectionLoaderMirror = Marshal::GetComInterfaceForObject(
                                                _wpfFontCollectionLoader,
                                                IDWriteFontCollectionLoaderMirror::typeid);
        hr = _pFactory->RegisterFontCollectionLoader(
                                                    (IDWriteFontCollectionLoader *)pIDWriteFontCollectionLoaderMirror.ToPointer()
                                                    );

        Marshal::Release(pIDWriteFontCollectionLoaderMirror);
        
        ConvertHresultToException(hr, "Factory::Factory");
    }

    __declspec(noinline) void Factory::Initialize(
                                                 FactoryType factoryType
                                                 )
    {
        IUnknown* factoryTemp;
        DWRITECREATEFACTORY pfnDWriteCreateFactory = (DWRITECREATEFACTORY)GetDWriteCreateFactoryFunctionPointer();

        HRESULT hr = (*pfnDWriteCreateFactory)(
            DWriteTypeConverter::Convert(factoryType),
            __uuidof(IDWriteFactory),
            &factoryTemp
            );
        
        ConvertHresultToException(hr, "Factory::Initialize");

        _pFactory = (IDWriteFactory*)factoryTemp;
    }

    #pragma warning (disable : 4950) // The Constrained Execution Region (CER) feature is not supported.  
    [ReliabilityContract(Consistency::WillNotCorruptState, Cer::Success)]
    #pragma warning (default : 4950) // The Constrained Execution Region (CER) feature is not supported.  
    __declspec(noinline) bool Factory::ReleaseHandle()
    {
        if (_wpfFontCollectionLoader != nullptr)
        {
            IntPtr pIDWriteFontCollectionLoaderMirror = Marshal::GetComInterfaceForObject(
                                                _wpfFontCollectionLoader,
                                                IDWriteFontCollectionLoaderMirror::typeid);
            
            _pFactory->UnregisterFontCollectionLoader((IDWriteFontCollectionLoader *)pIDWriteFontCollectionLoaderMirror.ToPointer());
            Marshal::Release(pIDWriteFontCollectionLoaderMirror);
            _wpfFontCollectionLoader = nullptr;
        }
        
        if (_wpfFontFileLoader != nullptr)
        {
            IntPtr pIDWriteFontFileLoaderMirror = Marshal::GetComInterfaceForObject(
                                                    _wpfFontFileLoader,
                                                    IDWriteFontFileLoaderMirror::typeid);
            
            _pFactory->UnregisterFontFileLoader((IDWriteFontFileLoader *)pIDWriteFontFileLoaderMirror.ToPointer());
            Marshal::Release(pIDWriteFontFileLoaderMirror);            
            _wpfFontFileLoader = nullptr;
        }

        if (_pFactory != NULL)
        {
            _pFactory ->Release();
            _pFactory  = NULL;
        }

        return true;        
    }

    __declspec(noinline) FontFile^ Factory::CreateFontFile(
                                     System::Uri^ filePathUri
                                     )
    {        
        IDWriteFontFile* dwriteFontFile = NULL;
        HRESULT hr = Factory::CreateFontFile(_pFactory, _wpfFontFileLoader, filePathUri, &dwriteFontFile);

        // If DWrite's CreateFontFileReference fails then try opening the file using WPF's logic.
        // The failures that WPF returns are more granular than the HRESULTs that DWrite returns
        // thus we use WPF's logic to open the file to throw the same exceptions that
        // WPF would have thrown before.
        if (FAILED(hr))
        {
            IFontSource^ fontSource = _fontSourceFactory->Create(filePathUri->AbsoluteUri);
            fontSource->TestFileOpenable();

        }

        //This call is made to prevent this object from being collected and hence get its finalize method called 
        //While there are others referencing it.
        System::GC::KeepAlive(this);

        ConvertHresultToException(hr, "FontFile^ Factory::CreateFontFile");

        return gcnew FontFile(dwriteFontFile);

    }

    FontFace^ Factory::CreateFontFace(
                                     System::Uri^ filePathUri,
                                     unsigned int faceIndex
                                     )
    {        
        return CreateFontFace(
                             filePathUri,
                             faceIndex,
                             FontSimulations::None
                             );
    }

    FontFace^ Factory::CreateFontFace(
                                     System::Uri^    filePathUri,
                                     unsigned int    faceIndex,
                                     FontSimulations fontSimulationFlags
                                     )
    {
        FontFile^ fontFile = CreateFontFile(filePathUri);
        DWRITE_FONT_FILE_TYPE dwriteFontFileType;
        DWRITE_FONT_FACE_TYPE dwriteFontFaceType;
        unsigned int numberOfFaces = 0;

        HRESULT hr;
        if (fontFile->Analyze(
                             dwriteFontFileType,
                             dwriteFontFaceType,
                             numberOfFaces,
                             hr
                             ))
        {
            if (faceIndex >= numberOfFaces)
            {
                throw gcnew System::ArgumentOutOfRangeException("faceIndex");
            }

            unsigned char dwriteFontSimulationsFlags = DWriteTypeConverter::Convert(fontSimulationFlags);
            IDWriteFontFace* dwriteFontFace = NULL;
            IDWriteFontFile* dwriteFontFile = fontFile->DWriteFontFileNoAddRef;
            
            HRESULT hr = _pFactory->CreateFontFace(
                                                 dwriteFontFaceType,
                                                 1,
                                                 &dwriteFontFile,
                                                 faceIndex,
                                                 (DWRITE_FONT_SIMULATIONS) dwriteFontSimulationsFlags,
                                                 &dwriteFontFace
                                                 );
            System::GC::KeepAlive(fontFile);
            System::GC::KeepAlive(this);

            ConvertHresultToException(hr, "FontFace^ Factory::CreateFontFace");

            return gcnew FontFace(dwriteFontFace);
        }
        
        // This path is here because there is a behavior mismatch between DWrite and WPF.
        // If a directory was given instead of a font uri WPF previously throws 
        // System.UnauthorizedAccessException. We handle most of the exception behavior mismatch 
        // in FontFile^ Factory::CreateFontFile by opening the file using WPF's previous (prior to DWrite integration) logic if 
        // CreateFontFileReference fails (please see comments in FontFile^ Factory::CreateFontFile).
        // However in this special case DWrite's CreateFontFileReference will succeed if given
        // a directory instead of a font file and it is the Analyze() call will fail returning DWRITE_E_FILEFORMAT.
        // Thus, incase the hr returned from Analyze() was DWRITE_E_FILEFORMAT we do as we did in FontFile^ Factory::CreateFontFile
        // to try and open the file using WPF old logic and throw System.UnauthorizedAccessException as WPF used to do.
        // If a file format exception is expected then opening the file should succeed and ConvertHresultToException()
        // Should throw the correct exception.
        // A final note would be that this overhead is only incurred in error conditions and so the normal execution path should
        // not be affected.
        else
        {
            if (hr == DWRITE_E_FILEFORMAT)
            {
                IFontSource^ fontSource = _fontSourceFactory->Create(filePathUri->AbsoluteUri);
                fontSource->TestFileOpenable();
            }
            ConvertHresultToException(hr, "FontFace^ Factory::CreateFontFace");
        }

        return nullptr;
    }

    FontCollection^ Factory::GetSystemFontCollection()
    {
        return GetSystemFontCollection(false);
    }

    __declspec(noinline) FontCollection^ Factory::GetSystemFontCollection(
                                                    bool checkForUpdates
                                                    )
    {
        IDWriteFontCollection* dwriteFontCollection = NULL;
        HRESULT hr                                  = _pFactory->GetSystemFontCollection(
                                                                                       &dwriteFontCollection,
                                                                                       checkForUpdates
                                                                                       );
        System::GC::KeepAlive(this);

        ConvertHresultToException(hr, "FontCollection^ Factory::GetSystemFontCollection");

        return gcnew FontCollection(dwriteFontCollection);
    }

    __declspec(noinline) FontCollection^ Factory::GetFontCollection(System::Uri^ uri)
    {
        System::String^ uriString = uri->AbsoluteUri;
        IDWriteFontCollection* dwriteFontCollection = NULL;
        pin_ptr<const WCHAR> uriPathWChar           = PtrToStringChars(uriString);

        IntPtr pIDWriteFontCollectionLoaderMirror = Marshal::GetComInterfaceForObject(
                                                _wpfFontCollectionLoader,
                                                IDWriteFontCollectionLoaderMirror::typeid);

        IDWriteFontCollectionLoader * pIDWriteFontCollectionLoader = 
            (IDWriteFontCollectionLoader *)pIDWriteFontCollectionLoaderMirror.ToPointer();
                        
        HRESULT hr = _pFactory->CreateCustomFontCollection(
                           pIDWriteFontCollectionLoader,
                           uriPathWChar,
                           (uriString->Length+1) * sizeof(WCHAR),
                           &dwriteFontCollection
                           );

        Marshal::Release(pIDWriteFontCollectionLoaderMirror);
        
        System::GC::KeepAlive(this);

        ConvertHresultToException(hr, "FontCollection^ Factory::GetFontCollection");

        return gcnew FontCollection(dwriteFontCollection);
    }  

    HRESULT Factory::CreateFontFile(
                                   IDWriteFactory*         factory,
                                   FontFileLoader^         fontFileLoader,
                                   System::Uri^            filePathUri,
                                   __out IDWriteFontFile** dwriteFontFile
                                   )
    {
        bool isLocal = Factory::IsLocalUri(filePathUri);
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

    __declspec(noinline) void Factory::CleanupTimeStampCache()
    {
        _timeStampCacheCleanupOp = nullptr;
        _timeStampCache->Clear();
    }

    __declspec(noinline) TextAnalyzer^ Factory::CreateTextAnalyzer()
    {
        IDWriteTextAnalyzer* textAnalyzer = NULL;
        HRESULT hr = _pFactory->CreateTextAnalyzer(&textAnalyzer);
        System::GC::KeepAlive(this);
        ConvertHresultToException(hr, "TextAnalyzer^ Factory::CreateTextAnalyzer");
        return gcnew TextAnalyzer(textAnalyzer);
    }

    bool Factory::IsInvalid::get()
    {
        return (_pFactory == NULL);
    }
}}}}//MS::Internal::Text::TextInterface
