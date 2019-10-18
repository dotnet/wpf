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
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "EvrFilterWrapper.tmh"

MtDefine(CEvrFilterWrapper, Mem, "CEvrFilterWrapper");

//
// Public methods
//

//+-----------------------------------------------------------------------------
//
//  Member:
//      CEvrFilterWrapper::Create
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CEvrFilterWrapper::
Create(
    __in        UINT                id,
        // Logical id that binds together a set of related objects (corresponds to 
        // one media clock)
    __deref_out CEvrFilterWrapper   **ppCEvrFilterWrapper
    )
{
    HRESULT             hr = S_OK;
    CEvrFilterWrapper   *pCEvrFilterWrapper = NULL;

    TRACEFID(id, &hr);

    pCEvrFilterWrapper = new CEvrFilterWrapper(id);

    IFCOOM(pCEvrFilterWrapper);

    IFC(pCEvrFilterWrapper->Init());

    *ppCEvrFilterWrapper = pCEvrFilterWrapper;
    pCEvrFilterWrapper = NULL;

Cleanup:
    ReleaseInterface(pCEvrFilterWrapper);

    EXPECT_SUCCESSID(id, hr);

    RRETURN(hr);
}


void
CEvrFilterWrapper::
SwitchToInnerIMediaSeeking(
    void
    )
{
    CGuard<CCriticalSection> guard(m_stateLock);
    m_useInnerIMediaSeeking = true;
}


//
// IUnknown implementation
//

ULONG
CEvrFilterWrapper::
AddRef(
    void
    )
{
    return InterlockedIncrement(&m_cRef);
}

ULONG
CEvrFilterWrapper::
Release(
    void
    )
{
    AssertConstMsgW(
        m_cRef != 0,
        L"Attempt to release an object with 0 references! Possible memory leak."
        );

    ULONG cRef = InterlockedDecrement(&m_cRef);

    if (0 == cRef)
    {
        delete this;
    }

    return cRef;
}

STDMETHODIMP
CEvrFilterWrapper::
QueryInterface(
    __in REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    __analysis_assume(SUCCEEDED(hr));

    if (IID_IUnknown == riid)
    {
        *ppvObject = static_cast<IUnknown*>(this);
        this->AddRef();
    }
    else if (__uuidof(IMediaSeeking) == riid)
    {
        Assert(m_pIMediaSeeking != NULL);
        *ppvObject = static_cast<IMediaSeeking*>(this);
        this->AddRef();
    }
    else
    {
        Assert(m_pINonDelegatingUnknown != NULL);
        IFCN(m_pINonDelegatingUnknown->QueryInterface(riid, ppvObject));
    }

Cleanup:
    RRETURN(hr);
}

//
// IMediaSeeking implementation
//

STDMETHODIMP
CEvrFilterWrapper::
GetCapabilities(
    __out   DWORD   *pCapabilities
    )
{
    HRESULT hr = S_OK;
    bool    useInnerIMediaSeeking = false;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_stateLock);
        useInnerIMediaSeeking = m_useInnerIMediaSeeking;
    }

    if (useInnerIMediaSeeking)
    {
        IFCN(m_pIMediaSeeking->GetCapabilities(pCapabilities));
    }
    else
    {
        *pCapabilities = gc_dwordAllFlags;
    }
Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
CheckCapabilities(
    __in    DWORD   *pCapabilities
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->CheckCapabilities(pCapabilities));
Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
SetTimeFormat(
    __in    const GUID  *pFormat
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->SetTimeFormat(pFormat));
Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetTimeFormat(
    __out   GUID    *pFormat
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetTimeFormat(pFormat));
Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
IsUsingTimeFormat(
    __in    const GUID  *pFormat
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->IsUsingTimeFormat(pFormat));
Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
IsFormatSupported(
    __in    const GUID  *pFormat
    )
{
    HRESULT hr = S_OK;
    bool    useInnerIMediaSeeking = false;
    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_stateLock);
        useInnerIMediaSeeking = m_useInnerIMediaSeeking;
    }

    if (useInnerIMediaSeeking)
    {
        IFCN(m_pIMediaSeeking->IsFormatSupported(pFormat));
    }
    else
    {
        if (TIME_FORMAT_MEDIA_TIME != *pFormat)
        {
            IFCN(E_NOTIMPL);
        }
    }

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
QueryPreferredFormat(
    __out   GUID    *pFormat
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->QueryPreferredFormat(pFormat));
Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
ConvertTimeFormat(
    __out       LONGLONG    *pTarget,
    __in_opt    const GUID  *pTargetFormat,
    __in        LONGLONG    Source,
    __in_opt    const GUID  *pSourceFormat
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->ConvertTimeFormat(pTarget, pTargetFormat, Source, pSourceFormat));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
SetPositions(
    __in_opt    LONGLONG    *pCurrent,
    __in        DWORD       CurrentFlags,
    __in_opt    LONGLONG    *pStop,
    __in        DWORD       StopFlags
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->SetPositions(pCurrent, CurrentFlags, pStop, StopFlags));

Cleanup:
    RRETURN(hr);
};


STDMETHODIMP
CEvrFilterWrapper::
GetPositions(
    __out_opt   LONGLONG    *pCurrent,
    __out_opt   LONGLONG    *pStop
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetPositions(pCurrent, pStop));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetCurrentPosition(
    __out   LONGLONG    *pCurrent
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetCurrentPosition(pCurrent));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetStopPosition(
    __out   LONGLONG    *pStop
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetStopPosition(pStop));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
SetRate(
    __in    double  dRate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->SetRate(dRate));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetRate(
    __out   double  *pdRate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetRate(pdRate));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetDuration(
    __out   LONGLONG    *pDuration
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetDuration(pDuration));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetAvailable(
    __out_opt   LONGLONG    *pEarliest,
    __out_opt   LONGLONG    *pLatest
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetAvailable(pEarliest, pLatest));

Cleanup:
    RRETURN(hr);
};

STDMETHODIMP
CEvrFilterWrapper::
GetPreroll(
    __out   LONGLONG    *pllPreroll
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFCN(m_pIMediaSeeking->GetPreroll(pllPreroll));

Cleanup:
    RRETURN(hr);
};



//
// Private methods
//
CEvrFilterWrapper::
CEvrFilterWrapper(
    __in        UINT                    uiID
    ) : m_uiID(uiID)
      , m_cRef(1)
      , m_pINonDelegatingUnknown(NULL)
      , m_pIMediaSeeking(NULL)
      , m_useInnerIMediaSeeking(false)
{
    TRACEF(NULL);
}

/*virtual*/
CEvrFilterWrapper::
~CEvrFilterWrapper(
    )
{
    TRACEF(NULL);
    ReleaseInterface(m_pINonDelegatingUnknown);

    //
    // We don't release m_pIMediaSeeking because that would actually decrement
    // the reference count on us.
    //
    m_pIMediaSeeking = NULL;
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CEvrFilterWrapper::Init
//
//  Synopsis:
//      Initialize any state that might fail.
//
//------------------------------------------------------------------------------
HRESULT
CEvrFilterWrapper::
Init(
    void
    )
{
    HRESULT     hr = S_OK;
    IUnknown    *pOuterIUnknown = NULL;
    IUnknown    *pInnerIUnknown = NULL;
    bool        isLoaded = false;
    TRACEF(&hr);

    IFC(m_stateLock.Init());

    IFC(this->QueryInterface(IID_IUnknown, reinterpret_cast<void**>(&pOuterIUnknown)));

    // This module reference (a single global reference) is released only when we're unloaded.
    IFC(CAVLoader::GlobalGetEVRLoadRef());

    IFC(CAVLoader::GetEVRLoadRefAndCreateEnhancedVideoRendererForDShow(pOuterIUnknown, &pInnerIUnknown));
    isLoaded = true;

    //
    // QI to make sure we get a pointer to the non-delegating unknown
    //
    IFC(pInnerIUnknown->QueryInterface(&m_pINonDelegatingUnknown));


    //
    // We hold onto a reference to IMediaSeeking to mimic previous behavior,
    // but we can't reference count it because it AddRef's on us. We release to
    // prevent a circular reference
    //
    IFC(m_pINonDelegatingUnknown->QueryInterface(__uuidof(IMediaSeeking), reinterpret_cast<void** >(&m_pIMediaSeeking)));
    m_pIMediaSeeking->Release();

Cleanup:

    ReleaseInterface(pOuterIUnknown);
    ReleaseInterface(pInnerIUnknown);

    if (isLoaded)
    {
        IGNORE_HR(CAVLoader::ReleaseEVRLoadRef());
    }

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

