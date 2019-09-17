// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "dwriteloader.h"

namespace WPFUtils
{

#if defined(__cplusplus_cli)
#endif
HMODULE LoadDWriteLibraryAndGetProcAddress(void **pfncptrDWriteCreateFactory)
{
    HMODULE hDWriteLibrary = nullptr;
    
    // KB2533623 introduced the LOAD_LIBRARY_SEARCH_SYSTEM32 flag. It also introduced
    // the AddDllDirectory function. We test for presence of AddDllDirectory as an 
    // indirect evidence for the support of LOAD_LIBRARY_SEARCH_SYSTEM32 flag. 
    HMODULE hKernel32 = GetModuleHandle(L"kernel32.dll");
    if (hKernel32 != nullptr)
    {
        if (GetProcAddress(hKernel32, "AddDllDirectory") != nullptr)
        {
            // All supported platforms newer than Vista SP2 shipped with dwrite.dll.
            // On Vista SP2, the .NET servicing process will ensure that a MSU containing 
            // dwrite.dll will be delivered as a prerequisite - effectively guaranteeing that 
            // this following call to LoadLibraryEx(dwrite.dll) will succeed, and that it will 
            // not be susceptible to typical DLL planting vulnerability vectors.
            hDWriteLibrary = LoadLibraryEx(L"dwrite.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
        }
        else 
        {
            // LOAD_LIBRARY_SEARCH_SYSTEM32 is not supported on this OS. 
            // Fall back to using plain ol' LoadLibrary
            // There is risk that this call might fail, or that it might be
            // susceptible to DLL hijacking. 
            hDWriteLibrary = LoadLibrary(L"dwrite.dll");
        }
    }
    
    if (hDWriteLibrary)
    {
        *pfncptrDWriteCreateFactory = GetProcAddress(hDWriteLibrary, "DWriteCreateFactory");
    }

    return hDWriteLibrary;
}

}// namespace WPFUtils
