// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "WmpClientSite.tmh"


MtDefine(CWmpClientSite, Mem, "CWmpClientSite");

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::CWmpClientSite
//
//  Synopsis: constructor
//
//  Returns: A new instance of CWmpClientSite
//
//------------------------------------------------------------------------------
CWmpClientSite::CWmpClientSite(
    __in    UINT                uiID,
    __in    CWmpStateEngine     *pPlayerState
    ) : m_uiID(uiID),
        m_pPresenter(NULL),
        m_pFilterGraph(NULL),
        m_pPlayerState(NULL),
        m_pCEvrFilterWrapper(NULL)
{
    AddRef();
    CD3DLoader::GetLoadRef();

    SetInterface(m_pPlayerState, pPlayerState);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::~CWmpClientSite
//
//  Synopsis: destructor
//
//------------------------------------------------------------------------------
CWmpClientSite::~CWmpClientSite()
{
    TRACEF(NULL);

    // These may be non-null if we're freed without playback starting
    ReleaseInterface(m_pPresenter);
    ReleaseInterface(m_pCEvrFilterWrapper);
    CD3DLoader::ReleaseLoadRef();

    ReleaseInterface(m_pPlayerState);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::Create
//
//  Synopsis: Public interface for creating a new instance of CWmpClientSite
//
//------------------------------------------------------------------------------
HRESULT
CWmpClientSite::Create(
    __in    UINT                uiID,
    __deref_out CWmpClientSite  **ppSetup,
    __in    CWmpStateEngine     *pPlayerState
    )
{
    HRESULT hr = S_OK;
    TRACEFID(uiID, &hr);
    CWmpClientSite* pSetup = new CWmpClientSite(uiID, pPlayerState);
    IFCOOM(pSetup);

    //
    // Transfer reference
    //
    *ppSetup = pSetup;
    pSetup = NULL;

Cleanup:

    ReleaseInterface(pSetup);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::QueryService, IServiceProvider
//
//  Synopsis: Acts as the factory method for any services exposed through an
//      implementation of IServiceProvider
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::QueryService(REFGUID guidService, REFIID riid, void ** ppv)
{
    return QueryInterface(riid, ppv);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::GetGraphCreationFlags, IWMPGraphCreation
//
//  Synopsis: Called by Windows Media Player to retrieve a value that represents
//      the graph creation preferences.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::GetGraphCreationFlags(DWORD*  pdwFlags)
{
    *pdwFlags |= WMPGC_FLAGS_SUPPRESS_DIALOGS;
    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::GraphCreationPostRender, IWMPGraphCreation
//
//  Synopsis: Called by Windows Media Player after the graph has been created.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::GraphCreationPostRender(IUnknown* pShouldBeAFilterGraph)
{
    HRESULT             hr = S_OK;
    IFilterGraph        *pGraph = NULL;
    IBaseFilter         *pIBaseFilter = NULL;
    IVideoWindow        *pVideoWindow = NULL;
    bool                fAudio = false;
    bool                fVideo = false;

    TRACEF(&hr);

    IFC(pShouldBeAFilterGraph->QueryInterface(&pGraph));

    //
    // Double check that we aren't getting the post render call for another
    // pre-render. We always take the latest graph.
    //
    Assert(pGraph == m_pFilterGraph);

    IFC(HasMedia(pGraph, &fAudio, &fVideo));

    m_pPlayerState->SetHasAudio(fAudio);
    m_pPlayerState->SetHasVideo(fVideo);

    IFC(pShouldBeAFilterGraph->QueryInterface(&pVideoWindow));
    IGNORE_HR(pVideoWindow->put_AutoShow(OAFALSE)); 
    IGNORE_HR(pVideoWindow->put_Visible(OAFALSE));

    // Get rid of unneeded objects if there's no video
    if (!fVideo)
    {
        IFC(m_pCEvrFilterWrapper->QueryInterface(__uuidof(IBaseFilter), reinterpret_cast<void**>(&pIBaseFilter)));
        IFC(pGraph->RemoveFilter(pIBaseFilter));

        m_pPresenter->AvalonShutdown();
    }

    //
    // Stop intercepting calls to IMediaSeeking
    //
    m_pCEvrFilterWrapper->SwitchToInnerIMediaSeeking();

    ReleaseInterface(m_pCEvrFilterWrapper);
    ReleaseInterface(m_pPresenter);

Cleanup:

    ReleaseInterface(pGraph);
    ReleaseInterface(pVideoWindow);
    ReleaseInterface(pIBaseFilter);

    EXPECT_SUCCESS(hr);
    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::GraphCreationPreRender, IWMPGraphCreation
//
//  Synopsis: Called by Windows Media Player before the VMR has been inserted
//      in the graph.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::GraphCreationPreRender(IUnknown* pShouldBeAFilterGraph, IUnknown* pReserved)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IFilterGraph        *pGraph = NULL;
    CEvrFilterWrapper   *pCEvrFilterWrapper = NULL;
    IBaseFilter         *pBaseFilter = NULL;
    IMFVideoRenderer    *pMFVideoRenderer = NULL;
    EvrPresenterObj     *pVideoPresenter  = NULL;

    CHECKPTRARG(pShouldBeAFilterGraph);

    IFC( pShouldBeAFilterGraph->QueryInterface(&pGraph) );

    IFC(CEvrFilterWrapper::Create(m_uiID, &pCEvrFilterWrapper));

    IFC(pCEvrFilterWrapper->QueryInterface(__uuidof(IBaseFilter), reinterpret_cast<void**>(&pBaseFilter)));
    IFC(pBaseFilter->QueryInterface(&pMFVideoRenderer));

    IFC(m_pPlayerState->NewPresenter(&pVideoPresenter));

    IFC(pMFVideoRenderer->InitializeRenderer(NULL, pVideoPresenter));

    IFC(pGraph->AddFilter(pBaseFilter, L"Avalon EVR"));

    //
    // Save the filter graph we were working on. We just want to know
    // its address to see if the graph has changed under us.
    //
    m_pFilterGraph = pGraph;
    ReplaceInterface(m_pCEvrFilterWrapper, pCEvrFilterWrapper);
    ReplaceInterface(m_pPresenter, pVideoPresenter);

Cleanup:

    ReleaseInterface(pVideoPresenter);
    ReleaseInterface(pGraph);
    ReleaseInterface(pBaseFilter);
    ReleaseInterface(pMFVideoRenderer);
    ReleaseInterface(pCEvrFilterWrapper);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpClientSite::HasMedia(IFilterGraph *pGraph, bool *pfAudio, bool *pfVideo)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    IEnumFilters *pEnum = NULL;
    IBaseFilter *pFilter = NULL;
    ULONG cFetched = 0;
    Assert(pGraph);
    Assert(pfAudio);
    Assert(pfVideo);

    *pfAudio = false;
    *pfVideo = false;
    IFC(pGraph->EnumFilters(&pEnum));

    while((hr = pEnum->Next(1, &pFilter, &cFetched)) == S_OK)
    {
        hr = THR(FilterHasMedia(pFilter, pfAudio, pfVideo));

        if (FAILED(hr))
        {
            ReleaseInterface(pFilter);
            continue;
        }

        if (*pfAudio && *pfVideo)
            break;

        ReleaseInterface(pFilter);
    }

    hr = S_OK;

Cleanup:
    ReleaseInterface(pEnum);
    ReleaseInterface(pFilter);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpClientSite::FilterHasMedia(IBaseFilter *pFilter, bool *pfAudio, bool *pfVideo)
{
    HRESULT    hr = S_OK;
    TRACEF(&hr);
    IEnumPins  *pEnum = NULL;
    IPin       *pPin = NULL;
    Assert(pFilter);
    Assert(pfAudio);
    Assert(pfVideo);


    IFC(pFilter->EnumPins(&pEnum));
    while(pEnum->Next(1, &pPin, 0) == S_OK)
    {
        PIN_DIRECTION PinDirThis;
        hr = THR(pPin->QueryDirection(&PinDirThis));
        if (FAILED(hr))
        {
            ReleaseInterface(pPin);
            continue;
        }

        if (PINDIR_OUTPUT == PinDirThis)
        {
            hr = THR(PinHasMedia(pPin, pfAudio, pfVideo));

            if (FAILED(hr))
            {
                ReleaseInterface(pPin);
                continue;
            }

            if (*pfAudio && *pfVideo)
                break;
        }

        // Release the pin for the next time through the loop.
        ReleaseInterface(pPin);
    }

    hr = S_OK;

Cleanup:
    ReleaseInterface(pEnum);
    ReleaseInterface(pPin);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpClientSite::PinHasMedia(IPin *pPin, bool *pfAudio, bool *pfVideo)
{
    HRESULT          hr = S_OK;
    TRACEF(&hr);
    IEnumMediaTypes *pEnum = NULL;
    AM_MEDIA_TYPE   *pMediaType = NULL;
    Assert(pPin);
    Assert(pfAudio);
    Assert(pfVideo);


    IFC(pPin->EnumMediaTypes(&pEnum));
    while(pEnum->Next(1, &pMediaType, 0) == S_OK)
    {
        if (pMediaType->majortype == MEDIATYPE_AnalogAudio ||
            pMediaType->majortype == MEDIATYPE_Audio ||
            pMediaType->majortype == MEDIATYPE_Midi)
        {
            *pfAudio = true;
        }
        else if (pMediaType->majortype == MEDIATYPE_AnalogVideo ||
            pMediaType->majortype == MEDIATYPE_Video)
        {
            *pfVideo = true;
        }
        if (*pfVideo && *pfAudio)
        {
            break;
        }

        DeleteMediaType(pMediaType);
        pMediaType = NULL;
    }

    hr = S_OK;

Cleanup:
    ReleaseInterface(pEnum);
    DeleteMediaType(pMediaType);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);

}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::HrFindInterface, CMILCOMBase
//
//  Synopsis: Get a pointer to another interface implemented by
//      CWmpClientSite
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::HrFindInterface(
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
    else if (riid == __uuidof(IWMPGraphCreation))
    {
        *ppvObject = static_cast<IWMPGraphCreation*>(this);
    }
    else if (riid == __uuidof(IWMPRemoteMediaServices))
    {
        IFCN(E_NOINTERFACE);
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

//
// IOleClientSite methods
//

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::GetContainer, IOleClientSite
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::GetContainer(
    LPOLECONTAINER FAR* ppContainer  // Address of output variable that
                                     // receives the IOleContainer
                                     // interface pointer
    )
{
    RRETURN(E_NOTIMPL);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpClientSite::GetMoniker, IOleClientSite
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpClientSite::GetMoniker(
    DWORD dwAssign,  // Value specifying how moniker is assigned
    DWORD dwWhichMoniker, // Value specifying which moniker is assigned
    IMoniker ** ppmk // Address of output variable that receives the
                     // IMoniker interface pointer
    )
{
    RRETURN(E_NOTIMPL);
}


