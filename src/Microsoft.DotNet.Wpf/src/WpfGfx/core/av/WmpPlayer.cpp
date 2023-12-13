// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "WmpPlayer.tmh"

MtDefine(CWmpPlayer, Mem, "CWmpPlayer");
MtDefine(UpdateState, Mem, "UpdateState");


//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::CWmpPlayer
//
//  Synopsis: Create a new instance of CWmpPlayer
//
//  Returns: A new instance CWmpPlayer
//
//------------------------------------------------------------------------------
CWmpPlayer::
CWmpPlayer(
    __in    MediaInstance       *pMediaInstance
    ) : m_uiID(pMediaInstance->GetID()),
        m_pMediaInstance(NULL),
        m_pCWmpStateEngine(NULL),
        m_pUpdateState(NULL),
        m_fShutdown(false),
        m_currentUrl(NULL)
{
    TRACEF(NULL);
    AddRef();

    SetInterface(m_pMediaInstance, pMediaInstance);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::~CWmpPlayer
//
//  Synopsis: Destructor for CWmpPlayer
//
//------------------------------------------------------------------------------
CWmpPlayer::
~CWmpPlayer()
{
    TRACEF(NULL);

    Assert(m_fShutdown);

    ReleaseInterface(m_pCWmpStateEngine);
    ReleaseInterface(m_pMediaInstance);
    ReleaseInterface(m_pUpdateState);

    delete[] m_currentUrl;
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::Create
//
//  Synopsis: Public interface for creating a new instance of CWmpPlayer
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
Create(
    __in            MediaInstance       *pMediaInstance,
    __in            bool                canOpenAnyMedia,
    __deref_out     CWmpPlayer          **ppPlayer
    )
{
    HRESULT         hr = S_OK;
    TRACEFID(pMediaInstance->GetID(), &hr);

    CWmpPlayer      *pWmpPlayer = NULL;

    pWmpPlayer = new CWmpPlayer(pMediaInstance);

    IFCOOM(pWmpPlayer);

    //
    // For now, wait for the WmpState interface to start up synchronously, we would
    // like to make this uniformly asynchronous when we have better eventing support.
    //
    IFC(pWmpPlayer->Init(canOpenAnyMedia));

    *ppPlayer = pWmpPlayer;
    pWmpPlayer = NULL;

Cleanup:
    ReleaseInterface(pWmpPlayer);

    EXPECT_SUCCESSID(pMediaInstance->GetID(), hr);
    RRETURN(hr);
}

HRESULT
CWmpPlayer::
Init(
    __in            bool                canOpenAnyMedia
    )
{
    HRESULT         hr = S_OK;
    CWmpStateEngine *pCWmpStateEngine = NULL;
    UpdateState     *pUpdateState = NULL;
    TRACEF(&hr);

    IFC(m_sharedState.Init());

    IFC(CWmpStateEngine::Create(
            m_pMediaInstance,
            canOpenAnyMedia,
            &m_sharedState,
            &pCWmpStateEngine));

    IFC(UpdateState::Create(m_pMediaInstance, pCWmpStateEngine, &pUpdateState));

    m_pCWmpStateEngine = pCWmpStateEngine;
    pCWmpStateEngine = NULL;

    m_pUpdateState = pUpdateState;
    pUpdateState = NULL;

Cleanup:
    ReleaseInterface(pCWmpStateEngine);
    ReleaseInterface(pUpdateState);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::Open
//
//  Synopsis: Opens an URL for playback
//
//  Returns: Success if the operation succeeds (this does NOT mean the URL is
//      valid)
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
Open(
    __in LPCWSTR pwszURL
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "Open(%S)",
        pwszURL);

    Assert(!m_fShutdown);

    //
    // Nothing changes if we're told to open the same
    // URL.
    //
    if (!AreStringsEqual(m_currentUrl, pwszURL))
    {
        m_sharedState.SetTimedOutPosition(0);
        m_sharedState.SetTimedOutDownloadProgress(0.0);
        m_sharedState.SetTimedOutBufferingProgress(0.0);
        m_sharedState.SetLength(0LL);
        m_sharedState.SetNaturalWidth(0);
        m_sharedState.SetNaturalHeight(0);

        delete[] m_currentUrl;
        m_currentUrl = NULL;

        IFC(CopyHeapString(pwszURL, &m_currentUrl));

        m_pUpdateState->OpenHelper(pwszURL);

        IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::Stop
//
//  Synopsis: Stop playback
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
Stop()
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "Stop()");

    Assert(!m_fShutdown);

    m_pUpdateState->SetTargetActionState(ActionState::Stop);

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::Close
//
//  Synopsis: Close the media player, this is treated as a request that the
//            Ocx is stopped and unloaded.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
Close()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "Close()");

    Assert(!m_fShutdown);

    m_sharedState.SetTimedOutPosition(0LL);
    m_sharedState.SetTimedOutDownloadProgress(0.0);
    m_sharedState.SetTimedOutBufferingProgress(0.0);
    m_sharedState.SetLength(0LL);
    m_sharedState.SetNaturalWidth(0);
    m_sharedState.SetNaturalHeight(0);

    delete[] m_currentUrl;
    m_currentUrl = NULL;

    m_pUpdateState->Close();

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::SetRate
//
//  Synopsis: Adjust playback speed
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
SetRate(
    __in double dRate
    )
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);

    Assert(!m_fShutdown);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "SetRate(%.4f)",
        dRate);

    m_pUpdateState->SetRateHelper(dRate);

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetNaturalHeight
//
//  Synopsis: Get the native height of the video
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetNaturalHeight(
    __out       UINT *puiHeight
    )
{
    TRACEF(NULL);

    *puiHeight = m_sharedState.GetNaturalHeight();

    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "GetNaturalHeight(*%u)",
        *puiHeight);

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetNaturalWidth
//
//  Synopsis: Get the native width of the video
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetNaturalWidth(
    __out       UINT    *puiWidth
    )
{
    TRACEF(NULL);

    *puiWidth = m_sharedState.GetNaturalWidth();

    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "GetNaturalWidth(*%u)",
        *puiWidth);

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::SetVolume
//
//  Synopsis: Adjust the volume of the media
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
SetVolume(
    __in double dblVolume
    )
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);
    Assert(!m_fShutdown);

    LONG volume = LONG((dblVolume * 100) + 0.5); // add 0.5 to round to the nearest integer

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "SetVolume(%.4f) (WMP format: %d)",
        dblVolume,
        volume);

    if (dblVolume < 0 || dblVolume > 1)
    {
        IFC(E_INVALIDARG);
    }

    Assert(volume >= 0 && volume <= 100);

    m_pUpdateState->SetTargetVolume(volume);

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::SetBalance
//
//  Synopsis: Adjust the balance of the media
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
SetBalance(
    __in    double      dblBalance
    )
{
    HRESULT         hr = S_OK;
    long            lBalance = 0;

    TRACEF(&hr);

    lBalance = long((dblBalance * 100) + 0.5); // add 0.5 to round to the nearest integer

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "SetBalance(%.4f) (WMP format: %d)",
        dblBalance,
        lBalance);

    Assert(!m_fShutdown);

    if (dblBalance < -1 || dblBalance > 1)
    {
        IFC(E_INVALIDARG);
    }

    Assert(lBalance >= -100 && lBalance <= 100);

    m_pUpdateState->SetTargetBalance(lBalance);

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

STDMETHODIMP
CWmpPlayer::
SetIsScrubbingEnabled(
    __in    bool        isScrubbingEnabled
    )
{
    HRESULT         hr = S_OK;

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "SetIsScrubbingEnabled(%!bool!)",
        isScrubbingEnabled);

    Assert(!m_fShutdown);

    m_pUpdateState->SetTargetIsScrubbingEnabled(isScrubbingEnabled);

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetMediaLength
//
//  Synopsis: Get the length of the media in 100 nanosecond ticks
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetMediaLength(
    __out       LONGLONG        *pllLength
    )
{
    TRACEF(NULL);

    Assert(!m_fShutdown);

    *pllLength = m_sharedState.GetLength();

    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "GetMediaLength(*%I64d)",
        *pllLength);

    RRETURN(S_OK);
}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::SetPosition
//
//  Synopsis: Set the playback position
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
SetPosition(
    __in    LONGLONG llTime
    )
{
    HRESULT             hr = S_OK;

    //
    // we must give the ocx the position in seconds as a double
    // we must convert this from 100 nanosecond ticks
    //
    double          position = double(llTime) / gc_ticksPerSecond;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "SetPosition(%I64d) (WMP format: %.4f)",
        llTime,
        position);

    Assert(!m_fShutdown);

    m_sharedState.SetTimedOutPosition(llTime);

    m_pUpdateState->SetTargetSeekTo(position);

    IFC(m_pCWmpStateEngine->AddItem(m_pUpdateState));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetPosition
//
//  Synopsis: Set the playback position
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetPosition(
    __out    LONGLONG *pllTime
    )
{
    HRESULT hr = S_OK;
    bool    didTimeOut = false;
    TRACEF(&hr);

    Assert(!m_fShutdown);

    IFC(m_pUpdateState->UpdateTransientsSync(10, &didTimeOut));

    if (didTimeOut)
    {
        *pllTime = m_sharedState.GetTimedOutPosition();
    }
    else
    {
        *pllTime = m_sharedState.GetPosition();
    }

    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "GetPosition(*%I64d)",
        *pllTime);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetSurfaceRenderer
//
//  Synopsis: Get the IAVSurfaceRenderer object associated with this player
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetSurfaceRenderer(
    __deref_out     IAVSurfaceRenderer **ppSurfaceRenderer
    )
{
    return m_pCWmpStateEngine->GetSurfaceRenderer(ppSurfaceRenderer);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpPlayer::RegisterResource
//
//  Synopsis:
//      Registers a new resource for new frame notifications.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
RegisterResource(
    __in    CMilSlaveVideo *pSlaveVideo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_pMediaInstance->GetCompositionNotifier().RegisterResource(pSlaveVideo));

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpPlayer::UnregisterResource
//
//  Synopsis:
//      Unregisters a resource from receiving new frame notifications.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
UnregisterResource(
    __in    CMilSlaveVideo *pSlaveVideo
    )
{
    TRACEF(NULL);

    m_pMediaInstance->GetCompositionNotifier().UnregisterResource(pSlaveVideo);

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::IsBuffering, IMILMedia
//
//  Synopsis: Return whether or not we're currently buffering
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
IsBuffering(
    __out   bool    *pIsBuffering
    )
{
    TRACEF(NULL);

    *pIsBuffering = m_sharedState.GetIsBuffering();

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "IsBuffering(*%!bool!)",
        *pIsBuffering);

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::CanPause, IMILMedia
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
CanPause(
    __out   bool    *pCanPause
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CHECKPTRARG(pCanPause);
    Assert(!m_fShutdown);

    *pCanPause = m_sharedState.GetCanPause();

    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "CanPause(*%!bool!)",
        *pCanPause);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetDownloadProgress, IMILMedia
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetDownloadProgress(
    __out   double      *pProgress
    )
{
    HRESULT hr = S_OK;
    bool    didTimeOut = false;
    TRACEF(&hr);

    IFC(m_pUpdateState->UpdateTransientsSync(10, &didTimeOut));

    if (didTimeOut)
    {
        *pProgress = m_sharedState.GetTimedOutDownloadProgress();
    }
    else
    {
        *pProgress = m_sharedState.GetDownloadProgress();
    }

    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "GetDownloadProgress(*%.4f)",
        *pProgress);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::GetBufferingProgress, IMILMedia
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
GetBufferingProgress(
    __out   double      *pProgress
    )
{
    HRESULT hr = S_OK;
    bool    didTimeOut = false;
    TRACEF(&hr);

    IFC(m_pUpdateState->UpdateTransientsSync(10, &didTimeOut));

    if (didTimeOut)
    {
        *pProgress = m_sharedState.GetTimedOutBufferingProgress();
    }
    else
    {
        *pProgress = m_sharedState.GetBufferingProgress();
    }


    LogAVDataM(
        AVTRACE_LEVEL_VERBOSE,
        AVCOMP_PLAYER,
        "GetBufferingProgress(*%.4f)",
        *pProgress);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::HasVideo
//
//  Synopsis: Determine if the media has video (currently we always return true)
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
HasVideo(
    __out    bool *pfHasVideo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    CHECKPTRARG(pfHasVideo);
    Assert(!m_fShutdown);

    *pfHasVideo = m_sharedState.GetHasVideo();

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "HasVideo(*%!bool!)",
        *pfHasVideo);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::HasAudio
//
//  Synopsis: Determine if the media has audio.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
HasAudio(
    __out   bool *pfHasAudio
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    CHECKPTRARG(pfHasAudio);
    Assert(!m_fShutdown);

    *pfHasAudio = m_sharedState.GetHasAudio();

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "HasAudio(*%!bool!)",
        *pfHasAudio);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::NeedUIFrameUpdate
//
//  Synopsis: Indicate that a frame update is required back up to the UI.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
NeedUIFrameUpdate()
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "NeedUIFrameUpdate()");

    Assert(!m_fShutdown);

    m_pCWmpStateEngine->NeedUIFrameUpdate();

    return S_OK;
}


//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::Shutdown
//
//  Synopsis: Called when we are done with video to
//      break reference circularities.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
Shutdown()
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "Shutdown()");

    Assert(!m_fShutdown);
    m_fShutdown = true;

    //
    // This will succeed unless we are under very low memory conditions
    //
    IFC(WmpStateEngineProxy::CallMethod(
            m_uiID,
            m_pCWmpStateEngine,
            m_pCWmpStateEngine,
            &CWmpStateEngine::Shutdown,
            0));

    //
    // We don't release m_pCWmpStateEngine here because we may need it to retrieve
    // the dummy presenter if someone calls GetSurfaceRenderer later on. We
    // don't need to release it here because there is no circular reference
    // from CWmpStateEngine back to the player.
    //

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::ProcessExitHandler
//
//  Synopsis: Called when we are done with video to
//      break reference circularities.
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::
ProcessExitHandler()
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PLAYER,
        "ProcessExitHandler()");

    m_pMediaInstance->GetMediaEventProxy().Shutdown();
    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member: CWmpPlayer::HrFindInterface, CMILCOMBase
//
//  Synopsis: Get a pointer to another interface implemented by
//      CWmpPlayer
//
//------------------------------------------------------------------------------
STDMETHODIMP
CWmpPlayer::HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppvObject
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    if (!ppvObject)
    {
        IFCN(E_INVALIDARG);
    }

    if (riid == IID_IMILMedia)
    {
        // No AddRef because CMILCOMBase does it for me
        *ppvObject = static_cast<IMILMedia *>(this);
    }
    if (riid == IID_IMILSurfaceRendererProvider)
    {
        // No AddRef because CMILCOMBase does it for me
        *ppvObject = static_cast<IMILSurfaceRendererProvider *>(this);
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_PLAYER,
            "Unexpected interface request: %!IID!",
            &riid);

        IFCN(E_NOINTERFACE);
    }

Cleanup:
    RRETURN(hr);
}


