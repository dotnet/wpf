// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "Utils.hxx"
#include <shlwapi.h>

#if _MANAGED
using namespace System::Security;
#endif

// These constants are cloned in
// wpf\src\Shared\MS\Internal\Registry.cs
// Should these reg keys change the above file should be also modified to reflect that.
#define FRAMEWORK_REGKEY        L"Software\\Microsoft\\Net Framework Setup\\NDP\\v4\\Client"
#define FRAMEWORK_INSTALLPATH_REGVALUE L"InstallPath"
#define WPF_SUBDIR              L"WPF"

#define DOTNET_FRAMEWORK_REGKEY L"Software\\Microsoft\\.NETFramework"
#define DOTNET_FRAMEWORK_INSTALLROOT_REGVALUE L"InstallRoot"

#define COMPLUS_Version         L"COMPLUS_Version"
#define COMPLUS_InstallRoot     L"COMPLUS_InstallRoot"

namespace WPFUtils {


//
// Reads a string value from the registry.
// If the function succeeds, the return value is ERROR_SUCCESS.
// If the function fails, the return value is a nonzero error code defined in Winerror.h
//
#if _MANAGED
#endif
LONG ReadRegistryString(__in HKEY rootKey, __in LPCWSTR keyName, __in LPCWSTR valueName,
                                     __out LPWSTR value, size_t cchMax)
{
    HKEY key = NULL;

    LONG result = RegOpenKeyEx(rootKey, keyName, 0, KEY_READ, &key);

    if (result == ERROR_SUCCESS)
    {
        if( cchMax > INT_MAX)
        {
            result = ERROR_INVALID_PARAMETER;
        }
        else
        {
            DWORD sizeInBytes = static_cast<DWORD>(cchMax) * sizeof(WCHAR);
            DWORD type;

            result = RegQueryValueEx(key, valueName, NULL, &type, (LPBYTE)value, &sizeInBytes);

            if (result == ERROR_SUCCESS && type != REG_SZ)
            {
                result = ERROR_UNSUPPORTED_TYPE;
            }

            RegCloseKey(key);
        }
    }

    return result;
}

HRESULT GetWPFInstallPath(__out_ecount(cchMaxPath) LPWSTR pszPath, size_t cchMaxPath)
{
    HRESULT hr = S_OK;
    DWORD ch;
    WCHAR wszVersion[MAX_PATH];

    // The PathAppend function doesn't handle small buffers.
    if(cchMaxPath < MAX_PATH)
        return E_OUTOFMEMORY;

    // We support a "private CLR" which allows someone to use a different framework
    // location than what is specified in the registry.  The CLR support for this
    // involves two environment variable: COMPLUS_InstallRoot and COMPLUS_Version.
    //
    // The full path to the WPF assemblies is:
    // %COMPLUS_InstallRoot%\%COMPLUS_Version%\wpf

    // Check for the COMPLUS_Version environment variable.
    // Change the following two calls use GetEnvironmentVariableW explicitly,
    // to work around a bug, which crashes all WPF apps at startup when run
    // against a CHK/no-opt build.   When the bug is fixed, revert these to use
    // GetEnvironmentVariable again. 
    ch = GetEnvironmentVariableW(COMPLUS_Version, wszVersion, MAX_PATH);
    if (ch > 0)
    {
        // Check for the COMPLUS_InstallRoot environment variable.
        ch = GetEnvironmentVariableW(COMPLUS_InstallRoot, pszPath, static_cast<DWORD>(cchMaxPath));
        if (ch <= 0)
        {
            // The COMPLUS_Version environment variable was set, but the
            // COMPLUS_InstallRoot environment variable was not.  We fall back
            // to getting the framework install root from the registry, but
            // still use the private CLR version.
            LONG result = ReadRegistryString(HKEY_LOCAL_MACHINE, DOTNET_FRAMEWORK_REGKEY, DOTNET_FRAMEWORK_INSTALLROOT_REGVALUE, pszPath, static_cast<DWORD>(cchMaxPath));
            if (result != ERROR_SUCCESS)
            {
                hr = HRESULT_FROM_WIN32(result);
            }
        }

        // Append the InstallRoot and the Version
        if(SUCCEEDED(hr))
        {
#pragma prefast(suppress:25025, "We don't know of a better API to use in place of PathAppend. The OACR spreadsheet and MSDN do not suggest any either.")
            if (!::PathAppend(pszPath, wszVersion))
            {
                hr = E_OUTOFMEMORY;
            }
        }
    }
    else
    {
        // The COMPLUS_Version environment variable was not set.  We do not support
        // extracting the appropriate version ourselves, since this could come from
        // various places (app config, etc), so we default to 4.0.  The entire path
        // is stored in the registry, under the v4 key.
        LONG result = ReadRegistryString(HKEY_LOCAL_MACHINE, FRAMEWORK_REGKEY, FRAMEWORK_INSTALLPATH_REGVALUE, pszPath, cchMaxPath);
        if (result != ERROR_SUCCESS)
        {
            hr = HRESULT_FROM_WIN32(result);
        }
    }

    // WPF chose to make a subdirectory for its own DLLs under the framework directory.
    if (SUCCEEDED(hr))
    {
#pragma prefast(suppress:25025, "We don't know of a better API to use in place of PathAppend. The OACR spreadsheet and MSDN do not suggest any either.")
        if (!::PathAppend(pszPath, WPF_SUBDIR))
        {
            hr = E_OUTOFMEMORY;
        }
    }

    return hr;
}

}//namespace
