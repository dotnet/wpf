// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

//------------------------------------------------------------------------------

#pragma once

#include "ComApartmentVerifier.hpp"

namespace ComUtils
{
    // DDVSO:514949
    // This class provides functionality used for various methods of
    // working around the COM rundown issues present in the OS (see OSGVSO:10779198).
    // The purpose is to call CoLockObjectExternal on a server object to ensure that
    // none of the COM hierarchy of the object is released during rundown.
    // NOTE:
    //      Unlocking makes this object invalid.  The server object is set to nullptr.
    //      Using this after unlocking will not succeed.
    class ComLockableWrapper
    {
    public:

        // Default constructor, wraps a nullptr
        ComLockableWrapper();

        // Wraps a pointer with specific apartment type
        // Requires manual locking/unlocking.
        ComLockableWrapper(IUnknown *obj, ComApartmentVerifier expectedApartment);

        // Attempts to lock the server object via CoLockObjectExternal.
        // The apartment is verified during this call.
        HRESULT Lock();

        // Attempts to unlock the server object via CoLockObjectExternal.
        // The apartment is verified during this call.
        HRESULT Unlock();

    private:

        IUnknown *m_serverObject;
        ComApartmentVerifier m_expectedApartment;
    };
}

