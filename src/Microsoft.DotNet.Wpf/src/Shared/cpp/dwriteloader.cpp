// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "dwriteloader.h"

namespace WPFUtils
{

HMODULE LoadDWriteLibraryAndGetProcAddress(void **pfncptrDWriteCreateFactory)
{
    HMODULE hDWriteLibrary = LoadLibraryEx(L"dwrite.dll", nullptr, LOAD_LIBRARY_SEARCH_SYSTEM32);
    if (hDWriteLibrary)
    {
        *pfncptrDWriteCreateFactory = GetProcAddress(hDWriteLibrary, "DWriteCreateFactory");
    }

    return hDWriteLibrary;
}

}// namespace WPFUtils
