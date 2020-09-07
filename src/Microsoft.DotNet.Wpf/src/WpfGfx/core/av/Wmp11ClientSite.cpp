// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "Wmp11ClientSite.tmh"

MtDefine(Wmp11ClientSite, Mem, "Wmp11ClientSite");

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::Wmp11ClientSite
//
//  Synopsis: constructor
//
//  Returns: A new instance of Wmp11ClientSite
//
//------------------------------------------------------------------------------
Wmp11ClientSite::Wmp11ClientSite(
    __in    UINT                uiID
    ) : m_uiID(uiID)
{
    TRACEF(NULL);
    AddRef();
}

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::~Wmp11ClientSite
//
//  Synopsis: destructor
//
//------------------------------------------------------------------------------
Wmp11ClientSite::~Wmp11ClientSite()
{
    TRACEF(NULL);
}

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::Create
//
//  Synopsis: Public interface for creating a new instance of Wmp11ClientSite
//
//------------------------------------------------------------------------------
HRESULT
Wmp11ClientSite::Create(
    __in        UINT            uiID,
    __deref_out Wmp11ClientSite **ppSetup
    )
{
    HRESULT hr = S_OK;
    TRACEFID(uiID, &hr);

    Wmp11ClientSite* pSetup = new Wmp11ClientSite(uiID);
    IFCOOM(pSetup);

    //
    // Transfer reference
    //
    *ppSetup = pSetup;
    pSetup = NULL;

Cleanup:
    ReleaseInterface(pSetup);

    EXPECT_SUCCESSID(uiID, hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::QueryService, IServiceProvider
//
//  Synopsis: Acts as the factory method for any services exposed through an
//      implementation of IServiceProvider
//
//------------------------------------------------------------------------------
STDMETHODIMP
Wmp11ClientSite::QueryService(REFGUID guidService, REFIID riid, void ** ppv)
{
    return QueryInterface(riid, ppv);
}

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::HrFindInterface, CMILCOMBase
//
//  Synopsis: Get a pointer to another interface implemented by
//      Wmp11ClientSite
//
//------------------------------------------------------------------------------
STDMETHODIMP
Wmp11ClientSite::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;

    if (!ppvObject)
    {
        IFCN(E_INVALIDARG);
    }

    if (riid == IID_IServiceProvider)
    {
        *ppvObject = static_cast<IServiceProvider*>(this);
    }
    else if (riid == __uuidof(IWMPRemoteMediaServices))
    {
        *ppvObject = static_cast<IWMPRemoteMediaServices*>(this);
    }
    else if (riid == IID_IOleClientSite)
    {
        *ppvObject = static_cast<IOleClientSite*>(this);
    }
    else if (riid == IID_IWMPEvents)
    {
        IFCN(E_NOINTERFACE);
    }
    else if (riid == IID_IOleControlSite)
    {
        IFCN(E_NOINTERFACE);
    }
    else if (riid == IID_IDispatch)
    {
        IFCN(E_NOINTERFACE);
    }
    else if (riid == IID_IOleWindow)
    {
        IFCN(E_NOINTERFACE);
    }
    else if (riid == IID_IOleInPlaceSite)
    {
        IFCN(E_NOINTERFACE);
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_DEFAULT,
            "Unexpected interface request: %!IID!",
            &riid);

        IFCN(E_NOINTERFACE);
    }

Cleanup:
    RRETURN(hr);
}

STDMETHODIMP
Wmp11ClientSite::
GetServiceType(
    __out   BSTR    *pType
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(SysAllocStringCheck(L"NoDialogs", pType));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

STDMETHODIMP
Wmp11ClientSite::
GetScriptableObject(
    __out       BSTR        *pName,
    __deref_out IDispatch   **ppDispatch
    )
{
    RRETURN(E_NOTIMPL);
}

//
// IOleClientSite methods
//

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::GetContainer, IOleClientSite
//
//------------------------------------------------------------------------------
STDMETHODIMP
Wmp11ClientSite::GetContainer(
    LPOLECONTAINER FAR* ppContainer  // Address of output variable that
                                     // receives the IOleContainer
                                     // interface pointer
    )
{
    RRETURN(E_NOTIMPL);
}

//+-----------------------------------------------------------------------------
//
//  Member: Wmp11ClientSite::GetMoniker, IOleClientSite
//
//------------------------------------------------------------------------------
STDMETHODIMP
Wmp11ClientSite::GetMoniker(
    DWORD dwAssign,  // Value specifying how moniker is assigned
    DWORD dwWhichMoniker, // Value specifying which moniker is assigned
    IMoniker ** ppmk // Address of output variable that receives the
                     // IMoniker interface pointer
    )
{
    RRETURN(E_NOTIMPL);
}

