// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

//------------------------------------------------------------------------------

#include "stdafx.h"

#include "ComApartmentVerifier.hpp"

using namespace ComUtils;

ComApartmentVerifier::ComApartmentVerifier()
    : m_valid(false)
{
}

ComApartmentVerifier ComApartmentVerifier::Mta()
{
    // MTA is free threaded.
    return ComApartmentVerifier(APTTYPE::APTTYPE_MTA);
}

ComApartmentVerifier ComApartmentVerifier::CurrentSta()
{
    APTTYPE aptType;
    APTTYPEQUALIFIER aptQualifier;

    HRESULT hr = CoGetApartmentType(&aptType, &aptQualifier);

    if (hr != S_OK || (aptType != APTTYPE::APTTYPE_STA && aptType != APTTYPE::APTTYPE_MAINSTA))
    {
        return ComApartmentVerifier();
    }

    // For STA, ensure we use the current thread and that the current apartment type matches.
    // This is so we capture the COM thread/apartment state at creation to use later.
    return ComApartmentVerifier(aptType, ::GetCurrentThreadId());
}

HRESULT ComApartmentVerifier::VerifyCurrentApartmentType()
{
    HRESULT hr = RPC_E_WRONG_THREAD;

    if (m_valid)
    {
        APTTYPE aptType;
        APTTYPEQUALIFIER aptQualifier;

        HRESULT aptHr = CoGetApartmentType(&aptType, &aptQualifier);

        if (aptHr == S_OK 
            && aptType == m_expectedApartment
            && (!m_expectedApartmentIsSta || m_expectedStaThreadId == ::GetCurrentThreadId()))
        {
            hr = S_OK;
        }
    }

    return hr;
}


ComApartmentVerifier::ComApartmentVerifier(APTTYPE aptType) :
    m_expectedApartment(aptType),
    m_expectedApartmentIsSta(false),
    m_expectedStaThreadId(0),
    m_valid(m_expectedApartment != APTTYPE::APTTYPE_STA && m_expectedApartment != APTTYPE::APTTYPE_MAINSTA)
{
    // We only verify STA/MAINSTA via a thread id constructor.
}

ComApartmentVerifier::ComApartmentVerifier(APTTYPE aptType, DWORD threadId) :
    m_expectedApartment(aptType),
    m_expectedApartmentIsSta(true),
    m_expectedStaThreadId(threadId),
    m_valid(m_expectedApartment == APTTYPE::APTTYPE_STA || m_expectedApartment == APTTYPE::APTTYPE_MAINSTA)
{
    // Don't use a thread id constructor for any other apartment types but STA/MAINSTA.
}


