// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma once

#include <windows.h>

namespace WPFUtils
{
    #if defined(__cplusplus_cli)
    #endif
    HMODULE LoadDWriteLibraryAndGetProcAddress(void **pfncptrDWriteCreateFactory);
}
