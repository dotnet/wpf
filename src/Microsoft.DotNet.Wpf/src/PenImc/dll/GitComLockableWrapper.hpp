// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

//------------------------------------------------------------------------------

#pragma once

#include "ComLockableWrapper.hpp"
#include "ComApartmentVerifier.hpp"

namespace ComUtils
{
    // DDVSO:514949
    // This class provides functionality used for various methods of
    // working around the COM rundown issues present in the OS (see OSGVSO:10779198).
    // The purpose is to obtain an object from the GIT and then use ComLockableWrapper
    // to ensure the object obtained survives rundown.
    // NOTE:
    //      Unlocking makes this object invalid.The GIT pointer is set to nullptr.
    //      Using this after unlocking will not succeed.
    template<typename T>
    class GitComLockableWrapper
    {
    public:

        // Default constructor, wraps a nullptr
        GitComLockableWrapper();

        // COM object constructor.
        // Inserts the COM object into the GIT and stores the key.
        GitComLockableWrapper(CComPtr<T> obj, ComApartmentVerifier expectedApartment);

        // GIT key constructor.
        // Stores the key for later use in other operations.
        GitComLockableWrapper(DWORD gitKey, ComApartmentVerifier expectedApartment);

        // Attempts to Lock the object by querying it from the GIT
        // and then using ComLockableWrapper.
        // Apartment is verified during this call.
        HRESULT Lock();

        // Attempts to Unlock the object by querying it from the GIT
        // and then using ComLockableWrapper.
        // Apartment is verified during this call.
        HRESULT Unlock();

        // Returns the GIT cookie that refers to this wrapped object in the GIT.
        DWORD GetCookie() const { return m_gitKey; }

        // Retrieves the wrapped object from the GIT.
        CComPtr<T> GetComObject();

        // Revokes the wrapped object from the GIT if the cookie is valid.
        // Otherwise this is a no_op.
        void RevokeIfValid();

        // Checks the validity of the GIT Cookie.
        HRESULT CheckCookie() { return (m_gitKey != 0) ? S_OK : E_FAIL; };

    private:

        DWORD m_gitKey;
        ComApartmentVerifier m_expectedApartment;
    };

    template<typename T>
    GitComLockableWrapper<T>::GitComLockableWrapper()
        : m_gitKey(0),
        m_expectedApartment(ComApartmentVerifier())
    {
    }

    template<typename T>
    GitComLockableWrapper<T>::GitComLockableWrapper(CComPtr<T> obj, ComApartmentVerifier expectedApartment)
        : m_expectedApartment(expectedApartment)
    {
        CComGITPtr<T> git(obj);
        m_gitKey = git.Detach();
    }

    template<typename T>
    GitComLockableWrapper<T>::GitComLockableWrapper(DWORD gitKey, ComApartmentVerifier expectedApartment)
        : m_gitKey(gitKey),
        m_expectedApartment(expectedApartment)
    {
    }

    template<typename T>
    CComPtr<T> GitComLockableWrapper<T>::GetComObject()
    {
        CComPtr<T> result = nullptr;

        if (m_gitKey != 0)
        {
            T *instance = nullptr;

            CComGITPtr<T> git(m_gitKey);
            HRESULT hr = git.CopyTo(&instance);
            git.Detach();

            if (SUCCEEDED(hr))
            {
                result = instance;
                instance->Release();
            }
        }

        return result;
    }

    template<typename T>
    HRESULT GitComLockableWrapper<T>::Lock()
    {
        HRESULT hr = E_FAIL;

        CComPtr<T> obj = GetComObject();

        if (obj != nullptr)
        {
            ComLockableWrapper wrapper(obj, m_expectedApartment);
            hr = wrapper.Lock();
        }

        return hr;
    }

    template<typename T>
    HRESULT GitComLockableWrapper<T>::Unlock()
    {
        HRESULT hr = E_FAIL;

        CComPtr<T> obj = GetComObject();

        if (obj != nullptr)
        {
            ComLockableWrapper wrapper(obj, m_expectedApartment);
            hr = wrapper.Unlock();
        }

        return hr;
    }

    template<typename T>
    void GitComLockableWrapper<T>::RevokeIfValid()
    {
        if (SUCCEEDED(CheckCookie()))
        {
            CComGITPtr<T> git(m_gitKey);
            git.Revoke();
        }
    }
}


