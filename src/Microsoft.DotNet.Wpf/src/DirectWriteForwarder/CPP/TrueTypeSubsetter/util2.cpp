// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "precomp.hxx"
#include "util2.h"

namespace MS { namespace Internal { namespace FontCache {

   

bool Util2::GetRegistryKeyLastWriteTimeUtc(System::String ^ registryKey, [System::Runtime::InteropServices::Out] System::Int64 % lastWriteTime)
{
    HKEY hkey = NULL;

    pin_ptr<const wchar_t> registryKeyUnmanaged = PtrToStringChars(registryKey);

    if (::RegOpenKeyExW(HKEY_LOCAL_MACHINE, registryKeyUnmanaged, 0, KEY_QUERY_VALUE, &hkey) == ERROR_SUCCESS)
    {
        try
        {
            ::FILETIME fileTime = { 0 };

            if (::RegQueryInfoKey(hkey, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, &fileTime) == ERROR_SUCCESS)
            {
                System::Int64 dt = ((System::Int64)fileTime.dwHighDateTime << 32) | ((System::Int64)fileTime.dwLowDateTime);
                System::DateTime ^ dateTime = System::DateTime::FromFileTimeUtc(dt);
                lastWriteTime = dateTime->Ticks;
                return true;
            }
        }
        finally
        {
            ::RegCloseKey(hkey);
        }
    }
    lastWriteTime = 0;
    return false;
}

}}}
