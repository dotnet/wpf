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
#include "EvrPresenter.tmh"

MtDefine(EvrPresenter, Mem, "EvrPresenter");

/*static*/ const float EvrPresenter::msc_defaultMaxRate = 60;
/*static*/ const float EvrPresenter::msc_maxThinningRate = 10000.0;

//
// These formats are listed in our order of preference.
//
/*static*/ const D3DFORMAT EvrPresenter::msc_d3dFormatOrder[] =
{
    D3DFMT_X8R8G8B8,
    D3DFMT_A8R8G8B8
};


// +---------------------------------------------------------------------------
//
// EvrPresenter::Create
//
// +---------------------------------------------------------------------------
/*static*/
HRESULT
EvrPresenter::Create(
    __in    MediaInstance           *pMediaInstance,
    __in    UINT                    resetToken,
    __in    CWmpStateEngine         *pWmpStateEngine,
    __in    CDXVAManagerWrapper     *pDXVAManagerWrapper,
    __deref_out EvrPresenterObj     **ppEvrPresenter
    )
{
    HRESULT hr = S_OK;

    TRACEFID(pMediaInstance->GetID(), &hr);

    EvrPresenterObj *pEvrPresenter
        = new EvrPresenterObj(
                    pMediaInstance,
                    resetToken,
                    pWmpStateEngine,
                    pDXVAManagerWrapper
                    );

    IFCOOM(pEvrPresenter);

    //
    // Initialize the presenter.
    //
    IFC(pEvrPresenter->Init());

    // Transfer reference
    *ppEvrPresenter = pEvrPresenter;
    pEvrPresenter = NULL;

Cleanup:

    if (pEvrPresenter)
    {
        pEvrPresenter->AvalonShutdown();
        ReleaseInterface(pEvrPresenter);
    }

    EXPECT_SUCCESSID(pMediaInstance->GetID(), hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::Init
//
//  Synopsis:
//      This method is required to be present by the AvClassFactory template. We
//      perform whatever initialization we can do without the initialization
//      params
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::Init()
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    IFC(m_csEntry.Init());

    IFC(m_timerWrapper.Init(m_uiID, this, &EvrPresenter::TimeCallback));

    IFC(m_sampleScheduler.Init());

    IFC(m_surfaceRenderer.Init(this, &m_timerWrapper));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ProcessMessage,    IMFVideoPresenter
//
//  Synopsis:
//      This is the main routine that the EVR uses to notify us of changes to
//      how we should be handling media.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
ProcessMessage(
    MFVP_MESSAGE_TYPE   eMessage,
        // The type of message we are being supplied with
    ULONG_PTR           ulParam
        // The paramter for the type of message.
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    switch (eMessage)
    {
        case MFVP_MESSAGE_FLUSH:
            IFC(Flush());
            break;

        case MFVP_MESSAGE_INVALIDATEMEDIATYPE:
            IFC(ProcessInvalidateMediaType());
            break;

        case MFVP_MESSAGE_PROCESSINPUTNOTIFY:
            IFC(ProcessInputNotify());
            break;

        case MFVP_MESSAGE_BEGINSTREAMING:
            IFC(BeginStreaming());
            break;

        case MFVP_MESSAGE_ENDSTREAMING:
            IFC(EndStreaming());
            break;

        case MFVP_MESSAGE_ENDOFSTREAM:
            IFC(EndOfStream());
            break;

        case MFVP_MESSAGE_STEP:
            IFC(Step(LODWORD(ulParam)));
            break;

        case MFVP_MESSAGE_CANCELSTEP:
            IFC(CancelStep());
            break;

        default:
        {
            RIP("Unexpected");
            IFC(MF_E_INVALIDREQUEST);
        }
    }
Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetCurrentMediaType, IMFVideoPresenter
//
//  Synopsis:
//      Returns the current media type that we are using to the EVR.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetCurrentMediaType(
    __deref_out IMFVideoMediaType   **ppIMediaType
        // The media type
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CHECKPTRARG(ppIMediaType);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        SetInterface(*ppIMediaType, m_pIVideoMediaType);
    }

Cleanup:

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetDeviceID, IMFVideoDeviceID
//
//  Synopsis:
//      Returns the current media type that we are using to the EVR.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetDeviceID(
    __out_ecount(1) IID* pDeviceID
    )
{
    HRESULT hr = S_OK;

    CHECKPTRARG(pDeviceID);

    *pDeviceID = IID_IDirect3DDevice9;

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::InitServicePointers, IMFTopologyServiceLookupClient
//
//  Synopsis:
//      Called by the EVR to supply us with various service pointers we might
//      need (including the upstream mixer and the clock).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
InitServicePointers(
    __in    IMFTopologyServiceLookup    *pILookup
        //  The interface that allows us to retrieve our data from the topology.
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    DWORD               dwObjectCount       = 1;
    IMFTransform        *pIMixer            = NULL;
    IMediaEventSink     *pIMediaEventSink   = NULL;
    IMFClock            *pIMFClock          = NULL;
    IMFTimer            *pIMFTimer          = NULL;

    CHECKPTRARG(pILookup);

    //
    // Take the general lock for these calls.
    //
    {
        CGuard<CCriticalSection> guard(m_csEntry);

        //
        // We should only ever be asked to initialize our service pointers
        // when we are stopped, or we could be asked to do so if our service
        // pointers are released and then re-initialized.
        //
        if (m_renderState != RenderState::Stopped && m_renderState != RenderState::Shutdown)
        {
            RIP("Unexpected render state");
            IFC(E_UNEXPECTED);
        }

        // It's optional whether or not we get this.
        if (m_timerWrapper.GetUnderlyingClockNoAddRef() == NULL)
        {
            IGNORE_HR(
                pILookup->LookupService(
                    MF_SERVICE_LOOKUP_GLOBAL,
                    0,
                    MR_VIDEO_RENDER_SERVICE,
                    __uuidof(IMFClock),
                    reinterpret_cast<void **>(&pIMFClock),
                    &dwObjectCount));

            Assert(1 == dwObjectCount || 0 == dwObjectCount);

            if (pIMFClock)
            {
                m_timerWrapper.SetUnderlyingClock(pIMFClock);
            }

            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Clock is present? %d",
                pIMFClock != NULL);
        }

        if (m_timerWrapper.GetUnderlyingTimerNoAddRef() == NULL)
        {
            // It's optional whether or not we get this.
            IGNORE_HR(
                pILookup->LookupService(
                    MF_SERVICE_LOOKUP_GLOBAL,
                    0,
                    MR_VIDEO_RENDER_SERVICE,
                    __uuidof(IMFTimer),
                    (LPVOID*)&pIMFTimer,
                    &dwObjectCount));

            Assert(1 == dwObjectCount || 0 == dwObjectCount);

            if (pIMFTimer)
            {
                m_timerWrapper.SetUnderlyingTimer(pIMFTimer);
            }
        }

        if (m_pIMixer == NULL)
        {
            // this is mandatory.
            IFC(
                pILookup->LookupService(
                    MF_SERVICE_LOOKUP_UPSTREAM_DIRECT,
                    0,
                    MR_VIDEO_MIXER_SERVICE,
                    __uuidof(IMFTransform),
                    reinterpret_cast<void **>(&pIMixer),
                    &dwObjectCount));

            Assert(1 == dwObjectCount);

            IFC(ValidateMixerHasCorrectType(pIMixer));

            SetInterface(m_pIMixer, pIMixer);
        }

        if (m_pIMediaEventSink == NULL)
        {
            // this is mandatory.
            IFC(pILookup->LookupService(
                    MF_SERVICE_LOOKUP_UPSTREAM_DIRECT,
                    0,
                    MR_VIDEO_RENDER_SERVICE,
                    __uuidof(IMediaEventSink),
                    (LPVOID*)&pIMediaEventSink,
                    &dwObjectCount));

            ASSERT(1 == dwObjectCount);

            SetInterface(m_pIMediaEventSink, pIMediaEventSink);
        }

        m_renderState = RenderState::Stopped;

        if (m_pIMixer)
        {
            IGNORE_HR(InvalidateMediaType());
        }
    }

Cleanup:
    ReleaseInterface(pIMixer);
    ReleaseInterface(pIMediaEventSink);
    ReleaseInterface(pIMFClock);
    ReleaseInterface(pIMFTimer);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ReleaseServicePointers, IMFTopologyServiceLookupClient
//
//  Synopsis:
//      Called by the EVR to supply us with various service pointers we might
//      need (including the upstream mixer and the clock).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
ReleaseServicePointers(
    )
{
    TRACEF(NULL);

    IMFTransform        *pIReleaseMixer = NULL;
    IMFVideoMediaType   *pIReleaseMediaType = NULL;
    IMediaEventSink     *pIMediaEventSink = NULL;

    //
    // This is called from Shutdown, which can in turn be called before we are
    // initialized. In this case though, the shutdown will not be multithreaded.
    //
    if (m_csEntry.IsValid())
    {
        m_csEntry.Enter();
    }

    m_renderState = RenderState::Shutdown;

    pIReleaseMixer = m_pIMixer;
    m_pIMixer = NULL;

    pIReleaseMediaType = m_pIVideoMediaType;
    m_pIVideoMediaType = NULL;

    pIMediaEventSink = m_pIMediaEventSink;
    m_pIMediaEventSink = NULL;

    if (m_csEntry.IsValid())
    {
        m_csEntry.Leave();
    }

    m_timerWrapper.Shutdown();

    ReleaseInterface(pIReleaseMixer);
    ReleaseInterface(pIReleaseMediaType);
    ReleaseInterface(pIMediaEventSink);

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ClockStarted
//
//  Synopsis:
//      Called when the clock is started or restarted
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
ClockStarted(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    ProcessSamplesData  processSamplesData;

    m_timerWrapper.ClockStarted();
    IFC(m_sampleScheduler.ClockStarted());

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        if (m_timerWrapper.GetUnderlyingClockNoAddRef() == NULL)
        {
            LogAVDataM(
                AVTRACE_LEVEL_ERROR,
                AVCOMP_PRESENTER,
                "Starting without a clock");
        }

        m_renderState = RenderState::Started;

        IFC(ProcessSamples(&processSamplesData));
    }

    IFC(NotifyStateEngineOfState(RenderState::Started));

Cleanup:

    //
    // We want to kick of the next time to start rendering new samples or
    // display them to composition.
    //
    ProcessSampleDataOutsideOfLock(processSamplesData);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::OnClockStart, IMFClockStateSink
//
//  Synopsis:
//      Called when the clock is started.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
OnClockStart(
    MFTIME systemTime,
    MFTIME startOffset
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(ClockStarted());

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::OnClockStop, IMFClockStateSink
//
//  Synopsis:
//      Called when the clock is stopped.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
OnClockStop(
    MFTIME systemTime
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        m_renderState = RenderState::Stopped;
    }

    m_timerWrapper.ClockStopped();

    IFC(NotifyStateEngineOfState(RenderState::Stopped));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::OnClockPause, IMFClockStateSink
//
//  Synopsis:
//      Called when the clock is paused
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
OnClockPause(
    MFTIME SystemTime
    )
{
    HRESULT hr = S_OK;
    LONGLONG    pauseTime = 0LL;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        m_renderState = RenderState::Paused;

        IFC(m_timerWrapper.GetRenderTime(&pauseTime));
    }

    m_timerWrapper.ClockPaused();

    IFC(m_sampleScheduler.ClockPaused(pauseTime));

    IFC(NotifyStateEngineOfState(RenderState::Paused));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::OnClockRestart, IMFClockStateSink
//
//  Synopsis:
//      Called when the clock is restarted.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
OnClockRestart(
    MFTIME SystemTime
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(ClockStarted());

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::OnClockSetRate, IMFClockStateSink
//
//  Synopsis:
//      Called when the rate is set for the clock (currently only used as a hint
//      to pause).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
OnClockSetRate(
    MFTIME systemTime,
    float flRate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));
    }

    if (flRate == 0)
    {
        IFC(OnClockPause(systemTime));
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetSlowestRate,      IMFRateSupport
//
//  Synopsis:
//      Used to query the slowest frame rate that we can run at.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetSlowestRate(
    MFRATE_DIRECTION    direction,
    BOOL                fAllowThinning,
    __out               float *pflRate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CHECKPTRARG(pflRate);

    if (direction != MFRATE_FORWARD)
    {
        IFC(MF_E_REVERSE_UNSUPPORTED);
    }

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));
    }

    //
    // We can go as slow as you want.
    //
    *pflRate = 0.0;

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetFastestRate,      IMFRateSupport
//
//  Synopsis:
//      Used to query the fastest rate that we are able to exeecute. (Currently,
//      this is just set to 60, need to have feedback from composition here).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetFastestRate(
    MFRATE_DIRECTION    direction,
    BOOL                fAllowThinning,
    __out               float *pflRate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CHECKPTRARG(pflRate);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));
    }

    switch(direction)
    {
    case MFRATE_FORWARD:

        if (fAllowThinning)
        {
            //
            // If thinning is allowed
            // it seems like our highest rate is infinite
            // Return some big number
            //
            *pflRate = msc_maxThinningRate;
        }
        else
        {
            *pflRate = msc_defaultMaxRate;
        }
        break;

    case MFRATE_REVERSE:

        *pflRate = 0.0;
        break;

    default:

        IFC(E_INVALIDARG);
        break;
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::IsRateSupported,      IMFRateSupport
//
//  Synopsis:
//      Returns whether the given rate is supported and optionally also returns
//      the closest rate that we are actually able to support.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
IsRateSupported(
    BOOL    fAllowThinning,
    float   flRate,
    __out_opt float   *pflNearestRate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    float   flNearestRate = flRate;

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));
    }

    if (flRate < 0.0)
    {
        hr = THR(MF_E_REVERSE_UNSUPPORTED);

        //
        // The closest rate to backwards we can support is stationary.
        //
        flNearestRate = 0;
    }
    else if  ((flRate > msc_defaultMaxRate) && !fAllowThinning)
    {
        hr = THR(MF_E_UNSUPPORTED_RATE);

        flNearestRate = msc_defaultMaxRate;
    }

    if (pflNearestRate)
    {
        *pflNearestRate = flNearestRate;
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetService,          IMFGetService
//
//  Synopsis:
//      Called by the EVR and the Mixer to retrieve services (such as the DirectX
//      acceleration manager), that we might modify or write.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetService(
    __in        REFGUID guidService,
    __in        REFIID  riid,
    __deref_out LPVOID  *ppvObject
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CHECKPTRARG(ppvObject);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        Assert(m_pDXVAManagerWrapper != NULL); // should be initialized in Create

        if (guidService == MR_VIDEO_RENDER_SERVICE)
        {
            if (riid == __uuidof(IDirect3DDeviceManager9))
            {
                IFC(m_pDXVAManagerWrapper->QueryInterface(__uuidof(IDirect3DDeviceManager9), ppvObject));
            }
            else if (riid == __uuidof(IMFVideoDisplayControl))
            {
                *ppvObject = static_cast<IMFVideoDisplayControl *>(this);
                static_cast<IMFVideoDisplayControl *>(this)->AddRef();
            }
            else if (riid == __uuidof(IMediaEventSink))
            {
                IFC(E_NOINTERFACE);
            }
            else
            {
                LogAVDataM(
                    AVTRACE_LEVEL_ERROR,
                    AVCOMP_PRESENTER,
                    "Unknown service requested");

                IFC(E_NOINTERFACE);
            }
        }
        else if (guidService == MR_VIDEO_ACCELERATION_SERVICE)
        {
            if (riid == __uuidof(IDirect3DDeviceManager9))
            {
                IFC(m_pDXVAManagerWrapper->QueryInterface(__uuidof(IDirect3DDeviceManager9), ppvObject));
            }
            else
            {
                LogAVDataM(
                    AVTRACE_LEVEL_ERROR,
                    AVCOMP_PRESENTER,
                    "Unknown service requested");

                IFC(E_NOINTERFACE);
            }
        }
        else
        {
            RIP("Unexpected service request");
            IFC(E_NOINTERFACE);
        }
    }

Cleanup:

    //
    // Don't expect success because we know that requests for IMediaEventSink
    // will fail
    //
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetNativeVideoSize,          IMFVideoDisplayControl
//
//  Synopsis:
//      Returns the native video size of the playing media, also the aspect
//      ratio size (although we don't care about or preserve aspect ratio).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetNativeVideoSize(
    __inout_opt SIZE    *pszVideo,
        // The size of the video
    __inout_opt SIZE    *pszARVideo
        // The aspect ratio size of the video, not honored by this implementation.
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    //
    // Either of the parameters can be NULL, but not both.
    //
    if (NULL == pszARVideo && NULL == pszVideo)
    {
        IFC(E_INVALIDARG);
    }

    {
        CGuard<CCriticalSection>    guard(m_csEntry);

        IFC(CheckForShutdown(m_renderState));

        if (NULL == m_pIVideoMediaType)
        {
            SIZE    retSize = { 0, 0 };

            if (NULL != pszVideo)
            {
                *pszVideo = retSize;
            }

            if (NULL != pszARVideo)
            {
                *pszARVideo = retSize;
            }
        }
        else
        {
            SIZE retSize = { m_pIVideoMediaType->GetVideoFormat()->videoInfo.dwWidth,
                             m_pIVideoMediaType->GetVideoFormat()->videoInfo.dwHeight
                            };

            if (NULL != pszVideo)
            {
                *pszVideo = retSize;
            }

            if (NULL != pszARVideo)
            {
                pszARVideo->cx = retSize.cx * m_pIVideoMediaType->GetVideoFormat()->videoInfo.PixelAspectRatio.Numerator;
                pszARVideo->cy = retSize.cy * m_pIVideoMediaType->GetVideoFormat()->videoInfo.PixelAspectRatio.Denominator;
            }
        }
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::SetAspectRatioMode,          IMFVideoDisplayControl
//
//  Synopsis:
//      Set's the aspect ratio mode that the caller wants to preserve. This is
//      WMP, but, we don't really care what WMP wants.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
SetAspectRatioMode(
    __in        DWORD                dwAspectRatioMode
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    if ((dwAspectRatioMode & ~MFVideoARMode_Mask) != 0)
    {
        IFC(E_INVALIDARG);
    }

    {
        CGuard<CCriticalSection>    guard(m_csEntry);

        IFC(CheckForShutdown(m_renderState));

        //
        // Just store it, we ignore the requested mode. (WMP
        // sets this).
        //
        m_aspectRatioMode = dwAspectRatioMode;
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetAspectRatioMode,          IMFVideoDisplayControl
//
//  Synopsis:
//      Retrieves the aspect ratio mode. (Assuming someone wants it).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetAspectRatioMode(
    __out       DWORD                *pdwAspectRatioMode
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(pdwAspectRatioMode);

    {
        CGuard<CCriticalSection>    guard(m_csEntry);

        IFC(CheckForShutdown(m_renderState));

        *pdwAspectRatioMode = m_aspectRatioMode;
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::SetFullscreen,        IMFVideoDisplayControl
//
//  Synopsis:
//      Sets whether we should run fullscreen. We never run fullscreen.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
SetFullscreen(
    __in        BOOL                fFullscreen
    )
{
    HRESULT     hr = S_OK;

    if (fFullscreen)
    {
        IFC(E_INVALIDARG);
    }

Cleanup:

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetFullscreen,        IMFVideoDisplayControl
//
//  Synopsis:
//      Returns whether we are currently fullscreen. (We are never fullscreen).
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::
GetFullscreen(
    __out       BOOL                *pfFullscreen
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(pfFullscreen);

    *pfFullscreen = FALSE;

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//
// Public non-inteface methods (can't be exposed outside the DLL).
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetSurfaceRenderer
//
//  Synopsis:
//      Returns the surface renderer inside the presenter. These are now separate
//      objects (rather than the previous implementation where the interface was
//      part of the presenter).
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
GetSurfaceRenderer(
    __deref_out IAVSurfaceRenderer      **ppIAVSurfaceRenderer
    )
{
    *ppIAVSurfaceRenderer = &m_surfaceRenderer;
    m_surfaceRenderer.AddRef();

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::Width
//
//  Synopsis:
//      Returns the width of the media type. This is used when the WMP Ocx cannot
//      determine the width of the media for whatever reason.
//
//------------------------------------------------------------------------------
DWORD
EvrPresenter::
DisplayWidth(
    void
    )
{
    HRESULT     hr = S_OK;
    DWORD       width = 0;
    MFVideoInfo videoInfo = { 0 };

    {
        CGuard<CCriticalSection>    guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        if (m_pIVideoMediaType)
        {
            videoInfo = m_pIVideoMediaType->GetVideoFormat()->videoInfo;
            width = videoInfo.dwWidth;

            if (videoInfo.PixelAspectRatio.Numerator != 0 && videoInfo.PixelAspectRatio.Denominator != 0)
            {
                width = (width * videoInfo.PixelAspectRatio.Numerator) / videoInfo.PixelAspectRatio.Denominator;
            }
        }
    }

Cleanup:
    return width;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::Height
//
//  Synopsis:
//      Returns the height of the media from the media type.
//
//------------------------------------------------------------------------------
DWORD
EvrPresenter::
DisplayHeight(
    void
    )
{
    HRESULT hr = S_OK;
    DWORD   height = 0;

    {
        CGuard<CCriticalSection>    guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        if (m_pIVideoMediaType)
        {
            height = m_pIVideoMediaType->GetVideoFormat()->videoInfo.dwHeight;
        }
    }

Cleanup:
    return height;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AvalonShutdown
//
//  Synopsis:
//      Shuts down the Avalon-related functionalities of Evr Presenter (as
//      opposed to the EVR-related ones). This releases Avalon-related pointers
//      but holds onto EVR-related pointers. We need to keep processing samples
//      until the EVR tells us to shutdown or we may cause non-responsiveness in the EVR.
//
//------------------------------------------------------------------------------
void
EvrPresenter::
AvalonShutdown(
    void
    )
{
    TRACEF(NULL);

    //
    // We don't call ReleaseServicePointers because we need to continue processing
    // samples to avoid non-responsiveness.
    //

    m_surfaceRenderer.Shutdown();

    m_sampleScheduler.AvalonShutdown();

    CWmpStateEngine     *pReleasePlayerState = NULL;

    {
        if (m_csEntry.IsValid())
        {
            m_csEntry.Enter();
        }

        pReleasePlayerState = m_pWmpStateEngine;
        m_pWmpStateEngine= NULL;

        if (m_csEntry.IsValid())
        {
            m_csEntry.Leave();
        }
    }

    ReleaseInterface(pReleasePlayerState);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::SignalMixer
//
//  Synopsis:
//      Signals the mixer to run at a particular time.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
SignalMixer(
    __in    DWORD               continuityKey,
    __in    LONGLONG            timeToSignal
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    IFC(m_timerWrapper.SetTimer(continuityKey, timeToSignal));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::CancelTimer
//
//  Synopsis:
//      Called when we want to Cancel the existing timer.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
CancelTimer(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    hr = SignalMixer(0, gc_invalidTimerTime);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::NewMixerDevice
//
//  Synopsis:
//      Called out from the AV Surface Renderer when we are changing the video
//      rendering device (for example, we are falling back to software or
//      switching between monitors).
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
NewMixerDevice(
    __in    CD3DDeviceLevel1        *pRenderDevice,
        // The device on which we are going to render
    __in    CD3DDeviceLevel1        *pMixerDevice,
        // The mixer device
    __in    D3DDEVTYPE              devType
        // The new Direct3D device we want to use for video processing.
    )
{
    HRESULT                         hr = S_OK;
    DBG_CODE(D3DDEVICE_CREATION_PARAMETERS   dcp);
    IDirect3DDevice9       *pIMixerDevice = NULL;

    TRACEF(&hr);

    GetUnderlyingDevice(pMixerDevice, &pIMixerDevice);

    //
    // Media requires a multithreaded device
    //
    DBG_CODE(IFC(pIMixerDevice->GetCreationParameters(&dcp)));
    DBG_CODE(Assert((dcp.BehaviorFlags & D3DCREATE_MULTITHREADED) != 0));

    {
        CGuard<CCriticalSection>     guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        //
        // Need to do this while holding m_csEntry to prevent races between
        // GetMixSample and ReturnMixSample in ProcessOneSample.
        //
        IFC(m_sampleScheduler.InvalidateDevice(
                pRenderDevice,
                pMixerDevice,
                devType));

        hr = THR(m_pDXVAManagerWrapper->ResetDevice(pIMixerDevice, m_ResetToken));

        //
        // We consider any error returned from ResetDevice to be a hardware
        // error. If we don't recognize it, we convert it to
        // WGXERR_AV_UNKNOWNHARDWAREERROR
        //
        IFC(TreatNonSoftwareFallbackErrorAsUnknownHardwareError(hr));
    }

Cleanup:

    ReleaseInterface(pIMixerDevice);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::TimeCallback
//
//  Synopsis:
//      This is called when the current timer has ellapsed. First we need to
//      decide whether the time we are notified about indicates a new sample (in
//      which case we want to notify composition about this), then we want to
//      process some more samples and potentially signal another timer again.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
TimeCallback(
    __in    IMFAsyncResult          *pIAsyncResult
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    LONGLONG            currentTime       = 0LL;
    ProcessSamplesData  processSamplesData;

    IFC(m_timerWrapper.GetMixTime(&currentTime));

    m_sampleScheduler.NotifyCompositionIfNecessary(currentTime);

    {
        CGuard<CCriticalSection>    guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        //
        // We pass in the current time to avoid a race condition where
        // ProcessSamples calculates a new current time and skips over
        // a frame
        //
        IFC(ProcessSamples(&processSamplesData, currentTime));
    }

Cleanup:

    //
    // Signal ourselves that the time has elapsed.
    //
    ProcessSampleDataOutsideOfLock(processSamplesData);

    if (processSamplesData.nextTime == gc_invalidTimerTime)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Don't have a time to signal the mixer within the timer callback");
    }

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}


//
// Protected methods
//
// +---------------------------------------------------------------------------
//
// EvrPresenter::EvrPresenter
//
// +---------------------------------------------------------------------------
EvrPresenter::
EvrPresenter(
    __in    MediaInstance           *pMediaInstance,
    __in    UINT                    resetToken,
    __in    CWmpStateEngine         *pWmpStateEngine,
    __in    CDXVAManagerWrapper     *pDXVAManagerWrapper
    ) : m_uiID(pMediaInstance->GetID()),
        m_ResetToken(resetToken),
        m_pDXVAManagerWrapper(NULL),
        m_pIMediaEventSink(NULL),
        m_pIMixer(NULL),
        m_pIVideoMediaType(NULL),
        m_renderState(RenderState::Stopped),
        m_endStreaming(false),
        m_notifiedOfSample(false),
        m_aspectRatioMode(0),
        m_sampleScheduler(pMediaInstance, pWmpStateEngine),
        m_surfaceRenderer(pMediaInstance->GetID(), pWmpStateEngine),
        m_pMediaInstance(NULL),
        m_prevMixSampleTime(0LL),
        m_finalSampleTime(gc_invalidTimerTime),
        m_videoWindow(NULL)
{
    SetInterface(m_pWmpStateEngine, pWmpStateEngine);
    SetInterface(m_pDXVAManagerWrapper, pDXVAManagerWrapper);
    SetInterface(m_pMediaInstance, pMediaInstance);
    ZeroMemory(&m_nrcSource, sizeof(m_nrcSource));
    ZeroMemory(&m_rcDest, sizeof(m_rcDest));
}

// +---------------------------------------------------------------------------
//
// EvrPresenter::~EvrPresenter
//
// +---------------------------------------------------------------------------
EvrPresenter::
~EvrPresenter()
{
    TRACEF(NULL);

    AvalonShutdown();

    ReleaseInterface(m_pMediaInstance);
    ReleaseInterface(m_pDXVAManagerWrapper);
}

//+-----------------------------------------------------------------------------
//
// EvrPresenter::GetInterface
//
//------------------------------------------------------------------------------
void *
EvrPresenter::
GetInterface(
    __in    REFIID riid
    )
{
    TRACEF(NULL);

    if (riid == __uuidof(IUnknown))
    {
        return static_cast<IUnknown *>(static_cast<IMFVideoPresenter *>(this));
    }
    if (riid == __uuidof(IMFVideoPresenter))
    {
        return static_cast<IMFVideoPresenter *>(this);
    }
    else if (riid == __uuidof(IMFClockStateSink))
    {
        return static_cast<IMFClockStateSink *>(static_cast<IMFVideoPresenter *>(this));
    }
    else if (riid == __uuidof(IMFVideoDeviceID))
    {
        return static_cast<IMFVideoDeviceID *>(this);
    }
    else if (riid == __uuidof(IMFRateSupport))
    {
        return static_cast<IMFRateSupport *>(this);
    }
    else if (riid == __uuidof(IMFGetService))
    {
        return static_cast<IMFGetService*>(this);
    }
    else if (riid == __uuidof(IMFTopologyServiceLookupClient))
    {
        return static_cast<IMFTopologyServiceLookupClient*>(this);
    }
    else if (riid == __uuidof(IMFVideoDisplayControl))
    {
        return static_cast<IMFVideoDisplayControl *>(this);
    }
    else if (   riid == __uuidof(IMFMediaEventGenerator)
             || riid == __uuidof(IMediaEventSink))
    {
        return NULL;
    }

    LogAVDataM(
        AVTRACE_LEVEL_ERROR,
        AVCOMP_PRESENTER,
        "Unexpected interface request in EvrPresenter");

    return NULL;
}

//
// Private methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ProcessInvalidateMediaType
//
//  Synopsis:
//      Called by the EVR when our existing media type has been invalidated or
//      changed.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
ProcessInvalidateMediaType(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_INVALIDATEMEDIATYPE received");

    hr = InvalidateMediaType();

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::Flush
//
//  Synopsis:
//      Called when the evr wants us to flush any buffers. This doesn't mean
//      quite that we should discard all buffers, it is merely a (strong) hint
//      that we might want to make room in our sample queue (of course, we will
//      do this anyway when the real sample time elapses). From our perspective,
//      we do this anyway in the sample queue, so this is just a hint to process
//      more samples.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
Flush(
    void
    )
{
    HRESULT             hr = S_OK;
    ProcessSamplesData  processSamplesData;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_FLUSH received");

    {
        CGuard<CCriticalSection>    guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        IFC(ProcessSamples(&processSamplesData));

        IFC(FlushSamples());
    }

Cleanup:
    ProcessSampleDataOutsideOfLock(processSamplesData);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ProcessInputNotify      (private)
//
//  Synopsis:
//      Called by the mixer when there are samples for us to process, this
//      doesn't necessarily mean that we have the buffering capacity to actually
//      do the processing now, in that case, we will process samples later when
//      the sample time elapses.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
ProcessInputNotify(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    ProcessSamplesData      processSamplesData;

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_PROCESSINPUTNOTIFY received");

    {
        CGuard<CCriticalSection>    guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        if (m_pIVideoMediaType == NULL)
        {
            IFC(MF_E_TRANSFORM_TYPE_NOT_SET);
        }

        m_notifiedOfSample = true;

        IFC(ProcessSamples(&processSamplesData));
    }

Cleanup:

    ProcessSampleDataOutsideOfLock(processSamplesData);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::InvalidateMediaType      (private)
//
//  Synopsis:
//      Called when the media type of the stream has changed, this can also be
//      when we are informed of a new mixer through InitServicePointers. We use
//      this to get a new media type and set it.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
InvalidateMediaType(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IMFMediaType        *pIBestMediaType    = NULL;

    {
        CGuard<CCriticalSection>    guard(m_csEntry);

        IFC(CheckForShutdown(m_renderState));

        if (NULL == m_pIMixer)
        {
            IFC(MF_E_INVALIDREQUEST);
        }

        IFCN(GetBestMediaType(&pIBestMediaType));

        IFC(SetMediaType(pIBestMediaType));

        IFC(m_pIMixer->SetOutputType(0, pIBestMediaType, 0));
    }

Cleanup:

    ReleaseInterface(pIBestMediaType);

    //
    // This is a common return, don't log if we hit it.
    //
    if (hr != MF_E_TRANSFORM_TYPE_NOT_SET)
    {
        EXPECT_SUCCESS(hr);
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::GetBestMediaType           (private)
//
//  Synopsis:
//      Asks the mixer for the set of media types it supports and then returns
//      the one that is best for us.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
GetBestMediaType(
    __out   IMFMediaType    **ppIBestMediaType
    )
{
    HRESULT             hr = S_OK;

    TRACEF(&hr);

    IMFVideoMediaType   *pIVideoType        = NULL;
    IMFMediaType        *pIMediaType        = NULL;
    UINT                bestFormatIndex     = UINT_MAX;
    IMFMediaType        *pIBestMediaType    = NULL;

    for (UINT i = 0;; ++i, SmartRelease(&pIVideoType), SmartRelease(&pIMediaType))
    {
        hr = m_pIMixer->GetOutputAvailableType(0, i, &pIMediaType);

        if (FAILED(hr))
        {
            break;
        }

        IFC(pIMediaType->QueryInterface(__uuidof(IMFVideoMediaType), reinterpret_cast<void **>(&pIVideoType)));

        D3DFORMAT format = FormatFromMediaType(pIVideoType);

        //
        // Now, look through our list of media-types to see if it is there.
        //
        for(UINT thisFormatIndex = 0; thisFormatIndex < COUNTOF(msc_d3dFormatOrder); thisFormatIndex++)
        {
            if (msc_d3dFormatOrder[thisFormatIndex] == format)
            {
                //
                // The formats are listed in order of preference.
                //
                if (thisFormatIndex < bestFormatIndex)
                {
                    bestFormatIndex = thisFormatIndex;
                    ReplaceInterface(pIBestMediaType, pIMediaType);
                }

                break;
            }
        }
    }

    //
    // We expect to terminate with MF_E_NO_MORE_TYPES.
    //
    if (hr == MF_E_NO_MORE_TYPES)
    {
        hr = S_OK;
    }

    IFCN(hr);

    //
    // If none of the media types matched, then we fail too.
    //
    if (NULL == pIBestMediaType)
    {
        IFC(MF_E_INVALIDMEDIATYPE);
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "D3DFORMAT: A8R8G8B8? %!bool!  X8R8G8B8? %!bool!",
        msc_d3dFormatOrder[bestFormatIndex] == D3DFMT_A8R8G8B8,
        msc_d3dFormatOrder[bestFormatIndex] == D3DFMT_X8R8G8B8);

    *ppIBestMediaType = pIBestMediaType;
    pIBestMediaType = NULL;

Cleanup:

    ReleaseInterface(pIVideoType);
    ReleaseInterface(pIMediaType);
    ReleaseInterface(pIBestMediaType);

    //
    // This is a common return, don't log if we hit it.
    //
    if (hr != MF_E_TRANSFORM_TYPE_NOT_SET)
    {
        EXPECT_SUCCESS(hr);
    }

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ProcessOneSample           (private)
//
//  Synopsis:
//      Processes get a single sample from the queue and request that the mixer
//      give us the data.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
ProcessOneSample(
    __in    LONGLONG    currentTime
    )
{
    HRESULT                 hr = S_OK;
    IMFSample               *pISample = NULL;
    DWORD                   dwStatus = 0;
    MFT_OUTPUT_DATA_BUFFER  dataBuffer = { 0 };
    LONGLONG                sampleTime = 0;

    TRACEF(&hr);

    IFCN(m_sampleScheduler.GetMixSample(currentTime, &pISample));

    dataBuffer.pSample = pISample;

    hr = m_pIMixer->ProcessOutput(0, 1, &dataBuffer, &dwStatus);

    //
    // The stream could change while we are busy processing the output.
    //
    if (MF_E_TRANSFORM_STREAM_CHANGE == hr)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Process output failed because of a stream change.");

        //
        // Automatically clear the media type in this case.
        //
        IGNORE_HR(SetMediaType(NULL));

        IFCN(hr);
    }
    else if (MF_E_TRANSFORM_NEED_MORE_INPUT == hr)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Process output failed it needs more input.");

        //
        // When we come to the end of the stream, we clear the fact
        // that we have been notified of a sample and signal that
        // the media is done.
        //
        m_notifiedOfSample = false;

        IFCN(hr);
    }
    //
    // Some fallback errors indicate that we should fallback to software even
    // if we encounter them, regardless if the sample succeeds.
    //
    else if (IsMandatorySoftwareFallbackError(hr))
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Process output has software fallback error.");

        IFCN(hr);
    }
    //
    // We handle E_INVALIDARG errors by ignoring the sample we got back and
    // continuing to process new samples. Not continuing can result in frozen video.
    //
    else if (E_INVALIDARG == hr)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Process output has E_INVALIDARG error");

        IFCN(hr);
    }
    else if (FAILED(hr))
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Process output has another failure %x.",
            hr);

        //
        // We consider errors that we don't recognize to be an unknown hardware
        // errors, which will trigger us to fallback to software.
        //
        IFCN(TreatNonSoftwareFallbackErrorAsUnknownHardwareError(hr));
    }
    else
    {
        IFC(pISample->GetSampleTime(&sampleTime));

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Process output succeeded. sampleTime: %I64d",
            sampleTime);

        if (sampleTime < 0)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Interpreting negative sampleTime: %I64d as 0",
                sampleTime);

            sampleTime = 0;
            IFC(pISample->SetSampleTime(0));
        }


        IFC(m_sampleScheduler.ReturnMixSample(currentTime));

        m_prevMixSampleTime = sampleTime;
    }


Cleanup:
    if (hr == E_INVALIDARG)
    {
        hr = S_OK;
    }

    ReleaseInterface(pISample);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ProcessSamples           (private)
//
//  Synopsis:
//      Processes a set of samples from the mixer. We can process more than one
//      sequentially so long as we are invalidating old samples
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
ProcessSamples(
    __inout       ProcessSamplesData  *pProcessSamplesData,
        // Return data for the samples that must be handled
    __in        LONGLONG            currentTime
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    //
    // If we don't have a mixer it is an invalid request to ask us to process a
    // sample.
    //
    if (NULL == m_pIMixer)
    {
        IFC(MF_E_INVALIDREQUEST);
    }

    //
    // We only get the currentTime if the caller didn't pass in a time
    //
    if (currentTime == gc_invalidTimerTime)
    {
        IFC(m_timerWrapper.GetMixTime(&currentTime, &(pProcessSamplesData->continuityKey)));
    }

    if (m_notifiedOfSample)
    {
        //
        // We consider all the time while processing the output to be the current time.
        // This is a simplification but at least guarantees that in the simplest case,
        // we will hit an upper bound of samples that the sample queue wants to accept.
        //
        while (hr == S_OK)
        {
            hr = ProcessOneSample(currentTime);
        }
    }

    if (m_endStreaming && !m_notifiedOfSample)
    {
        m_endStreaming = false;

        m_finalSampleTime = m_prevMixSampleTime;
    }

    //
    // We can't call MediaFinished before the currentTime has reached the last
    // sample time. If we did, the clock would never reach the last sample time.
    // This would cause us to either not show the last sample or skip the 2nd
    // last sample or two.
    //
    if (currentTime >= m_finalSampleTime)
    {
        m_finalSampleTime = gc_invalidTimerTime;
        pProcessSamplesData->mediaFinished = true;
    }

    m_sampleScheduler.NotifyCompositionIfNecessary(currentTime);

    pProcessSamplesData->nextTime = m_sampleScheduler.CalculateNextCallbackTime(currentTime);

Cleanup:

    //
    // We might swallow errors from ProcessSamples, but if the error indicates that we
    // should failback to software, we want to let composition know about it.
    //
    if (IsSoftwareFallbackError(hr))
    {
        pProcessSamplesData->fallbackFailure = hr;
    }

    hr = S_OK;

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ProcessSampleDataOutsideOfLock
//
//  Synopsis:
//      Gets the data returned from ProcessSamples and
//
//------------------------------------------------------------------------------
void
EvrPresenter::
ProcessSampleDataOutsideOfLock(
    __in        const ProcessSamplesData    &processSamplesData
    )
{
    TRACEF(NULL);

    if (IsSoftwareFallbackError(processSamplesData.fallbackFailure))
    {
        //
        // Tell the surface renderer about the failure.
        //
        m_surfaceRenderer.SignalFallbackFailure(processSamplesData.fallbackFailure);

        //
        // If we have a next sample time, then let this just come around
        // with the next sample, otherwise, tell composition to render a
        // new frame NOW so that the surface renderer will be guaranteed to
        // fallback to software.
        //
        if (processSamplesData.nextTime == gc_invalidTimerTime)
        {
            m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
        }
    }

    if (processSamplesData.nextTime != gc_invalidTimerTime)
    {
        //

        if (m_renderState != RenderState::Started)
        {
            m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
        }
        else
        {
            IGNORE_HR(SignalMixer(processSamplesData.continuityKey, processSamplesData.nextTime));
        }
    }

    if (processSamplesData.mediaFinished)
    {
        MediaFinished();
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::BeginStreaming
//
//  Synopsis:
//      Called when streaming begins. Currently we don't do anything here.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
BeginStreaming(
    void
    )
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_BEGINSTREAMING received");

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::BeginStreaming
//
//  Synopsis:
//      Called when streaming ends. Currently we don't do anything here.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
EndStreaming(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_ENDSTREAMING received");

    return hr;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::EndOfStream
//
//  Synopsis:
//      Called when the stream is at an end. We use this to drain the samples
//      from the sample queue. (We will be told about the samples with
//      oninputnotify.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
EndOfStream(
    void
    )
{
    HRESULT     hr = S_OK;
    bool        mediaFinished = false;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_ENDOFSTREAM received");

    //
    // We need to notify the IMediaEventSink that the stream is complete, but not
    // until we've actually presented the last frame.
    //
    {
        CGuard<CCriticalSection>    guard(m_csEntry);

        IFC(CheckForShutdown(m_renderState));

        //
        // If we have samples that need to be drained from the mixer, then set this
        // so that ProcessSamples will hit the end of the stream.
        //
        if (m_notifiedOfSample)
        {
            m_endStreaming = true;
        }
        //
        // Otherwise, just indicate the we have reached the end of the stream now.
        //
        else
        {
            LONGLONG    currentTime = 0LL;

            IFC(m_timerWrapper.GetMixTime(&currentTime));

            m_finalSampleTime = m_prevMixSampleTime;

            if (currentTime >= m_finalSampleTime)
            {
                m_finalSampleTime = gc_invalidTimerTime;
                mediaFinished = true;
            }
        }
    }

    if (mediaFinished)
    {
        MediaFinished();
    }

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::Step
//
//  Synopsis:
//      Called when we want to do stepping we are asked to step a certain number
//      of frames.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
Step(
    __in        DWORD               stepCount
        // The step count that is being requested.
    )
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_STEP received");

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "EvrPresenter::Step(%d)",
        stepCount);

    RIP("Step unexpected");

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::CancelStep
//
//  Synopsis:
//      Cancelled when stepping is cancelled.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
CancelStep(
    void
    )
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "MFVP_MESSAGE_CANCELSTEP received");

    RIP("CancelStep unexpected");

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::ValidateMixerHasCorrectType      (private)
//
//  Synopsis:
//      Validates whether the mixer has the type we expect (D3DDevice), we don't
//      support mixer's that use another device type. (Actually academic since
//      the EVR currently only supports D3D, but still a good check).
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
ValidateMixerHasCorrectType(
    __in        IMFTransform        *pIMixer
        // The mixer
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    IMFVideoDeviceID        *pIMFVideoDeviceId = NULL;
    IID                     deviceID = { 0 };

    IFC(
        pIMixer->QueryInterface(
            __uuidof(IMFVideoDeviceID),
            reinterpret_cast<void **>(&pIMFVideoDeviceId)));

    IFC(pIMFVideoDeviceId->GetDeviceID(&deviceID));

    if (deviceID != IID_IDirect3DDevice9)
    {
        IFC(E_INVALIDARG);
    }

Cleanup:

    ReleaseInterface(pIMFVideoDeviceId);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::MediaFinished        (private)
//
//  Synopsis:
//      Called when media comes to an end, we notify the player that media has
//      finished, this allows it to immediately pause us.
//
//------------------------------------------------------------------------------
void
EvrPresenter::
MediaFinished()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(NotifyEvent(EC_COMPLETE, S_OK, 0));

Cleanup:
    //
    // We swallow an error here. If we fail calling NotifyEvent then we
    // won't be able to call NotifyEvent to send an error message either.
    //
    EXPECT_SUCCESS(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::NotifyEvent          (private)
//
//  Synopsis:
//      Helper function to call stuff on the IMediaEventSink
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
NotifyEvent(
    long EventCode,
    __in_opt LONG_PTR EventParam1,
    __in_opt LONG_PTR EventParam2
    )
{
    HRESULT         hr = S_OK;
    IMediaEventSink *pIMediaEventSink = NULL;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "NotifyEvent(%d, %I64d, %I64d)",
        EventCode,
        EventParam1,
        EventParam2);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        SetInterface(pIMediaEventSink, m_pIMediaEventSink);
    }

    if (pIMediaEventSink)
    {
        IFC(pIMediaEventSink->Notify(EventCode, EventParam1, EventParam2));
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Attempting to call NotifyEvent, but no interface!");
    }

Cleanup:
    ReleaseInterface(pIMediaEventSink);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::NotifyStateEngineOfState (private)
//
//  Synopsis:
//      Helper function to call stuff on CWmpStateEngine
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
NotifyStateEngineOfState(
    RenderState::Enum   state
    )
{
    HRESULT         hr = S_OK;
    CWmpStateEngine *pCWmpStateEngine = NULL;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_csEntry);
        IFC(CheckForShutdown(m_renderState));

        SetInterface(pCWmpStateEngine, m_pWmpStateEngine);
    }

    if (pCWmpStateEngine)
    {
        IFC(WmpStateEngineProxy::AsyncCallMethod(
                m_uiID,
                pCWmpStateEngine,
                pCWmpStateEngine,
                &CWmpStateEngine::EvrReachedState,
                state
            ));
    }

Cleanup:
    ReleaseInterface(pCWmpStateEngine);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::FlushSamples
//
//  Synopsis:
//      Flushes all of the samples in the sample queue to be invalid.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
FlushSamples(
    void
    )
{
    HRESULT     hr = S_OK;
    LONGLONG    currentTime = 0LL;
    TRACEF(&hr);

    //
    // We call GetRenderTime so that the current time will default to
    // gc_invalidTimerTime and Flush will keep the latest sample if it's not
    // possible to get a valid time.
    //
    IFC(m_timerWrapper.GetRenderTime(&currentTime));

    //
    // flush can't fail.
    //
    m_sampleScheduler.Flush(currentTime);

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::SetMediaType     (private)
//
//  Synopsis:
//      Set the media type (can also set the media type to NULL).
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::
SetMediaType(
    __in_opt    IMFMediaType            *pIMediaType
        // The media type that we have decided to use (or NULL if we have no media).
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IMFVideoMediaType       *pIVideoMediaType = NULL;

    if (pIMediaType)
    {
        IFC(pIMediaType->QueryInterface(__uuidof(IMFVideoMediaType), reinterpret_cast<void **>(&pIVideoMediaType)));
    }

    ReplaceInterface(m_pIVideoMediaType, pIVideoMediaType);

    //
    // This changes the content rect size for the IAVSurfaceRenderer, so, let it know.
    //
    IFC(m_surfaceRenderer.ChangeMediaType(pIVideoMediaType));

    //
    // This also caused us to change the media type for the sample queue (used when
    // allocating samples).
    //
    IFC(m_sampleScheduler.ChangeMediaType(pIVideoMediaType));

Cleanup:

    ReleaseInterface(pIVideoMediaType);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// AVSurfaceRenderer implementation
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer     constructor
//
//  Synopsis:
//      The part of the EVR presenter that is responsible for talking to composition.
//      This is separated out as a separate class in order to make clear what code
//      paths need to either not use locks or use really low-contention locks.
//
//------------------------------------------------------------------------------
EvrPresenter::AVSurfaceRenderer::
AVSurfaceRenderer(
    __in    UINT                uiID,
    __in    CWmpStateEngine     *pWmpStateEngine
    ) : m_uiID(uiID),
        m_pEvrPresenter(NULL),
        m_pCurrentRenderDevice(NULL),
        m_pSoftwareDevice(NULL),
        m_pRenderedBuffer(NULL),
        m_pCompositionRenderDevice(NULL),
        m_haveMultipleCompositionDevices(false),
        m_isPaused(false),
        m_lastSampleTime(-1),
        m_fallbackFailure(S_OK),
        m_pDummySource(NULL),
        m_dwWidth(0),
        m_dwHeight(0),
        m_pWmpStateEngine(NULL),
        m_syncChannel(false),
        m_deviceContinuity(1LL),
        m_lastHardwareDeviceContinuity(0LL)
{
    TRACEF(NULL);

    SetInterface(m_pWmpStateEngine, pWmpStateEngine);
}

EvrPresenter::AVSurfaceRenderer::
~AVSurfaceRenderer(
    void
    )
{
    TRACEF(NULL);

    Shutdown();

    ReleaseInterface(m_pCurrentRenderDevice);
    ReleaseInterface(m_pSoftwareDevice);
    ReleaseInterface(m_pCompositionRenderDevice);
    ReleaseInterface(m_pRenderedBuffer);
    ReleaseInterface(m_pDummySource);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::Init
//
//  Synopsis:
//      Creates a new surface renderer. We also create the initial device and do
//      a software fallback if necessary.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::Init(
    __in    EvrPresenter        *pEvrPresenter,
    __in    RenderClock         *pRenderClock
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CD3DDeviceLevel1        *pRenderDevice = NULL;

    m_pEvrPresenter = pEvrPresenter;
    m_pRenderClock = pRenderClock;

    IFC(m_compositionLock.Init());

    IFC(m_mediaLock.Init());

    m_pDummySource = new CDummySource(0, 0, MilPixelFormat::BGR32bpp);

    IFCOOM(m_pDummySource);

    m_pDummySource->AddRef();

    Assert(m_pCurrentRenderDevice == NULL);

    THR(hr = GetHWDevice(msc_defaultAdapter, false, &pRenderDevice));

    if (SUCCEEDED(hr))
    {
        THR(hr = NewRenderDevice(pRenderDevice));
    }

    //
    // Check to see if we should fallback to software (either because
    // we couldn't create the device or because we could reset DXVA
    // with this new device).
    //
    IFC(FallbackToSoftwareIfNecessary(hr));

Cleanup:
    ReleaseInterface(pRenderDevice);

    RRETURN(hr);
}

//
// IUnknown
//
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
QueryInterface(
    __in        REFIID      riid,
    __deref_out void        **ppvObject
    )
{
    HRESULT     hr  = E_NOINTERFACE;

    TRACEF(&hr);

    void        *pv = NULL;

    if (riid == __uuidof(IUnknown))
    {
        pv = static_cast<IUnknown *>(this);
    }
    else if (riid == IID_IAVSurfaceRenderer)
    {
        pv = static_cast<IAVSurfaceRenderer *>(this);
    }

    if (pv)
    {
        if (ppvObject)
        {
            *ppvObject = pv;
            AVSurfaceRenderer::AddRef();
            hr = S_OK;
        }
        else
        {
            hr = E_INVALIDARG;
        }
    }

    RRETURN(hr);
}

STDMETHODIMP_(ULONG)
EvrPresenter::AVSurfaceRenderer::
AddRef(
    void
    )
{
    return static_cast<IMFVideoPresenter *>(m_pEvrPresenter)->AddRef();
}

STDMETHODIMP_(ULONG)
EvrPresenter::AVSurfaceRenderer::
Release(
    void
    )
{
    return static_cast<IMFVideoPresenter *>(m_pEvrPresenter)->Release();
}

//
// IAVSurfaceRenderer
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::BeginComposition, IAVSurfaceRender
//
//  Synopsis:
//      Called by composition when we need to render a frame. We find a frame
//      from the sample queue and return it.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
BeginComposition(
    __in    CMilSlaveVideo  *pCaller,
    __in    BOOL            displaySetChanged,
    __in    BOOL            syncChannel,
    __inout LONGLONG        *pLastCompositionSampleTime,
    __out   BOOL            *pIsNewFrameReady
    )
{
    HRESULT             hr                  = S_OK;
    LONGLONG            currentTime         = 0LL;
    HRESULT             mixerFallbackError  = S_OK;
    CDisplaySet const   *pDisplaySet  = NULL;

    TRACEF(&hr);

    CHECKPTRARG(pIsNewFrameReady);

    //
    // Record whether we are on a synchronous channel or not.
    //
    m_syncChannel = !!syncChannel;

    *pIsNewFrameReady = FALSE;

    {
        CGuard<CCriticalSection>    guard(m_compositionLock);

        IFC(AddCompositingResource(pCaller));

        DBG_CODE(DumpResourceList());

        //
        // Read and transfer any fallback failure from the mixer.
        //
        mixerFallbackError = m_fallbackFailure;
        m_fallbackFailure = S_OK;

        IFC(m_pRenderClock->GetRenderTime(&currentTime));
    }

    if (displaySetChanged)
    {
        m_deviceContinuity++;
    }

    //
    // Check if it means we need to go to software processing based on failures
    // in the mixer.
    //
    IFC(FallbackToSoftwareIfNecessary(mixerFallbackError));

    //
    // Snap a sample from the sample queue.
    // We might be being called because of a change in composition, not just
    // because we have a new frame, so, we always want to squirrel away a
    // frame, even if it has already been displayed.
    //
    ReleaseInterface(m_pRenderedBuffer);
    IFCN(m_pEvrPresenter->GetSampleScheduler().GetCompositionSample(
                currentTime,
                pLastCompositionSampleTime,
                &m_pRenderedBuffer,
                pIsNewFrameReady));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "BeginComposition(%p, %!bool!, %!bool!, *%!bool!)",
        pCaller,
        displaySetChanged,
        syncChannel,
        *pIsNewFrameReady);

Cleanup:

    ReleaseInterface(pDisplaySet);

    //
    // This can happen for a while while we haven't been given a mixer or haven't had
    // a new media type set for any other reasons.
    //
    if (IsTransientError(hr))
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Missed a frame in a BeginComposition pass with hr %x",
            hr);

        *pIsNewFrameReady = FALSE;
        hr = S_OK;
    }

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::BeginRender, IAVSurfaceRender
//
//  Synopsis:
//      Called by composition when we need to render a frame. We find a frame
//      from the sample queue and return it.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
BeginRender(
    __in_ecount_opt(1)          CD3DDeviceLevel1 *pDeviceLevel1,
        // The device to which we are rendering, NULL if we are rendering to software.
    __deref_opt_out_ecount(1)   IWGXBitmapSource **ppWGXBitmapSource
        // The bitmap source that holds the frame.
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(ppWGXBitmapSource);

    if (pDeviceLevel1)
    {
        if (m_pCompositionRenderDevice != pDeviceLevel1)
        {
            if (m_pCompositionRenderDevice)
            {
                m_haveMultipleCompositionDevices = true;
            }
            else
            {
                SetInterface(m_pCompositionRenderDevice, pDeviceLevel1);
            }
        }
    }

    if (m_pRenderedBuffer)
    {
        IFC(m_pRenderedBuffer->GetBitmapSource(
                m_syncChannel,
                pDeviceLevel1,
                ppWGXBitmapSource));
    }
    else
    {
        //
        // If we don't have a rendered buffer handy, we just return a dummy
        // source, this will always be the same size as any media we might have
        // set, but it will be black.
        //
        CGuard<CCriticalSection>    guard(m_compositionLock);

        *ppWGXBitmapSource = m_pDummySource;
        m_pDummySource->AddRef();
    }

Cleanup:

    //
    // See whether any of our errors indicate we should fall back to software.
    //
    hr = FallbackToSoftwareIfNecessary(hr);

    if (IsTransientError(hr))
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Missed a frame in a BeginRender pass with hr %x",
            hr);

        hr = S_OK;
    }

    if (hr != MF_E_SHUTDOWN)
    {
        EXPECT_SUCCESS(hr);
    }

    //
    // We don't want to make the composition engine non-responsive because we encountered an
    // error. If we did encounter an error, it will be logged.
    //
    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::EndRender, IAVSurfaceRender
//
//  Synopsis:
//      Called when composition has renderer our sample. We don't currently do
//      anything at the end of a render pass, we return the sample at the end of
//      the composition pass.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
EndRender(
    )
{
    TRACEF(NULL);

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::EndComposition, IAVSurfaceRender
//
//  Synopsis:
//      Called at the end of the composition pass.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
EndComposition(
    __in    CMilSlaveVideo  *pCaller
    )
{
    HRESULT         hr = S_OK;
    bool            lastResource = false;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "EndComposition(%p)",
        pCaller);

    {
        CGuard<CCriticalSection>    guard(m_compositionLock);

        RemoveCompositingResource(pCaller);

        DBG_CODE(DumpResourceList());

        lastResource = m_compositingResources.IsEmpty();

    }

    if (lastResource)
    {
        IFC(PostCompositionPassCleanup());
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
EvrPresenter::AVSurfaceRenderer::
PostCompositionPassCleanup(
    void
    )
{
    HRESULT         hr = S_OK;
    bool            signalMixer = false;

    TRACEF(&hr);

    //
    // Tell the media buffer that we are done with it. (It can be safely
    // used by the EVR).
    //
    if (m_pRenderedBuffer)
    {
        IGNORE_HR(m_pRenderedBuffer->DoneWithBitmap());
    }

    //
    // If we don't have multiple composition devices and the composition device isn't
    // the same as our render device and either
    // a) we aren't rendering in software or
    // b) we are rendering in software, but the composition device isn't software,
    //    and we haven't attempted to render with it
    // then we want to switch over to the new device.
    //
    if (   !m_haveMultipleCompositionDevices
        && m_pCompositionRenderDevice != NULL
        && m_pCompositionRenderDevice != m_pCurrentRenderDevice
        && (   m_pCurrentRenderDevice != m_pSoftwareDevice
            || (   m_deviceContinuity > m_lastHardwareDeviceContinuity
                && m_pCompositionRenderDevice != m_pSoftwareDevice)))
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Changing over to a new device because we are consistently rendering to it.");

        if (m_pCompositionRenderDevice != m_pSoftwareDevice)
        {
            m_lastHardwareDeviceContinuity = m_deviceContinuity;

            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Re-attempting hardware");
        }

        IGNORE_HR(
            FallbackToSoftwareIfNecessary(
                NewRenderDevice(m_pCompositionRenderDevice)));
    }

    IFC(m_pEvrPresenter->GetSampleScheduler().ReturnCompositionSample(&signalMixer));
    ReleaseInterface(m_pRenderedBuffer);

Cleanup:

    ReleaseInterface(m_pCompositionRenderDevice);

    m_haveMultipleCompositionDevices = false;

    //
    // This takes a lock but it should be reasonably infrequent.
    //
    if (signalMixer)
    {
        IGNORE_HR(SignalMixer());
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::GetContentRect, IAVSurfaceRender
//
//  Synopsis:
//      Returns the size of the media we are going to return.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
GetContentRect(
    __out_ecount(1) MilPointAndSizeL *prcContent
        // The rectangle containing the media
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(prcContent);

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        prcContent->X = 0;
        prcContent->Y = 0;
        prcContent->Height = m_dwHeight;
        prcContent->Width = m_dwWidth;
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "Returned rect (d) : {%d, %d, %d, %d}",
        prcContent->X,
        prcContent->Y,
        prcContent->Height,
        prcContent->Width);

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::GetContentRectF, IAVSurfaceRender
//
//  Synopsis:
//      Returns the size of the media we are going to return.
//
//------------------------------------------------------------------------------
STDMETHODIMP
EvrPresenter::AVSurfaceRenderer::
GetContentRectF(
    __out_ecount(1) MilPointAndSizeF *prcContent
        // The floating point rectangle the media will be contained in
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        prcContent->X = 0;
        prcContent->Y = 0;
        prcContent->Height = static_cast<float>(m_dwHeight);
        prcContent->Width = static_cast<float>(m_dwWidth);
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "Returned rect (f) : {%f, %f, %f, %f}",
        prcContent->X,
        prcContent->Y,
        prcContent->Height,
        prcContent->Width);

    return S_OK;
}

//
// Normal methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::ChangeMediaType
//
//  Synopsis:
//      Called by the EVR when our media type changes (or is reset). The surface
//      renderer mainly cares about this because it changes the size of our content
//      Rect.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::
ChangeMediaType(
    __in_opt    IMFVideoMediaType       *pIVideoMediaType
        // The media type (might be NULL).
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CDummySource    *pDummySource = NULL;
    CDummySource    *pOldDummySource = NULL;
    DWORD           width = 0;
    DWORD           height = 0;

    if (pIVideoMediaType)
    {
        width = pIVideoMediaType->GetVideoFormat()->videoInfo.dwWidth;
        height = pIVideoMediaType->GetVideoFormat()->videoInfo.dwHeight;

        pDummySource = new CDummySource(width, height, MilPixelFormat::BGR32bpp);

        IFCOOM(pDummySource);

        pDummySource->AddRef();

        {
            CGuard<CCriticalSection>    guard(m_compositionLock);

            m_dwWidth = width;
            m_dwHeight = height;

            pOldDummySource = m_pDummySource;
            m_pDummySource = pDummySource;
            pDummySource = NULL;
        }

    }

Cleanup:
    ReleaseInterface(pDummySource);
    ReleaseInterface(pOldDummySource);

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::Shutdown
//
//  Synopsis:
//      Called when we are being shutdown. We need to release any interfaces
//      that might result in a circular reference.
//
//------------------------------------------------------------------------------
void
EvrPresenter::AVSurfaceRenderer::
Shutdown(
    void
    )
{
    TRACEF(NULL);

    CWmpStateEngine *pWmpStateEngineRelease = NULL;

    if (m_compositionLock.IsValid())
    {
        m_compositionLock.Enter();
    }

    pWmpStateEngineRelease = m_pWmpStateEngine;
    m_pWmpStateEngine = NULL;

    if (m_compositionLock.IsValid())
    {
        m_compositionLock.Leave();
    }

    ReleaseInterface(pWmpStateEngineRelease);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::SignalFallbackFailure
//
//  Synopsis:
//      Called by the evr to tell us about fallback failures it encounters.
//
//------------------------------------------------------------------------------
void
EvrPresenter::AVSurfaceRenderer::
SignalFallbackFailure(
    __in        HRESULT                 hr
    )
{
    TRACEF(NULL);

    Assert(IsSoftwareFallbackError(hr));

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        m_fallbackFailure = hr;
    }
}

//
// Private methods
//

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::GetSWDevice
//
//  Synopsis:
//      Creates a D3D device of the corresponding type.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::
GetSWDevice(
    __deref_out CD3DDeviceLevel1            **ppD3DDevice
        // The returned device
    )
{
    HRESULT     hr = S_OK;
    CFloatFPU   oGuard;

    TRACEF(&hr);

    CD3DDeviceManager *pD3DDeviceManager = CD3DDeviceManager::Get();

    IFC(pD3DDeviceManager->GetSWDevice(ppD3DDevice));

Cleanup:
    if (pD3DDeviceManager)
    {
        CD3DDeviceManager::Release();
    }

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::GetHWDevice
//
//  Synopsis:
//      Creates a D3D device of the corresponding type.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::GetHWDevice(
    __in        UINT                        adapter,
    __in        bool                        forceMultithreaded,
    __deref_out CD3DDeviceLevel1            **ppD3DDevice
        // The returned device
    )
{
    HRESULT     hr = S_OK;
    CFloatFPU   oGuard;

    TRACEF(&hr);

    CD3DDeviceManager *pD3DDeviceManager = CD3DDeviceManager::Get();
    CDisplaySet const *pDisplaySet = NULL;

    // Note: Since the goal is not to create a swap chain the only flag that
    //       matters is FULLSCREEN.  If this code should work with fullscreen
    //       devices a FULLSCREEN_AGNOSTIC flag or similar mechanism should be
    //       added.
    static const MilRTInitialization::Flags sc_milInitFlags = MilRTInitialization::Default;

    if (g_DisplayManager.HasCurrentDisplaySet())
    {
        g_DisplayManager.GetCurrentDisplaySet(&pDisplaySet);
    }
    else
    {
        IFC(g_DisplayManager.DangerousGetLatestDisplaySet(&pDisplaySet));
    }

    //
    // Unless D3D recognizes an adapter, we can't even load a software device.
    //
    if (pDisplaySet->GetNumD3DRecognizedAdapters() <= adapter)
    {
        IFC(WGXERR_AV_VIDEOACCELERATIONNOTAVAILABLE);
    }

    Assert(pDisplaySet->Display(adapter));
    Assert(pDisplaySet->D3DObject());

    //
    // This may fail if D3D support is unavailable
    //
    IFC(pD3DDeviceManager->GetD3DDeviceAndPresentParams(
            GetDesktopWindow(),
            sc_milInitFlags,
            pDisplaySet->Display(adapter),
            D3DDEVTYPE_HAL,
            ppD3DDevice,
            NULL,
            NULL));

Cleanup:

    ReleaseInterface(pDisplaySet);

    if (pD3DDeviceManager)
    {
        CD3DDeviceManager::Release();
    }

    EXPECT_SUCCESS(hr);

    hr = TreatNonSoftwareFallbackErrorAsUnknownHardwareError(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::NewRenderDevice
//
//  Synopsis:
//      Called when we are changing to a new D3D render device. Changes the
//      mixer device appropriately.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::
NewRenderDevice(
    __in    CD3DDeviceLevel1            *pNewRenderDevice
        // The new device we are going to use.
    )
{
    HRESULT             hr = S_OK;
    CD3DDeviceLevel1    *pMixerDevice = NULL;

    TRACEF(&hr);

    ReplaceInterface(m_pCurrentRenderDevice, pNewRenderDevice);

    //
    // We just use the mixer device as the render device. This will need
    // to be changed if we go back to the shared surface optimization
    //
    SetInterface(pMixerDevice, m_pCurrentRenderDevice);

    //
    // Tell the Evr presenter about the new device.
    //
    IFC(m_pEvrPresenter->NewMixerDevice(
            m_pCurrentRenderDevice,
            pMixerDevice,
            m_pCurrentRenderDevice == m_pSoftwareDevice ? D3DDEVTYPE_SW : D3DDEVTYPE_HAL));

Cleanup:
    ReleaseInterface(pMixerDevice);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::FallbackToSoftware
//
//  Synopsis:
//      Creates the software device (if it hasn't been created yet) and sets
//      this as the new device type.
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::
FallbackToSoftware(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CD3DDeviceLevel1    *pD3DDevice = NULL;

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "Falling back to software");

    if (NULL == m_pSoftwareDevice)
    {
        IFC(GetSWDevice(&pD3DDevice));

        SetInterface(m_pSoftwareDevice, pD3DDevice);
    }

    //
    // We don't want to call ResetDevice with the same device
    //
    if (m_pCurrentRenderDevice != m_pSoftwareDevice)
    {
        IFC(NewRenderDevice(m_pSoftwareDevice));
    }

Cleanup:

    ReleaseInterface(pD3DDevice);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      EvrPresenter::AVSurfaceRenderer::SignalMixer
//
//  Synopsis:
//      Signals the mixer to start immediately (we have just returned a sample
//      that they have indicated they want).
//
//------------------------------------------------------------------------------
HRESULT
EvrPresenter::AVSurfaceRenderer::
SignalMixer(
    void
    )
{
    HRESULT     hr = S_OK;
    LONGLONG    currentTime   = 0;
    DWORD       continuityKey = 0;
    TRACEF(&hr);

    //
    // We call GetRenderTime just so that we can get the continuity key
    //
    IFC(m_pRenderClock->GetRenderTime(&currentTime, &continuityKey));

    //
    // We pass in time 0 so that we'll get called back immediately.
    // GetRenderTime returns gc_invalidTimerTime in certain cases so we don't
    // pass in the time returned from it.
    //
    IFC(m_pEvrPresenter->SignalMixer(continuityKey, 0LL));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
EvrPresenter::AVSurfaceRenderer::
AddCompositingResource(
    __in    CMilSlaveVideo  *pCMilSlaveVideo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_compositingResources.AddHead(pCMilSlaveVideo));

    hr = S_OK; // ignore S_FALSE, which means already exists

Cleanup:
    RRETURN(hr);
}

void
EvrPresenter::AVSurfaceRenderer::
RemoveCompositingResource(
    __in    CMilSlaveVideo  *pCMilSlaveVideo
    )
{
    TRACEF(NULL);

    m_compositingResources.Remove(pCMilSlaveVideo);
}

void
EvrPresenter::AVSurfaceRenderer::
DumpResourceList(
    void
    )
{
    TRACEF(NULL);

    UniqueList<CMilSlaveVideo*>::Node   *pCurrent = NULL;
    UniqueList<CMilSlaveVideo*>::Node   *pNext = NULL;

    pCurrent = m_compositingResources.GetHead();

    while (pCurrent != NULL)
    {
        pNext = pCurrent->GetNext();

        LogAVDataM(
            AVTRACE_LEVEL_VERBOSE,
            AVCOMP_PRESENTER,
            "Resource: %p",
            pCurrent->instance);

        pCurrent = pNext;
    }
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// EvrPresenter::ProcessSamplesData implementation
//
inline
EvrPresenter::ProcessSamplesData::
ProcessSamplesData(
    void
    ) : nextTime(gc_invalidTimerTime),
        continuityKey(0),
        fallbackFailure(S_OK),
        mediaFinished(false)
{}

HRESULT
EvrPresenter::
SetVideoWindow(
    __in        HWND                 hwndVideo
    )
{
    TRACEF(NULL);

    m_videoWindow = hwndVideo;

    RRETURN(S_OK);
}

HRESULT
EvrPresenter::
GetVideoWindow(
    __deref_out HWND                 *phwndVideo
    )
{
    TRACEF(NULL);

    *phwndVideo = m_videoWindow;

    RRETURN(S_OK);
}

HRESULT
EvrPresenter::
SetVideoPosition(
    __inout_opt const MFVideoNormalizedRect *pnrcSource,
    __inout_opt const LPRECT                prcDest
    )
{
    TRACEF(NULL);

    if (pnrcSource)
    {
        m_nrcSource = *pnrcSource;
    }
    else
    {
        ZeroMemory(&m_nrcSource, sizeof(m_nrcSource));
    }

    if (prcDest)
    {
        m_rcDest = *prcDest;
    }
    else
    {
        ZeroMemory(&m_rcDest, sizeof(m_rcDest));
    }

    RRETURN(S_OK);
}

HRESULT
EvrPresenter::
GetVideoPosition(
    __inout_opt MFVideoNormalizedRect *pnrcSource,
    __out       LPRECT               prcDest
    )
{
    TRACEF(NULL);

    if (pnrcSource)
    {
        *pnrcSource = m_nrcSource;
    }

    *prcDest = m_rcDest;

    RRETURN(S_OK);
}

HRESULT
EvrPresenter::
SetRenderingPrefs(
    __in        DWORD               dwRenderFlags
    )
{
    TRACEF(NULL);
    RRETURN(E_NOTIMPL);
}

HRESULT
EvrPresenter::
GetRenderingPrefs(
    __out       DWORD               *pdwRenderFlags
    )
{
    TRACEF(NULL);
    RRETURN(E_NOTIMPL);
}


