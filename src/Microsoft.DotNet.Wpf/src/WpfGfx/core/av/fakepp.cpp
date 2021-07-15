// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "fakepp.tmh"

#if DBG // This file is not needed in production code

MtDefine(CFakePP, Mem, "CFakePP");
MtExtern(AVEvent); // defined in wmpeventhandler.cpp

#define CHECK_FOR_SHUTDOWN()                                                    \
    if (m_status == Terminated)                                                 \
    {                                                                           \
        goto Cleanup;                                                           \
    }                                                                           \

static const double sc_ticksPerMillisecond = (gc_ticksPerSecond / 1000);

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::CFakePP
//
//  Synopsis: Create a new instance of CFakePP
//
//------------------------------------------------------------------------------
CFakePP::CFakePP(MediaInstance *pMediaInstance)
: m_uiID(pMediaInstance->GetID())
, m_pMediaInstance(NULL)
, m_pVideoResource(NULL)
, m_hThread(NULL)
, m_hEvent(NULL)
, m_fThreadRunning(false)
, m_uiColor(0)
, m_uiFrames(50)
, m_uiCurrentFrame(0)
, m_dwFrameDuration(150)
, m_uiVideoWidth(100)
, m_uiVideoHeight(100)
, m_dblRate(1)
, m_pMediaBuffer(NULL)
, m_pCD3DDeviceLevel1(NULL)
, m_status(Stopped)
, m_pLogFile(NULL)
{
    AddRef();
    SetInterface(m_pMediaInstance, pMediaInstance);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::~CFakePP
//
//  Synopsis: Destructor for CFakePP
//
//------------------------------------------------------------------------------
CFakePP::~CFakePP()
{
    TRACEF(NULL);
    // Shutdown should be called prior to the destructor
    Assert(m_pMediaBuffer == NULL);

    if (m_pLogFile != NULL)
    {
        int returnCode = fclose(m_pLogFile);
        Assert(returnCode == 0); // Make the drt fail if we can't close the log file
        m_pLogFile = NULL;
    }


    IGNORE_HR(Shutdown()); // prevent memory leaks even in the case of errors

    ReleaseInterface(m_pMediaInstance);
    ReleaseInterface(m_pCD3DDeviceLevel1);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Initialize
//
//  Synopsis: Initialization for CFakePP
//
//------------------------------------------------------------------------------
HRESULT CFakePP::Initialize()
{
    HRESULT hr = S_OK;

    IFC(m_csEntry.Init());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::~CFakePP
//
//  Synopsis: make sure the state is consistent
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::VerifyConsistency()
{
    HRESULT hr = S_OK;
    if (m_status != Playing && m_status != Stopped && m_status != Paused)
    {
        RIP("UNEXPECTED: m_status != Playing, Stopped, or Paused");
        IFC(E_UNEXPECTED);
    }
    if (m_dblRate < 0.01 || m_dblRate > 100)
    {
        RIP("UNEXPECTED: !(0.01 <= m_dblRate <= 100)");
        IFC(E_UNEXPECTED);
    }

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Create
//
//  Synopsis: Public interface for creating a new instance of CFakePP
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::Create(
    __in MediaInstance *pMediaInstance,
    __in DWORD dwFrameDuration,
    __in UINT uiFrames,
    __in UINT uiVideoWidth,
    __in UINT uiVideoHeight,
    __deref_out_ecount(1) CFakePP ** ppFakePP
    )
{
    HRESULT                    hr = S_OK;
    TRACEFID(pMediaInstance->GetID(), &hr);
    CFakePP                    *pFakePP = NULL;
    CHECKPTRARG(ppFakePP);

    pFakePP = new CFakePP(pMediaInstance);
    IFCOOM(pFakePP);
    IFC(pFakePP->Initialize());

    pFakePP->SetFrameDuration(dwFrameDuration);
    pFakePP->SetFrames(uiFrames);


    pFakePP->SetVideoWidth(uiVideoWidth);
    pFakePP->SetVideoHeight(uiVideoHeight);

    //
    // Copy references
    //
    *ppFakePP = pFakePP;
    pFakePP = NULL;

Cleanup:
    ReleaseInterface(pFakePP);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::RaiseEvent
//
//  Synopsis: Raise an event in managed code by sending it through the proxy
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::RaiseEvent(
    __in AVEvent avEventType
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pMediaInstance->GetMediaEventProxy().RaiseEvent(
            avEventType));
Cleanup:

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::StaticThreadProc
//
//  Synopsis: Static member function called by the worker thread
//
//------------------------------------------------------------------------------
DWORD WINAPI CFakePP::StaticThreadProc(VOID *pv)
{
    // Immediately invoke the member function
    Assert(pv);
    CFakePP* pFakePP = static_cast<CFakePP *>(pv);
    pFakePP->ThreadProc();
    return 0;
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::ThreadProc
//
//  Synopsis: Member function called by the worker thread
//
//------------------------------------------------------------------------------
void
CFakePP::ThreadProc()
{
    //
    // local variables to avoid race conditions
    //
    DWORD dwFrameDuration = 0;
    Status status = Stopped;

    for (;;)
    {
        // We must enter a critical section before accessing member variables
        m_csEntry.Enter();
        if (m_status == Terminated)
        {
            m_csEntry.Leave();
            break;
        }

        Assert(SUCCEEDED(VerifyConsistency()));
        dwFrameDuration = DWORD(m_dwFrameDuration / m_dblRate);
        status = m_status;

        if (status == Playing && m_uiCurrentFrame >= m_uiFrames)
        {
            status = Stopped;
            IGNORE_HR(RaiseEvent(AVMediaEnded));

            IGNORE_HR(RaiseEvent(AVMediaClosed));
        }

        if (status == Playing)
        {
            NotifyVideoResource();
            m_uiCurrentFrame++;
        }

        m_csEntry.Leave();

        if (status == Terminated)
        {
            break;
        }
        else if (status == Playing)
        {
            Sleep(dwFrameDuration);
        }
        else
        {
            WaitForSingleObject(m_hEvent, INFINITE);
        }
    }
}

//+-----------------------------------------------------------------------------
//
// CFakePP::NotifyVideoResource
//
//------------------------------------------------------------------------------
void
CFakePP::NotifyVideoResource()
{
    // private no need to lock or CHECK_FOR_SHUTDOWN()

    if (m_pVideoResource)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_PRESENTER,
            "Resource exists - notifying");

        m_pVideoResource->NewFrame();
    }
}


//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Open
//
//  Synopsis: Pretends to open an URL for playback
//
//  Returns: Success if the operation succeeds (this does NOT mean the URL is
//      valid)
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::Open(
    __in LPCWSTR pwszURL
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    Assert(fopen_s(&m_pLogFile, "avdrt.log", "w") == 0);  // Make the drt fail if we can't open the log file

    IGNORE_HR(RaiseEvent(AVMediaOpened));

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Start
//
//  Synopsis: Start playback
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::Start()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    if (m_status != Playing)
    {
        m_status = Playing;

        if (m_fThreadRunning)
        {
            IGNORE_HR(RaiseEvent(AVMediaStarted));
            SetEvent(m_hEvent);
        }
        else
        {
            m_hThread = CreateThread(NULL,
                                     0,
                                     CFakePP::StaticThreadProc,
                                     (void*)this,
                                     0,
                                     NULL
                                     );
            if (!m_hThread)
            {
                IFC(E_UNEXPECTED);
            }
            m_hEvent = CreateEvent(NULL,
                                   FALSE,
                                   FALSE,
                                   TEXT("FakePP Thread")
                                   );
            if (!m_hEvent)
            {
                IFC(E_UNEXPECTED);
            }

            m_fThreadRunning = true;
            IGNORE_HR(RaiseEvent(AVMediaStarted));
        }
    }

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Stop
//
//  Synopsis: Stop playback
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::Stop()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    m_status = Stopped;
    m_uiCurrentFrame = 0;
    IGNORE_HR(RaiseEvent(AVMediaStopped));

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Close
//
//  Synopsis: Close the player, same as a stop for the CFakePP.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::Close()
{

    int returnCode = fclose(m_pLogFile);
    Assert(returnCode == 0); // Make the drt fail if we can't close the log file
    m_pLogFile = NULL;

    RRETURN(Stop());
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Pause
//
//  Synopsis: Pause media playback
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::Pause()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    // Do the same thing regardless of previous status
    m_status = Paused;
    IGNORE_HR(RaiseEvent(AVMediaPaused));

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::SetRate
//
//  Synopsis: Adjust playback speed
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::SetRate(double dblRate)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    if ((dblRate < 0.01 || dblRate > 100) && dblRate != 0)
    {
        IFC(E_INVALIDARG);
    }

    if (dblRate > 0)
    {
        if (m_status != Playing)
        {
            IFC(Start());
            m_dblRate = dblRate;
        }
    }
    else
    {
        if (m_status != Paused)
        {
            IFC(Pause());
        }
    }

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::GetNaturalHeight
//
//  Synopsis: Get the native height of the video
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetNaturalHeight(__out_ecount(1) UINT *puiHeight)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(puiHeight);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *puiHeight = m_uiVideoHeight;

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::GetNaturalWidth
//
//  Synopsis: Get the native width of the video
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetNaturalWidth(__out_ecount(1) UINT *puiWidth)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(puiWidth);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *puiWidth = m_uiVideoWidth;

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::SetVolume
//
//  Synopsis: Adjust the volume of the media
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::SetVolume(double dblVolume)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::SetBalance
//
//  Synopsis: Adjust the volume of the media
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::SetBalance(double dblBalance)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::SetIsScrubbingEnabled
//
//  Synopsis: Enable/Disable scrubbing
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::SetIsScrubbingEnabled(bool isScrubbingEnabled)
{
    RRETURN(S_OK);
}


/* Return whether or not we're currently buffering */
STDMETHODIMP CFakePP::IsBuffering(
    __out_ecount(1) bool *pIsBuffering
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CHECKPTRARG(pIsBuffering);

    *pIsBuffering = false;

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

/* Return whether or not we can pause */
STDMETHODIMP CFakePP::CanPause(
    __out_ecount(1) bool *pCanPause
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CHECKPTRARG(pCanPause);

    *pCanPause = true;

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

/* Get the download progress */
STDMETHODIMP CFakePP::GetDownloadProgress(
    __out_ecount(1) double *pProgress
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CHECKPTRARG(pProgress);

    *pProgress = 1;

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

/* Get the buffering progress */
STDMETHODIMP CFakePP::GetBufferingProgress(
    __out_ecount(1) double *pProgress
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CHECKPTRARG(pProgress);

    *pProgress = 1;

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::GetMediaLength
//
//  Synopsis: Get the length of the media in 100 nanosecond ticks
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetMediaLength(__out_ecount(1) LONGLONG *pllLength)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(pllLength);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *pllLength = static_cast<LONGLONG>(sc_ticksPerMillisecond * m_uiFrames * m_dwFrameDuration);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::GetMediaProgress
//
//  Synopsis: Get the length of the media in 100 nanosecond ticks
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetPosition(__out_ecount(1) LONGLONG *pllProgress)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(pllProgress);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *pllProgress = static_cast<LONGLONG>(sc_ticksPerMillisecond * m_uiCurrentFrame * m_dwFrameDuration);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::SetPosition
//
//  Synopsis: Set the playback position
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::SetPosition(LONGLONG llTime)
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    // ignore seek calls - we don't want broken synchronization to affect this
    // DRT

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::GetSurfaceRenderer
//
//  Synopsis: Get the IAVSurfaceRenderer object associated with this player
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetSurfaceRenderer(
    __deref_out_ecount(1) IAVSurfaceRenderer **ppSurfaceRenderer
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(ppSurfaceRenderer);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *ppSurfaceRenderer = this;
    (*ppSurfaceRenderer)->AddRef();

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::HasVideo
//
//  Synopsis: Determine if the media has video (currently we always return true)
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::HasVideo(
    __out_ecount(1) bool *pfHasVideo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(pfHasVideo);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *pfHasVideo = true;

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::HasAudio
//
//  Synopsis: Determine if the media has audio (currently we always return true)
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::HasAudio(
    __out_ecount(1) bool *pfHasAudio
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(pfHasAudio);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    *pfHasAudio = true;

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::NeedUIFrameUpdate
//
//  Synopsis: Called whenever the UI needs a frame update, throttling mechanism
//            to ensure that decode doesn't outrun the UI.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::NeedUIFrameUpdate()
{
    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::Shutdown
//
//  Synopsis: Called when we are done with video to
//      break reference circularities.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::Shutdown()
{
    TRACEF(NULL);

    m_csEntry.Enter();
    m_status = Terminated;
    SetEvent(m_hEvent);
    m_csEntry.Leave();

    WaitForSingleObject(m_hThread, INFINITE);

    m_csEntry.Enter();
    CloseHandle(m_hThread);
    m_hThread = NULL;
    CloseHandle(m_hEvent);
    m_hEvent = NULL;
    ReleaseInterface(m_pMediaBuffer);
    m_csEntry.Leave();

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::ProcessExitHandler
//
//  Synopsis:
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::ProcessExitHandler()
{
    TRACEF(NULL);
    m_pMediaInstance->GetMediaEventProxy().Shutdown();
    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CFakePP::HrFindInterface, CMILCOMBase
//
//  Synopsis: Get a pointer to another interface implemented by
//      CFakePP
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(ppvObject);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    if (riid == IID_IMILMedia)
    {
        *ppvObject = static_cast<IMILMedia *>(this);
    }
    else if (riid == IID_IMILSurfaceRendererProvider)
    {
        *ppvObject = static_cast<IMILSurfaceRendererProvider *>(this);
    }
    else if (riid == IID_IAVSurfaceRenderer)
    {
        *ppvObject = static_cast<IAVSurfaceRenderer *>(this);
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_DEFAULT,
            "Unexpected interface request in CFakePP");

        IFC(E_NOINTERFACE);
    }

Cleanup:
    RRETURN(hr);
}


//
// IAVSurfaceRenderer methods
//
STDMETHODIMP
CFakePP::BeginComposition(
    __in    CMilSlaveVideo  *pCaller,
    __in    BOOL            displaySetChanged,
    __in    BOOL            syncChannel,
    __inout LONGLONG        *pLastCompositionSampleTime,
    __out   BOOL            *pbFrameReady
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    CHECKPTRARG(pbFrameReady);

    *pbFrameReady = TRUE;

Cleanup:

    RRETURN(hr);
}


HRESULT
CFakePP::CreateDevice(
    __deref_out CD3DDeviceLevel1            **ppD3DDevice
        // The returned device
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    CD3DDeviceManager *pD3DDeviceManager = CD3DDeviceManager::Get();
    CDisplaySet const *pDisplaySet = NULL;

    // Note: Since the goal is not to create a swap chain the only flag that
    //       matters is FULLSCREEN.  If this code should work with fullscreen
    //       devices a FULLSCREEN_AGNOSTIC flag or similar mechanism should be
    //       added.
    static const MilRTInitialization::Flags sc_milInitFlags = MilRTInitialization::Default;

    g_DisplayManager.GetCurrentDisplaySet(&pDisplaySet);

    //
    // Unless D3D recognizes an adapter, we can't even load a software device.
    //
    if (pDisplaySet->GetNumD3DRecognizedAdapters() <= 0)
    {
        IFC(WGXERR_AV_VIDEOACCELERATIONNOTAVAILABLE);
    }

    Assert(pDisplaySet->Display(0));
    Assert(pDisplaySet->D3DObject());

    //
    // This may fail if D3D support is unavailable
    //
    IFC(pD3DDeviceManager->GetD3DDeviceAndPresentParams(
            GetDesktopWindow(),
            sc_milInitFlags,
            pDisplaySet->Display(0),
            D3DDEVTYPE_SW,
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

    RRETURN(hr);
}




//+-----------------------------------------------------------------------------
//
// CFakePP::BeginRender
//
// Renders the video frame to a surface.
// If a backbuffer is provided, rendering will happen
// directly on the backbuffer using the given offsets
// in the rectangle. Otherwise rendering happens on
// an intermediate surface. Returns IWGXBitmapSource
// when using an intermediate surface.
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::BeginRender(
    __in_ecount_opt(1)   CD3DDeviceLevel1  *pDeviceLevel1,        // NULL OK (in SW)
    __deref_out_ecount(1) IWGXBitmapSource **ppWGXBitmapSource
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    IDirect3DDevice9      *pIDevice = NULL;
    IDirect3DSurface9     *pSurface = NULL;
    IMFGetService         *pGetService = NULL;
    D3DCOLOR              color = D3DCOLOR_XRGB(0x00, 0x00, 0x00);

    CHECKPTRARG(ppWGXBitmapSource);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    if (m_pMediaBuffer == NULL)
    {
        IFC(CreateDevice(&m_pCD3DDeviceLevel1));

        IFC(CMFMediaBuffer::Create(
                0,                  // No component id. (Only ever one fake pp).
                0,                  // No continuity number.
                m_uiVideoWidth,
                m_uiVideoHeight,
                D3DFMT_X8R8G8B8,
                m_pCD3DDeviceLevel1,
                m_pCD3DDeviceLevel1,
                D3DDEVTYPE_SW,
                &m_pMediaBuffer));
    }

    IFC(m_pMediaBuffer->QueryInterface(__uuidof(IMFGetService),
                                       reinterpret_cast<void**>(&pGetService)));
    IFC(m_pMediaBuffer->GetService(MR_BUFFER_SERVICE,
                                   IID_IDirect3DSurface9,
                                   reinterpret_cast<void**>(&pSurface)));


    // Alternate between flag colors
    if (m_uiColor == 0)
    {
        color = D3DCOLOR_XRGB(0xff, 0x00, 0x00);
        m_uiColor++;
    }
    else if (m_uiColor == 1)
    {
        color = D3DCOLOR_XRGB(0xff, 0xff, 0xff);
        m_uiColor++;
    }
    else
    {
        color = D3DCOLOR_XRGB(0x00, 0x00, 0xff);
        m_uiColor = 0;
    }

    GetUnderlyingDevice(m_pCD3DDeviceLevel1, &pIDevice);

    IFC(pIDevice->ColorFill(
        pSurface,
        NULL,
        color
        ));

    IFC(m_pMediaBuffer->GetBitmapSource(
    		false,  		// Not the synchronous channel
    		pDeviceLevel1, 
    		ppWGXBitmapSource));

    fprintf(m_pLogFile, "Frame shown.\n");

Cleanup:
    ReleaseInterface(pSurface);
    ReleaseInterface(pIDevice);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
// CFakePP::EndRender
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::EndRender()
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    IFC(m_pMediaBuffer->DoneWithBitmap());

Cleanup:
    RRETURN(hr);
}

STDMETHODIMP
CFakePP::EndComposition(
    __in    CMilSlaveVideo  *pCaller
    )
{
    return S_OK;
}

//+-----------------------------------------------------------------------------
//
// CFakePP::GetContentRectF
//
// GetContentRect returns the source rectangle of the video
// Typically this is the width and the height of the video
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetContentRectF(
        __out_ecount(1) MilPointAndSizeF *prcContent
        )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(prcContent);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    Assert(prcContent);
    prcContent->X = 0;
    prcContent->Y = 0;
    prcContent->Height = 0;
    prcContent->Width = 0;

    // 
    //if (m_uiVideoHeight > FLOAT_MAX || m_uiVideoWidth > FLOAT_MAX)
    //{
    //    IFC(E_UNEXPECTED);
    //}

    prcContent->Height = float(m_uiVideoHeight);
    prcContent->Width = float(m_uiVideoWidth);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
// CFakePP::GetContentRect
//
//  Integer version
// GetContentRect returns the source rectangle of the video
// Typically this is the width and the height of the video
//
//------------------------------------------------------------------------------
STDMETHODIMP
CFakePP::GetContentRect(
    __out_ecount(1) MilPointAndSizeL *prcContent
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECKPTRARG(prcContent);
    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    prcContent->X = 0;
    prcContent->Y = 0;
    prcContent->Height = m_uiVideoHeight;
    prcContent->Width = m_uiVideoWidth;

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
// CFakePP::RegisterResource
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::RegisterResource(
    __in CMilSlaveVideo *pSlaveVideo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    m_pVideoResource = pSlaveVideo;

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
// CFakePP::UnregisterResource
//
//------------------------------------------------------------------------------
HRESULT
CFakePP::UnregisterResource(
    __in CMilSlaveVideo *pSlaveVideo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CGuard<CCriticalSection> guard(m_csEntry);

    CHECK_FOR_SHUTDOWN();
    IFC(VerifyConsistency());

    Assert(m_pVideoResource == pSlaveVideo);
    m_pVideoResource = NULL;

Cleanup:
    RRETURN(hr);
}

#endif //DBG


