// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

//------------------------------------------------------------------------------

#include "stdafx.h"

#include "ComApartmentVerifier.hpp"
#include "ComLockableWrapper.hpp"

using namespace ComUtils;

ComLockableWrapper::ComLockableWrapper()
    : m_serverObject(nullptr),
    m_expectedApartment(ComApartmentVerifier())

{
}

ComLockableWrapper::ComLockableWrapper(IUnknown *obj, ComApartmentVerifier expectedApartment)
    : m_serverObject(obj),
    m_expectedApartment(expectedApartment)
{
}

HRESULT ComLockableWrapper::Lock()
{
    HRESULT hr = m_expectedApartment.VerifyCurrentApartmentType();

    if (SUCCEEDED(hr))
    {
        hr = E_ILLEGAL_METHOD_CALL;

        if (m_serverObject != nullptr)
        {
            IUnknown *unk = nullptr;
            hr = m_serverObject->QueryInterface(IID_IUnknown, reinterpret_cast<void**>(&unk));

            if (SUCCEEDED(hr))
            {
                hr = CoLockObjectExternal(unk,
                    true,    // fLock
                    false);  // fLastUnlockReleases - unused

                unk->Release();
            }
        }
    }

    return hr;
}

HRESULT ComLockableWrapper::Unlock()
{
    HRESULT hr = m_expectedApartment.VerifyCurrentApartmentType();

    if (SUCCEEDED(hr))
    {
        hr = E_ILLEGAL_METHOD_CALL;

        if (m_serverObject != nullptr)
        {
            IUnknown *unk = nullptr;
            hr = m_serverObject->QueryInterface(IID_IUnknown, reinterpret_cast<void**>(&unk));

            if (SUCCEEDED(hr))
            {
                hr = CoLockObjectExternal(unk,
                    false,  // fLock
                    true);  // fLastUnlockReleases

                // The QI AddRefs, so balance it
                unk->Release();

                // This lock is one shot, do not allow further operations.
                m_serverObject = nullptr;
            }
        }
    }

    return hr;
}

