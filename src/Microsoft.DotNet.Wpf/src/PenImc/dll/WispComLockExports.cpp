// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "stdafx.h"

#include "ComApartmentVerifier.hpp"
#include "GitComLockableWrapper.hpp"

using namespace ComUtils;

// Exported call to lock WISP objects stored in the GIT
extern "C" BOOL WINAPI LockWispObjectFromGit(__in DWORD gitKey)
{
    GitComLockableWrapper<IUnknown> git(gitKey, ComApartmentVerifier::Mta());
    HRESULT hr = git.Lock();

    return SUCCEEDED(hr);
}

// Exported call to unlock WISP objects stored in the GIT
extern "C" BOOL WINAPI UnlockWispObjectFromGit(__in DWORD gitKey)
{
    GitComLockableWrapper<IUnknown> git(gitKey, ComApartmentVerifier::Mta());
    HRESULT hr = git.Unlock();

    return SUCCEEDED(hr);
}
