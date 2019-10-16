// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

//------------------------------------------------------------------------------

#pragma once

#include <objidl.h>

namespace ComUtils
{
    // DDVSO:514949
    // This class provides functionality for checking and verifying apartment state.
    class ComApartmentVerifier
    {
    public:

        // Default constructor, sets an invalid state.
        ComApartmentVerifier();

        // Returns a verifier for MTA
        static ComApartmentVerifier Mta();

        // Returns a verifier for the current STA.
        // NOTE: This verifier includes the current thread id in verification.
        static ComApartmentVerifier CurrentSta();

        // Verifies the current apartment and, if applicable, thread id.
        HRESULT VerifyCurrentApartmentType();

    private:

        // Constructor for apartment type, free threaded.
        ComApartmentVerifier(APTTYPE);

        // Constructor for apartment type, specific thread.
        ComApartmentVerifier(APTTYPE, DWORD);

        // The COM apartment type to expect.
        APTTYPE m_expectedApartment;

        // If the apartment type is STA.
        bool m_expectedApartmentIsSta;

        // The id of the thread to expect.
        DWORD m_expectedStaThreadId;

        // Determines if this is in a valid (non-default constructed, appropriate arguments) state.
        bool m_valid;
    };
}

