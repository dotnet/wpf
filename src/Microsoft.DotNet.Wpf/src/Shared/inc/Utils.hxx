// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <sal.h>
#include <codeanalysis/sourceannotations.h> // TEMPORARY INCLUDE
#include <Windows.h>

namespace WPFUtils {

    LONG ReadRegistryString(__in HKEY rootKey, __in LPCWSTR keyName, __in LPCWSTR valueName,
                                     __out LPWSTR value, size_t cchMax);

    HRESULT GetWPFInstallPath(__out_ecount(cchMaxPath) LPWSTR pszPath, size_t cchMaxPath);
}