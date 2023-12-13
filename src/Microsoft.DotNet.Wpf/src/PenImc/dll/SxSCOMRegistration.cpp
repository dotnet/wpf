// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "stdafx.h"
#include "peninc.h"

EXTERN_C IMAGE_DOS_HEADER __ImageBase;

// Creates an ActivationContext using the embedded manifest and pushes it on the
// context stack.  The ActivationContext cookie is returned to the caller: a non-zero value 
// indicates success; zero indicates failure.  Caller is responsible for deactivating 
// the context.
extern "C" ULONG_PTR WINAPI RegisterDllForSxSCOM()
{
    // Get the full path to this Dll
    WCHAR moduleFullPath[MAX_PATH] = {0};
    if (!GetModuleFileNameW((HINSTANCE)&__ImageBase, moduleFullPath, _countof(moduleFullPath)))
    {
        return 0;
    }

    // ACTCTX.lpResourceName must be 'ISOLATIONAWARE_MANIFEST_RESOURCE_ID' for Dlls.
    // Defined as: ISOLATIONAWARE_MANIFEST_RESOURCE_ID MAKEINTRESOURCE(2)
    ACTCTX activationContext = {}; 
    activationContext.cbSize = sizeof(activationContext); 
    activationContext.dwFlags = ACTCTX_FLAG_RESOURCE_NAME_VALID | ACTCTX_FLAG_APPLICATION_NAME_VALID;
    activationContext.lpSource = moduleFullPath;
    activationContext.lpResourceName = ISOLATIONAWARE_MANIFEST_RESOURCE_ID;

    // Create and activate context : context is added to the top of the context stack
    HANDLE activationContextHandle = ::CreateActCtxW(&activationContext);
    if (INVALID_HANDLE_VALUE == activationContextHandle)
    {
        return 0;
    }

    ULONG_PTR activationContextCookie = 0;
    BOOL activateActCtxResult = ::ActivateActCtx(activationContextHandle, &activationContextCookie);
    if (activateActCtxResult == FALSE)
    {
        return 0;
    }

    // Return the context cookie : caller is responsible for deactivating the context.
    return activationContextCookie; 
}
