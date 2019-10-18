// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  $TAG ENGR

//      $Module:    win_mil_graphics_media
//      $Keywords:
//
//  $Description:
//      Provide implementation of the activation object. This object provides
//      the EVR presenter instance to the
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "activate.tmh"

MtDefine(MFActivate, Mem, "MFActivate");


MFLockWrapper::
MFLockWrapper(
    void
    ) : m_hr(E_FAIL)
{
    //
    // CMFAttributesImpl actually calls into us during construction time, so, we
    // need to have the lock initialized before then.
    //

    m_hr = CCriticalSection::Init();
}

MFLockWrapper::
~MFLockWrapper(
    void
    )
{
}

HRESULT
MFLockWrapper::
Init(
    void
    ) const
{
    return m_hr;
}

void
MFLockWrapper::
Lock(
    void
    )
{
    //
    // If we are only being called during construction,
    // the lock is irrelevant and we will fail the call anyway when our
    // real Init call is made.
    //
    if (SUCCEEDED(m_hr))
    {
        Enter();
    }
}

void
MFLockWrapper::
Unlock(
    void
    )
{
    if (SUCCEEDED(m_hr))
    {
        Leave();
    }
}

//
// MFActivate implementation
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      MFActivate::Create
//
//  Synopsis:
//      Creates a new IMFActivate object that is used to instantiate our
//      EvrPresenter.
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
MFActivate::
Create(
    __in        UINT                uiID,
    __in        CWmpStateEngine     *pWmpStateEngine,
    __deref_out MFActivateObj       **ppMFActivateObj
    )
{
    HRESULT     hr = S_OK;

    TRACEFID(uiID, &hr);

    MFActivateObj       *pMFActivate = NULL;

    pMFActivate = new MFActivateObj(uiID, pWmpStateEngine);

    IFCOOM(pMFActivate);

    IFC(pMFActivate->Init());

    *ppMFActivateObj = pMFActivate;
    pMFActivate = NULL;

Cleanup:

    ReleaseInterface(pMFActivate);

    EXPECT_SUCCESSID(uiID, hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      MFActivate::DetachObject,   IMFActivate
//
//  Synopsis:
//      Creates a new IMFActivate object that is used to instantiate our
//      EvrPresenter.
//
//------------------------------------------------------------------------------
STDMETHODIMP
MFActivate::
DetachObject(
    )
{
    TRACEF(NULL);

    EvrPresenterObj *pEvrPresenter = NULL;

    {
        CGuard<CCriticalSection>     guard(m_Lock);

        pEvrPresenter = m_pEvrPresenter;
        m_pEvrPresenter = NULL;
    }

    ReleaseInterface(pEvrPresenter);

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      MFActivate::ShutdownObject,   IMFActivate
//
//  Synopsis:
//      Called when we are finally shut down, release all of our interfaces.
//
//------------------------------------------------------------------------------
STDMETHODIMP
MFActivate::
ShutdownObject(
    )
{
    TRACEF(NULL);

    CWmpStateEngine     *pWmpStateEngine = NULL;
    EvrPresenterObj     *pEvrPresenter   = NULL;

    {
        CGuard<CCriticalSection>     guard(m_Lock);

        pWmpStateEngine = m_pWmpStateEngine;
        m_pWmpStateEngine = NULL;

        pEvrPresenter = m_pEvrPresenter;
        m_pEvrPresenter = NULL;
    }

    ReleaseInterface(pWmpStateEngine);
    ReleaseInterface(pEvrPresenter);

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      MFActivate::ActivateObject,   IMFActivate
//
//  Synopsis:
//      Finally activate the object.
//
//------------------------------------------------------------------------------
STDMETHODIMP
MFActivate::
ActivateObject(
    __in        REFIID  riid,
        // The interface to the object being requested.
    __deref_out void    **ppv
        // The object.
    )
{
    HRESULT     hr = S_OK;

    EvrPresenterObj *pEvrPresenter = NULL;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection>    guard(m_Lock);

        if (NULL == m_pWmpStateEngine)
        {
            IFC(MF_E_SHUTDOWN);
        }

        if (NULL == m_pEvrPresenter)
        {
            IFC(m_pWmpStateEngine->NewPresenter(&m_pEvrPresenter));
        }

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    IFC(pEvrPresenter->QueryInterface(riid, ppv));

Cleanup:

    ReleaseInterface(pEvrPresenter);


    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//
// Protected methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      MFActivate::GetInterface
//
//  Synopsis:
//      Call by RealComObjects QueryInterface implementation to return our new
//      interface.
//
//------------------------------------------------------------------------------
void *
MFActivate::
GetInterface(
    __in    REFIID      riid
    )
{
    if (riid == __uuidof(IUnknown))
    {
        return static_cast<IUnknown *>(this);
    }
    else if (riid == __uuidof(IMFAttributes))
    {
        return static_cast<IMFAttributes *>(this);
    }
    else if (riid == __uuidof(IMFActivate))
    {
        return static_cast<IMFActivate *>(this);
    }

    return NULL;
}


MFActivate::
MFActivate(
    __in    UINT                uiID,
    __in    CWmpStateEngine     *pWmpStateEngine
    ) : m_uiID(uiID),
        m_pWmpStateEngine(NULL),
        m_pEvrPresenter(NULL)
{
    TRACEF(NULL);

    SetInterface(m_pWmpStateEngine, pWmpStateEngine);
}

MFActivate::
~MFActivate(
    void
    )
{
    TRACEF(NULL);

    ReleaseInterface(m_pWmpStateEngine);
    ReleaseInterface(m_pEvrPresenter);
}

//
// Private methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      MFActivate::Init        (private)
//
//  Synopsis:
//      Initializes the new activation object. In fact, we always create our
//      presenter up front and just return the instance we have when Activate
//      is called
//
//------------------------------------------------------------------------------
HRESULT
MFActivate::
Init(
    void
    )
{
    HRESULT     hr = S_OK;

    //
    // Lock has already been initialized, this checks if this failed.
    //
    IFC(m_Lock.Init());

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}


