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
//      Provides support for the player state manager. This provides a separate
//      thread which starts up the Player OCX and also provides services for
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "WmpStateEngine.tmh"

#define SEEK_GRANULARITY 0.001

MtDefine(CWmpStateEngine, Mem, "CWmpStateEngine");
MtDefine(SubArcMethodItem, Mem, "SubArcMethodItem");

//
// Public methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Create
//
//  Synopsis:
//      Creates a new object the performs state management for the WmpOcx. This
//      object waits for state transitions to be applied to it (from any thread,
//      and then dispatches them to the Ocx). It then in turns waits for the
//      Ocx to notify it that it has reached a particular state before allowing
//      a queued up "Target" transition to become the next transition.
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CWmpStateEngine::
Create(
    __in        MediaInstance       *pMediaInstance,
        // per media globals
    __in        bool                canOpenAnyMedia,
    __in        SharedState         *pSharedState,
    __deref_out CWmpStateEngine     **ppPlayerState
    )
{
    HRESULT             hr = S_OK;
    CWmpStateEngine     *pWmpStateEngine = NULL;

    TRACEFID(pMediaInstance->GetID(), &hr);

    pWmpStateEngine = new CWmpStateEngine(pMediaInstance, canOpenAnyMedia, pSharedState);

    IFCOOM(pWmpStateEngine);

    pWmpStateEngine->InternalAddRef();

    IFC(pWmpStateEngine->Init());

    *ppPlayerState = pWmpStateEngine;
    pWmpStateEngine = NULL;

Cleanup:

    ReleaseInterface(pWmpStateEngine);

    EXPECT_SUCCESSID(pMediaInstance->GetID(), hr);

    RRETURN(hr);
}

void
CWmpStateEngine::
SetHasAudio(
    __in    bool            hasAudio
    )
{
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "SetHasAudio(%d)",
        hasAudio);

    m_pSharedState->SetHasAudio(hasAudio);
}

void
CWmpStateEngine::
SetHasVideo(
    __in    bool            hasVideo
    )
{
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "SetHasVideo(%d)",
        hasVideo);

    m_pSharedState->SetHasVideo(hasVideo);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdatePosition
//
//  Synopsis:
//      Updates the shared state position
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdatePosition(
    void
    )
{
    HRESULT hr = S_OK;

    //
    // We need to get the player because we're in a different apartment
    //
    IWMPControls    *pControls = NULL;
    double          position = 0.0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    if (m_targetState.m_seekTo.m_isValid)
    {
        position = m_targetState.m_seekTo.m_value;
    }
    else if (m_targetInternalState.m_seekTo.m_isValid)
    {
        position = m_targetInternalState.m_seekTo.m_value;
    }
    else
    {
        if (!m_isMediaEnded)
        {
            if (NULL != m_pIWMPPlayer)
            {
                IFC(m_pIWMPPlayer->get_controls(&pControls));

                IFC(pControls->get_currentPosition(&position));
            }
            else
            {
                LogAVDataM(
                    AVTRACE_LEVEL_INFO,
                    AVCOMP_STATEENGINE,
                    "OCX not yet available - returning position of 0");

                position = 0;
            }
        }
        //
        // If we've reached the end of the media we report our position as being
        // the end of the media. We need to explicitly take care of this
        // condition because otherwise WMP will return 0 as the position.
        //
        else
        {
            position = m_mediaLength;
        }
    }

    //
    // we must return the position in 100 nanosecond ticks
    //
    m_pSharedState->SetPosition(SecondsToTicks(position));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Updating position to %I64d (WMP format: %f)",
        m_pSharedState->GetPosition(),
        position);

Cleanup:
    ReleaseInterface(pControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateDownloadProgress
//
//  Synopsis:
//      Update the download progress of media.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateDownloadProgress(
    void
    )
{
    HRESULT hr = S_OK;
    IWMPNetwork *pNetwork = NULL;
    long        lProgress = 0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // WMP's download progress isn't valid if the user has requested a different
    // URL.
    //
    if (   NULL != m_pIWMPPlayer
        && m_targetState.m_url
        && AreStringsEqual(m_targetState.m_url, m_currentInternalState.m_url))
    {
        IFC(m_pIWMPPlayer->get_network(&pNetwork));

        IFC(pNetwork->get_downloadProgress(&lProgress));
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "DownloadProgress not valid - returning progress of 0");

        lProgress = 0;
    }

    SetDownloadProgress(double(lProgress) / 100.0);

    hr = S_OK;

Cleanup:
    ReleaseInterface(pNetwork);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateBufferingProgress
//
//  Synopsis:
//      Updates the buffering progress of media.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateBufferingProgress(
    void
    )
{
    HRESULT hr = S_OK;
    IWMPNetwork *pNetwork = NULL;
    long        lProgress = 0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // WMP's buffering progress isn't valid if the user has requested a
    // different URL.
    //
    if (   NULL != m_pIWMPPlayer
        && m_targetState.m_url
        && AreStringsEqual(m_targetState.m_url, m_currentInternalState.m_url))
    {
        IFC(m_pIWMPPlayer->get_network(&pNetwork));

        IFC(pNetwork->get_bufferingProgress(&lProgress));
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "BufferingProgress not valid - returning progress of 0");

        lProgress = 0;
    }

    SetBufferingProgress(double(lProgress) / 100.0);

    hr = S_OK;

Cleanup:
    ReleaseInterface(pNetwork);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateMediaLength
//
//  Synopsis:
//      Decide whether the given media can be seeked. Stores the result in
//      m_canSeek
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateMediaLength(
    void
    )
{
    HRESULT hr = S_OK;
    IWMPMedia   *pMedia = NULL;
    double      mediaLength = 0.0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    IFC(m_pIWMPPlayer->get_currentMedia(&pMedia));

    Assert(pMedia); // should have media by this point

    IFC(pMedia->get_duration(&mediaLength));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "UpdateMediaLength: WMP format length of %.4f",
        mediaLength);

    SetMediaLength(mediaLength);

    hr = S_OK;

Cleanup:
    ReleaseInterface(pMedia);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetMediaLength
//
//  Synopsis:
//      Set the length of media
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
SetMediaLength(
    __in        double          length
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_mediaLength = length;

    //
    // duration gives us the number of seconds as a double
    // we must convert this to 100 nanosecond ticks
    // We add 0.5 to round up to the nearest integer
    //
    m_pSharedState->SetLength(SecondsToTicks(m_mediaLength));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Setting length to %4f",
        m_mediaLength);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateNaturalHeight
//
//  Synopsis:
//      Get the native height of the video. Returns 0 if the height is not yet
//      available. The height is not cached, in case it changes mid-stream (I
//      haven't encountered any videos that actually do this, but I'm not
//      certain that it's not possible)
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateNaturalHeight(
    void
    )
{
    UINT            pixels = static_cast<UINT>(-1);

    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    pixels = m_presenterWrapper.DisplayHeight();

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Updating height to %d",
        pixels);

    m_pSharedState->SetNaturalHeight(pixels);

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateNaturalWidth
//
//  Synopsis:
//      Get the native width of the video, returns 0 if the width is not yet
//      available. The width is not cached, in case it changes mid-stream (I
//      haven't encountered any videos that actually do this, but I'm not
//      certain that it's not possible)
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateNaturalWidth(
    void
    )
{
    UINT            pixels          = static_cast<UINT>(-1);

    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    pixels = m_presenterWrapper.DisplayWidth();

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Updating width to %d",
        pixels);

    m_pSharedState->SetNaturalWidth(pixels);

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetIsBuffering
//
//  Synopsis:
//      Set whether or not we're currently buffering
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
SetIsBuffering(
    __in    bool    isBuffering
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "IsBuffering(%!bool!)",
        isBuffering);

    m_pSharedState->SetIsBuffering(isBuffering);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetCanPause
//
//  Synopsis:
//      Set whether or not we can pause
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
SetCanPause(
    __in    bool    canPause
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "CanPause(%!bool!)",
        canPause);

    m_canPause = canPause;

    m_pSharedState->SetCanPause(canPause);
}

void
CWmpStateEngine::
SetDownloadProgress(
    __in    double  downloadProgress
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "SetDownloadProgress(%.4f)",
        downloadProgress);

    m_pSharedState->SetDownloadProgress(downloadProgress);
}

void
CWmpStateEngine::
SetBufferingProgress(
    __in    double  bufferingProgress
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "SetBufferingProgress(%.4f)",
        bufferingProgress);

    m_pSharedState->SetBufferingProgress(bufferingProgress);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Close
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
Close(
    void
    )
{
    HRESULT hr = S_OK;
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // Our target state is to go back to the beginning.
    // We don't need to set the m_isOcxCreated field to false because
    // that is a side effect of Clear. We do need to set the target
    // volume to the default because Clear will set it to 0.
    //
    m_targetState.Clear();
    m_targetState.m_volume = gc_defaultAvalonVolume;

    m_isScrubbingEnabled = false;
    m_didRaisePrerolled = false;

    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetOcx
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetOcx(
    __in    bool                isOcxCreated
    )
{
    HRESULT hr = S_OK;
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_targetState.m_isOcxCreated = isOcxCreated;

    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetUrl
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetUrl(
    __in    LPCWSTR             url
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    Assert(m_stateThreadId == GetCurrentThreadId());

    delete[] m_targetState.m_url;
    m_targetState.m_url = NULL;

    IFC(CopyHeapString(url, &m_targetState.m_url));

    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetActionState
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetActionState(
    __in    ActionState::Enum   actionState
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    m_targetState.m_actionState = actionState;

    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetVolume
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetVolume(
    __in    long                volume
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_targetState.m_volume = volume;
    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetBalance
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetBalance(
    __in    long                balance
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_targetState.m_balance = balance;
    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetRate
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetRate(
    __in    double              rate
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_targetState.m_rate = rate;
    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetSeekTo
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetSeekTo(
    __in    Optional<double>    seekTo
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_targetState.m_seekTo = seekTo;
    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetTargetIsScrubbingEnabled
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetTargetIsScrubbingEnabled(
    __in    bool                isScrubbingEnabled
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "SetTargetIsScrubbingEnabled(%d)",
        isScrubbingEnabled);

    //
    // We don't need to signal ourselves - the next time we do a seek while
    // paused we'll query this property and do the right thing
    //
    m_isScrubbingEnabled = isScrubbingEnabled;

    RRETURN(S_OK);
}

HRESULT
CWmpStateEngine::
InvalidateDidRaisePrerolled(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_didRaisePrerolled = false;
    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateHasVideoForWmp11
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateHasVideoForWmp11(
    void
    )
{
    UINT        height = static_cast<UINT>(-1);
    UINT        width = static_cast<UINT>(-1);
    bool        hasVideo = false;

    TRACEF(NULL);

    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // If we are using render config, we can't use the graph to
    // determine whether we have video.
    //
    if (m_useRenderConfig)
    {
        width = m_presenterWrapper.DisplayWidth();

        height = m_presenterWrapper.DisplayHeight();

        hasVideo = width != 0 || height != 0;

        SetHasVideo(hasVideo);
    }

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::ErrorInTransition
//
//  Synopsis:
//      Handle a failed transition. In the case of failures we
//
//      1. Raise an event to the caller (it might be a failure returned by the Ocx
//         but not raised as an asycnhronous error event).
//      2. Abandon the previous state transition arc.
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
ErrorInTransition(
    __in    HRESULT                 failureHr
        // HRESULT of failure.
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_ERROR,
        AVCOMP_STATEENGINE,
        "Hit failure on state transition %x, abandoning arc",
        failureHr);

    //
    // Abandon all pending transitions
    //
    m_actualState.m_seekTo.m_isValid = false;
    IFC(m_actualState.Copy(&m_currentInternalState));
    IFC(m_currentInternalState.Copy(&m_pendingInternalState));
    IFC(m_currentInternalState.Copy(&m_targetInternalState));
    IFC(m_nextSubArcMethodStack.Clear());
    m_isMediaEnded = false;

    //
    // We might not have a video presenter if we don't have support on the platform.
    // for example, amd64 or WMP9.
    //
    m_presenterWrapper.EndScrub();
    m_presenterWrapper.EndFakePause();

    //
    // This is kind of nasty because it almost surely takes the
    // unmanaged/managed layers out of sync, but the alternative is to
    // repetitively try the same arc over and over again. I'm not sure which is
    // worse.
    //
    IFC(m_currentInternalState.Copy(&m_targetState));

    m_volumeMask.m_isValid = false;

    IFC(RaiseEvent(AVMediaFailed, failureHr));

Cleanup:
    EXPECT_SUCCESS(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PlayerReachedActionState
//
//  Synopsis:
//      This call-back is called from the Wmp Ocx when certain play states are
//      reached. We translate into our own state and then finalize an arc (we
//      could also start moving to a new target state). We discard events that
//      don't map directly to a useable state for us.
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
PlayerReachedActionState(
    __in    WMPPlayState            state
        // The WMP player state
    )
{
    TRACEF(NULL);

    bool                        runState = false;
    Optional<ActionState::Enum> ourState = MapWmpStateEngine(state);

    if (state == wmppsBuffering)
    {
        SetIsBuffering(true);
    }
    else
    {
        SetIsBuffering(false);
    }

    if (ourState.m_isValid)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "PlayerReachedActionState ours: %d, wmps: %d",
            ourState.m_value,
            state);

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Notified of our player state %d",
            ourState.m_value);

        //
        // If the player hasn't changed states, we don't need to run the transition
        //
        if (m_actualState.m_actionState != ourState.m_value)
        {
            m_actualState.m_actionState = ourState.m_value;
            runState = true;
        }

        //
        // PlayerReachedActionStatePlay relies upon m_actualState.m_actionState
        // being set correctly, so we need to call the method after setting the
        // variable.
        //
        if (ourState.m_value == ActionState::Play)
        {
            PlayerReachedActionStatePlay();
        }

    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Notified of Ocx player state %d",
            state);

        if (state == wmppsMediaEnded)
        {
            THR(MediaFinished());
        }
    }

    if (runState)
    {
        THR(SignalSelf());
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PlayerReachedOpenState
//
//  Synopsis:
//      This call-back is called from the Wmp Ocx (via CWmpEventHandler) when
//      open states are reached. This allows us to treat opening a new URL as an
//      asynchronous transition.
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
PlayerReachedOpenState(
    __in    WMPOpenState            state
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    if (state == wmposPlaylistOpenNoMedia)
    {
        // The MSDN documentation says we're not allowed to call get_URL from an
        // event handler, so we just have to trust that the file that is open is
        // really the one we just opened.

        if (!AreStringsEqual(m_actualState.m_url, m_pendingInternalState.m_url))
        {
            IFC(CopyHeapString(m_pendingInternalState.m_url, &m_actualState.m_url));

            IFC(SignalSelf());
        }
    }

Cleanup:
    EXPECT_SUCCESS(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PlayerReachedPosition
//
//  Synopsis:
//      Called from the WMP OCX (via CWmpEventHandler) when the player reaches a
//      particular position. This allows us to treat seek as an asynchronous
//      transition.
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
PlayerReachedPosition(
    __in double newPosition
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // Should not receive duplicate reached position notifications
    //
    Assert(!m_actualState.m_seekTo.m_isValid);

    m_actualState.m_seekTo = newPosition;

    THR(SignalSelf());
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::EvrReachedState
//
//  Synopsis:
//      Called from the EVR (via CEvrPresenter) when the EVR reaches a new
//      state.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
EvrReachedState(
    __in    RenderState::Enum   renderState
    )
{
    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "EvrReachedState(%d)",
        renderState);

    m_isEvrClockRunning = EvrStateToIsEvrClockRunning(renderState);

    THR(SignalSelf());

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::ScrubSampleComposited
//
//  Synopsis:
//      Called from the EVR (via CEvrPresenter) when the EVR reaches a new
//      state.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
ScrubSampleComposited(
    __in    int                 placeHolder
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    m_didReceiveScrubSample = true;

    IFC(SignalSelf());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::NewPresenter
//
//  Synopsis:
//      Creates a new presenter and sets it on the WmpStateEngine, we shutdown
//      the old presenter.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
NewPresenter(
    __deref_out EvrPresenterObj **ppNewPresenter
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(
        EvrPresenter::Create(
            m_pMediaInstance,
            m_ResetToken,
            this,
            m_pDXVAManagerWrapper,
            ppNewPresenter));

    m_presenterWrapper.SetPresenter(*ppNewPresenter);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::GetSurfaceRenderer
//
//  Synopsis:
//      Retrieves the surface renderer, this could either by a dummy renderer or
//      the surface renderer associated with the evr presenter. We don't
//      necessarily have a surface renderer if we haven't yet initialized the
//      Ocx, or if we have closed the Ocx.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
GetSurfaceRenderer(
    __deref_out    IAVSurfaceRenderer  **ppISurfaceRenderer
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    IFC(m_presenterWrapper.GetSurfaceRenderer(ppISurfaceRenderer));

    if (!*ppISurfaceRenderer)
    {
        SetInterface(*ppISurfaceRenderer, m_pDummyRenderer);
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::NeedUIFrameUpdate
//
//  Synopsis:
//      Indicates that we need a UI frame update.
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
NeedUIFrameUpdate(
    void
    )
{
    m_pMediaInstance->GetCompositionNotifier().NeedUIFrameUpdate();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Shutdown
//
//  Synopsis:
//      Shut down the wmp player from the UI thread.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
Shutdown(
    __in    int     placeholder
    )
{
    TRACEF(NULL);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_isShutdown = true;
    m_targetState.m_isOcxCreated = false;

    IGNORE_HR(RemoveOcx());

    ReleaseInterface(m_pDXVAManagerWrapper);

    //
    // Make sure no more items are added to the state thread through us.
    //
    if (!SetEvent(m_isShutdownEvent))
    {
        RIP("The only way an event can fail to be signalled is if the handle has become invalid through a Close");
    }

    //
    // Remove the apartment item from the apartment scheduler.
    //
    m_pStateThread->ReleaseItem(this);

    m_pStateThread->CancelAllItemsWithOwner(static_cast<IUnknown*>(static_cast<CMILCOMBase*>(this)));

    //
    // This should be released by RemoveOcx
    //
    Assert(m_pDXVAManagerWrapper == NULL);

    RRETURN(S_OK);
}


//
// Protected methods
//
STDMETHODIMP
CWmpStateEngine::
HrFindInterface(
    __in_ecount(1)  REFIID          riid,
    __deref_out     void            **ppv
    )
{
    //
    // IUnknown handled by CMILCOMBase
    //
    RRETURN(E_NOINTERFACE);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Run,   CStateThreadItem
//
//  Synopsis:
//      This is called whenever we are run on the apartment thread side this
//      corresponds to an AddItem call on the appartment manager.
//
//------------------------------------------------------------------------------
__override
void
CWmpStateEngine::
Run(
    void
    )
{
    TRACEF(NULL);

    DBG_CODE(if (m_pStateThread) { m_stateThreadId = m_pStateThread->GetThreadId(); });

    HandleStateChange();
}

//
// Private methods
//
CWmpStateEngine::
CWmpStateEngine(
    __in        MediaInstance           *pMediaInstance,
    __in        bool                    canOpenAnyMedia,
    __in        SharedState         *pSharedState
    ) : m_uiID(pMediaInstance->GetID()),
        m_pDummyRenderer(NULL),
        m_pIWMPPlayer(NULL),
        m_pIConnectionPoint(NULL),
        m_connectionPointAdvise(0),
        m_isMediaEnded(false),
        m_didSeek(false),
        m_needFlushWhenEndingFreeze(false),
        m_canSeek(true),
        m_isShutdown(false),
        m_pStateThread(NULL),
        m_uiThreadId(0),
        m_pDXVAManagerWrapper(NULL),
        m_pWmpEventHandler(NULL),
        m_mediaLength(0.0),
        m_waitForActionState(ActionState::Stop),
        m_lastActionState(ActionState::Stop),
        m_useRenderConfig(false),
        m_lastRenderState(ActionState::Stop),
        m_isEvrClockRunning(false),
        m_nextSubArcMethodStack(pMediaInstance->GetID()),
        m_isScrubbingEnabled(false),
        m_canOpenAnyMedia(canOpenAnyMedia),
        m_didReceiveScrubSample(false),
        m_didPreroll(false),
        m_canPause(false),
        m_didRaisePrerolled(false),
        m_presenterWrapper(pMediaInstance->GetID()),
        m_pSharedState(pSharedState) // not ref-counted
#if DBG
        , m_stateThreadId(static_cast<DWORD>(-1))
#endif
{
    TRACEF(NULL);

    SetInterface(m_pMediaInstance, pMediaInstance);

    m_nextSubArcMethodStack.SetStateEngine(this);
}

/*virtual*/
CWmpStateEngine::
~CWmpStateEngine(
    )
{
    TRACEF(NULL);

    ReleaseInterface(m_pIWMPPlayer);
    ReleaseInterface(m_pIConnectionPoint);
    ReleaseInterface(m_pStateThread);
    ReleaseInterface(m_pDXVAManagerWrapper);
    ReleaseInterface(m_pWmpEventHandler);
    ReleaseInterface(m_pDummyRenderer);
    ReleaseInterface(m_pMediaInstance);

    m_pSharedState = NULL; // not ref-counted

    if (m_isShutdownEvent)
    {
        CloseHandle(m_isShutdownEvent);
        m_isShutdownEvent = NULL;
    }

    //
    // This destructor is called from the garbage collector thread, so we
    // aren't allowed to block. However, we maintain a global reference on
    // the state thread that isn't given up until DLL Process Detach, so
    // we know that we won't release the last reference to the state
    // thread, and therefore we won't block.
    //
    ReleaseInterface(m_pStateThread);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Init
//
//  Synopsis:
//      Initialize any state that might fail. In this case, we have the apartment
//      tnread (could fail to initialize its synchronization events), an event
//      to signal when we are initialized and an an event to signal when we
//      are shutdown. We also tell the state thread to start instantiating the
//      WMP OCX.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
Init(
    void
    )
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);

    m_uiThreadId = GetCurrentThreadId();

    IFC(m_presenterWrapper.Init());

    IFC(CStateThreadItem::Init());

    IFC(CStateThread::CreateApartmentThread(&m_pStateThread));

    //
    // Create a dummy surface renderer so that if we are caught without
    // a surface renderer (because we are initializing), we can just
    // return the dummy.
    //
    IFC(DummySurfaceRenderer::Create(m_pMediaInstance, &m_pDummyRenderer));

    //
    // We can't call the accessor methods because they assert that
    // we're on the state thread. Since the state thread hasn't run
    // yet, it's safe to initialize on the UI thread.
    //
    m_targetState.m_isOcxCreated = true;
    m_targetState.m_volume = gc_defaultAvalonVolume;


    m_isShutdownEvent
        = CreateEvent(
                NULL,                   // Security Attributes
                TRUE,                   // Manual Reset
                FALSE,                  // Initial State is not signalled
                NULL);                  // Name

    if (NULL == m_isShutdownEvent)
    {
        IFC(GetLastErrorAsFailHR());
        IFC(E_UNEXPECTED);
    }

    IFC(SignalSelf());

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::InitializeOcx
//
//  Synopsis:
//      In the apartment thread, initialize the Ocx, if this fails, then the
//      failure can be returned to the UI thread through some asynchronous call
//      later. This should only fail in extremely low resource conditions or if
//      the wrong version of WMP is present.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
InitializeOcx(
    void
    )
{
    HRESULT                    hr = S_OK;
    IWMPSettings              *pSettings = NULL;
    IOleObject                *pOleObj = NULL;
    IConnectionPointContainer *pConnectionContainer = NULL;
    CWmpClientSite            *pClientSite = NULL;
    Wmp11ClientSite           *p11ClientSite = NULL;
    CWmpEventHandler          *pEventHandler = NULL;
    IWMPPlayer                *pIWmpPlayer = NULL;
    IConnectionPoint          *pIConnectionPoint = NULL;
    IWMPVideoRenderConfig     *pIWMPVideoRenderConfig = NULL;
    IWMPRenderConfig          *pIWMPRenderConfig = NULL;
    MFActivateObj             *pActivateObj = NULL;
    DWORD                     connectionPointAdvise = static_cast<DWORD>(-1);

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_targetState.m_isOcxCreated);
    Assert(!m_pendingInternalState.m_isOcxCreated);
    m_pendingInternalState.m_isOcxCreated = m_targetState.m_isOcxCreated;

    //
    // If we don't have a DXVA manager wrapper (the first time this call is made),
    // the create one.
    //
    if (NULL == m_pDXVAManagerWrapper)
    {
        IFC(CDXVAManagerWrapper::Create(m_uiID, &m_ResetToken, &m_pDXVAManagerWrapper));
    }

    //
    // Create objects
    //
    THR(hr = CAVLoader::CreateWmpOcx(&pIWmpPlayer));

    if (FAILED(hr) && hr != WGXERR_AV_WMPFACTORYNOTREGISTERED)
    {
        hr = WGXERR_AV_INVALIDWMPVERSION;
    }

    IFC(hr);

    IFC(pIWmpPlayer->QueryInterface(&pOleObj));

    //
    // Creating the MFActivateObj will result in the m_pPresenter being initialized
    // through the call to NewPresenter, this will be returned to WMP10 through
    // the client site or it will be returned to WMP11 through the activation
    // object.
    //
    IFC(
        MFActivate::Create(
            m_uiID,
            this,
            &pActivateObj));

    //
    // On Polaris (and with Vista), we insert our presenter
    //
    if (SUCCEEDED(pIWmpPlayer->QueryInterface(__uuidof(IWMPVideoRenderConfig), reinterpret_cast<void **>(&pIWMPVideoRenderConfig))))
    {
        m_useRenderConfig = true;

        IFC(Wmp11ClientSite::Create(
                m_uiID,
                &p11ClientSite));

        IFC(pOleObj->SetClientSite(p11ClientSite));

        //
        // On Vista we want to make sure that they don't attempt to initialize us in another
        // process.
        //
        if (SUCCEEDED(pIWmpPlayer->QueryInterface(__uuidof(IWMPRenderConfig), reinterpret_cast<void **>(&pIWMPRenderConfig))))
        {
            IFC(pIWMPRenderConfig->put_inProcOnly(TRUE));
        }

        IFC(pIWMPVideoRenderConfig->put_presenterActivate(pActivateObj));
    }
    else
    {
        m_useRenderConfig = false;

        IFC(
            CWmpClientSite::Create(
                m_uiID,
                &pClientSite,
                this));

        //
        // Connect the objects to each other
        //
        IFC(pOleObj->SetClientSite(pClientSite));
    }

    IFC(CheckPlayerVersion(pIWmpPlayer));

    IFC(pIWmpPlayer->QueryInterface(&pSettings));
    IFC(pIWmpPlayer->QueryInterface(&pConnectionContainer));

    if (!m_canOpenAnyMedia)
    {
       IFC(SetSafeForScripting(pIWmpPlayer));
    }

    //
    // We don't want the media to start until we explicitly say so
    //
    IFC(pSettings->put_autoStart(VARIANT_FALSE));

    //
    // Don't show error dialogs
    //
    IFC(pSettings->put_enableErrorDialogs(VARIANT_FALSE));

    //
    // don't invoke urls in browser
    //
    IFC(pSettings->put_invokeURLs(VARIANT_FALSE));

    IFC(CWmpEventHandler::Create(m_pMediaInstance, this, &pEventHandler));

    //
    // We try to connect to the IWMPEvents interface.
    // If that fails, we fall back to _WMPOCXEvents.
    //
    hr = pConnectionContainer->FindConnectionPoint(__uuidof(IWMPEvents), &pIConnectionPoint);

    if (FAILED(hr))
    {
        //
        // If not, try the _WMPOCXEvents interface, which will use IDispatch
        //
        IFC(pConnectionContainer->FindConnectionPoint(
                __uuidof(_WMPOCXEvents),
                &pIConnectionPoint
                ));
    }

    //
    // IConnectionPoint::Advise takes an IUnknown. We have to cast here to
    // prevent an ambiguous conversion error
    //
    IFC(pIConnectionPoint->Advise(
            static_cast<IWMPEvents*>(pEventHandler),
            &connectionPointAdvise));

    Assert(NULL == m_pIWMPPlayer);
    m_pIWMPPlayer = pIWmpPlayer;
    pIWmpPlayer = NULL;

    Assert(NULL == m_pIConnectionPoint);
    m_pIConnectionPoint = pIConnectionPoint;
    pIConnectionPoint = NULL;

    //
    // Save the event handler so that we can disconnect it later.
    //
    Assert(NULL == m_pWmpEventHandler);
    m_pWmpEventHandler = pEventHandler;
    pEventHandler = NULL;

    m_connectionPointAdvise = connectionPointAdvise;

    m_currentInternalState.m_isOcxCreated = true;

    //
    // The OCX is created - we make a note of that so we won't try to create it
    // again.
    //
    m_actualState.m_isOcxCreated = true;

Cleanup:

    ReleaseInterface(pIConnectionPoint);
    ReleaseInterface(pIWmpPlayer);
    ReleaseInterface(pSettings);
    ReleaseInterface(pOleObj);
    ReleaseInterface(pConnectionContainer);
    ReleaseInterface(pClientSite);
    ReleaseInterface(p11ClientSite);
    ReleaseInterface(pEventHandler);
    ReleaseInterface(pIWMPVideoRenderConfig);
    ReleaseInterface(pIWMPRenderConfig);
    ReleaseInterface(pActivateObj);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::DiscardOcx
//
//  Synopsis:
//      In the apartment thread, initialize the Ocx, if this fails, then the
//      failure can be returned to the UI thread through some asynchronous call
//      later. This should only fail in extremely low resource conditions or if
//      the wrong version of WMP is present.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
DiscardOcx(
    void
    )
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);

    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(!m_targetState.m_isOcxCreated);
    Assert(m_pendingInternalState.m_isOcxCreated);

    IFC(RemoveOcx());

Cleanup:

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::RemoveOcx
//
//  Synopsis:
//      Removes the Ocx from memory and sets our internal state to reflect this
//      correctly
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
RemoveOcx(
    void
    )
{
    TRACEF(NULL);

    //
    // I don't use IFC here because I want to continue shutting down as much as
    // possible, even if I get errors.
    //
    IWMPControls    *pIControls = NULL;
    IOleObject      *pOleObj = NULL;

    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // Disconect the video presenter from the composition engine, update the
    // dummy presenter's size to match the video presenter, and force a
    // frame update
    //
    {
        DWORD width = m_presenterWrapper.DisplayWidth();
        DWORD height = m_presenterWrapper.DisplayHeight();

        if (width != 0 && height != 0)
        {
            //
            // Force the dummy surface renderer to do a frame update.
            //
            m_pDummyRenderer->ForceFrameUpdate(width, height);

            m_presenterWrapper.SetPresenter(NULL);

            //
            // Notify composition so that we'll display black. Since we've
            // released the video presenter, we'll hand out the dummy
            // surface renderer on the next composition pass.
            //
            m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
        }
        else
        {
            //
            // Even if a frame update wasn't necessary, we still have to
            // set the presenter to null to break circular dependencies
            //
            m_presenterWrapper.SetPresenter(NULL);
        }
    }

    //
    // Clear our state. We are about to throw everything away.
    //
    m_currentInternalState.Clear();
    m_pendingInternalState.Clear();
    m_actualState.Clear();

    m_didPreroll = false;
    m_didRaisePrerolled = false;

    //
    // Disconnect the event handler from ourselves. This prevents spurious
    // run-down messages occuring while we are shutting down.
    //
    if (m_pWmpEventHandler)
    {
        m_pWmpEventHandler->DisconnectStateEngine();
    }

    ReleaseInterface(m_pWmpEventHandler);

    //
    // Clear any action states that might have been floating around. This prevents
    // us getting surprised if we were in the middle of an arc when we closed media.
    //
    IGNORE_HR(m_nextSubArcMethodStack.Clear());

    if (m_pIWMPPlayer)
    {
        if (SUCCEEDED(m_pIWMPPlayer->get_controls(&pIControls)))
        {
            THR(pIControls->stop());
            ReleaseInterface(pIControls);
        }
        else
        {
            RIP("Couldn't get controls");
        }


        if (SUCCEEDED(m_pIWMPPlayer->QueryInterface(&pOleObj)))
        {
            THR(pOleObj->SetClientSite(NULL));
            THR(pOleObj->Close(OLECLOSE_NOSAVE));
            ReleaseInterface(pOleObj);
        }
        else
        {
            RIP("QueryInterface failed.");
        }
    }

    ReleaseInterface(m_pIWMPPlayer);

    if (m_pIConnectionPoint)
    {
        m_pIConnectionPoint->Unadvise(m_connectionPointAdvise);
    }

    ReleaseInterface(m_pIConnectionPoint);
    m_connectionPointAdvise = 0;

    return S_OK;
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::CheckPlayerVersion
//
//  Synopsis:
//      Checks to see whether the player version is what we expect it to be.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
CheckPlayerVersion(
    __in            IWMPPlayer          *pIWmpPlayer
    )
{
    HRESULT hr = S_OK;
    BSTR szVersion = NULL;
    UINT uiVersion = 0;
    UINT i = 0;
    TRACEFID(0, &hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    IFC(pIWmpPlayer->get_versionInfo(&szVersion));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Version is %ws",
        szVersion);

    while (szVersion[i] != NULL && szVersion[i] != '.')
    {
        uiVersion = (10 * uiVersion) + (szVersion[i] - '0');
        i++;
    }

    if (uiVersion < 10)
    {
        // video needs at least version 10
        IFC(WGXERR_AV_INVALIDWMPVERSION);
    }

Cleanup:

    SysFreeString(szVersion); // it's okay to pass NULL here

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::HandleStateChange
//
//  Synopsis:
//      This is the central function that is called whenever we hit a state
//      change in the apartment.
//
//      NOTE: Like anything called by the apartment manager, this function could
//      be called more than once for the same transition in some race conditions
//      it must be written to handle the case.
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
HandleStateChange(
    void
    )
{
    HRESULT hr = S_OK;
    int i = 0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_targetState.DumpPlayerState(m_uiID, "Start of HSC: m_targetState");
    m_targetInternalState.DumpPlayerState(m_uiID, "Start of HSC: m_targetInternalState");
    m_pendingInternalState.DumpPlayerState(m_uiID, "Start of HSC: m_pendingInternalState");
    m_currentInternalState.DumpPlayerState(m_uiID, "Start of HSC: m_currentInternalState");
    m_actualState.DumpPlayerState(m_uiID, "Start of HSC: m_actualState");

    IFC(DoPreemptiveTransitions());

    //
    // If the OCX has been torn down, then the stack should have been emptied
    //
    Assert(m_currentInternalState.m_isOcxCreated || m_nextSubArcMethodStack.IsEmpty());

    //
    // We may need to fire prerolled again. This can happen when:
    // 1. CWmpPlayer::Open("file1.wmv")
    // 2. CWmpPlayer::Open("file2.wmv")
    // 3. CWmpPlayer::Open("file1.wmv")
    //
    // If 2 and 3 occur in quick succession, then we won't have actually opened
    // file2, but we will have invalidated everything on SharedState. We need to
    // set all the state back and fire prerolled so that managed code will know
    // to call DrawVideo with a non-zero size.
    //
    IFC(RaisePrerolledIfNecessary());

    //
    // If we have a transition to continue, continue it
    //
    IFC(m_nextSubArcMethodStack.PopAndCall());

    //
    // We can't begin other transitions unless
    // a) the Ocx has not been torn down
    // b) the current URL is not null, and
    // c) we aren't waiting for a transition to complete
    //
    if (    m_currentInternalState.m_isOcxCreated
        &&  m_currentInternalState.m_url != NULL
        &&  m_nextSubArcMethodStack.IsEmpty())
    {
        //
        // We resync the target internal state to the target state
        // before beginning new arcs
        //
        IFC(m_targetState.Copy(&m_targetInternalState));
        m_targetInternalState.m_volume = m_volumeMask.ApplyAsMask(m_targetInternalState.m_volume);

        //
        // Start new transitions until we're told to wait for the stack to
        // unwind, or there are no new transitions to begin
        //
        while (m_nextSubArcMethodStack.IsEmpty() && m_currentInternalState != m_targetInternalState)
        {
            i++;
            AssertMsg(i < 20, "Infinite loop detected");
            Assert(m_currentInternalState == m_pendingInternalState);

            IFC(BeginNewTransition());
        }

        Assert(!m_nextSubArcMethodStack.IsEmpty() || m_targetInternalState == m_targetState || m_volumeMask.m_isValid);
    }

    m_targetState.DumpPlayerState(m_uiID, "End of HSC: m_targetState");
    m_targetInternalState.DumpPlayerState(m_uiID, "End of HSC: m_targetInternalState");
    m_pendingInternalState.DumpPlayerState(m_uiID, "End of HSC: m_pendingInternalState");
    m_currentInternalState.DumpPlayerState(m_uiID, "End of HSC: m_currentInternalState");
    m_actualState.DumpPlayerState(m_uiID, "End of HSC: m_actualState");

Cleanup:
    if (FAILED(hr))
    {
        LogAVDataM(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_STATEENGINE,
            "Couldn't handle state transition, hr = %x",
            hr);

        ErrorInTransition(hr);
    }

    EXPECT_SUCCESS(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::DoPreemptiveTransitions
//
//  Synopsis:
//      Possibly pre-empt pending transitions to start really important
//      transitions. This is called by HandleStateChange
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
DoPreemptiveTransitions(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    //
    // If we haven't created the OCX, we do that first
    //
    if (m_targetState.m_isOcxCreated != m_pendingInternalState.m_isOcxCreated)
    {
        if (m_targetState.m_isOcxCreated)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_STATEENGINE,
                "Preemptively chose to initialize the ocx");

            IFC(InitializeOcx());
        }
        else
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_STATEENGINE,
                "Preemptively chose to discard the ocx");

            IFC(DiscardOcx());
        }
    }

    //
    // Only do the other state transitions if we have an Ocx.
    //
    if (m_currentInternalState.m_isOcxCreated)
    {
        //
        // We pre-empt transitions for new urls, if the OCX has been created. We may have to stop this if WMP balks
        //
        if (!AreStringsEqual(m_targetState.m_url, m_pendingInternalState.m_url) && m_currentInternalState.m_isOcxCreated)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_STATEENGINE,
                "Preemptively chose to update the url");

            IFC(BeginUrlArc());
        }
    }

Cleanup:

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginNewTransition
//
//  Synopsis:
//      Start a new transition (or do nothing if the target state is the same
//      as pending state). This is called by HandleStateChange when
//      m_currentInternalState != m_targetInternalState
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginNewTransition(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    //
    // We process target states. There is no need to process shutdown changes,
    // url changes or ocx changes since we do those in DoPreemptiveTransitions.
    //
    if (m_currentInternalState.m_volume != m_targetInternalState.m_volume)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Chose to update the volume");

        IFC(DoVolumeArc());
    }
    else if (m_currentInternalState.m_balance != m_targetInternalState.m_balance)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Chose to update the balance");

        IFC(DoBalanceArc());
    }
    else if (m_currentInternalState.m_seekTo != m_targetInternalState.m_seekTo && m_currentInternalState.m_actionState != ActionState::Play)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Chose to seek");

        IFC(BeginSeekToAndScrubArc());
    }
    else if (m_currentInternalState.m_actionState != m_targetInternalState.m_actionState)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Chose to update the playstate");

        IFC(BeginActionStateArc());
    }
    else if (m_currentInternalState.m_seekTo != m_targetInternalState.m_seekTo)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Chose to seek");

        IFC(BeginSeekToAndScrubArc());
    }
    else if (m_currentInternalState.m_rate != m_targetInternalState.m_rate)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Chose to update the rate");

        IFC(DoRateArc());
    }
    else
    {
        RIP("Didn't find anything to update even though target != current");
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginActionStateArc
//
//  Synopsis:
//      Start an action state transition. This method just dispatches to the
//      appropriate handler.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginActionStateArc(
    void
    )
{
    static const struct
    {
        ActionState::Enum   currentState;
        ActionState::Enum   pendingState;
        SubArcMethod        actionMethod;
    }
    sc_stateTransitionTable[] =
    {
        //
        // Current State        Transitioning To    Action to perform
        //

        { ActionState::Stop,    ActionState::Pause, &CWmpStateEngine::BeginStopToPauseArc   },
        { ActionState::Stop,    ActionState::Play,  &CWmpStateEngine::BeginStopToPlayArc    },

        { ActionState::Pause,   ActionState::Stop,  &CWmpStateEngine::BeginPauseToStopArc   },
        { ActionState::Pause,   ActionState::Play,  &CWmpStateEngine::BeginPauseToPlayArc   },

        { ActionState::Play,    ActionState::Stop,  &CWmpStateEngine::BeginPlayToStopArc    },
        { ActionState::Play,    ActionState::Pause, &CWmpStateEngine::BeginPlayToPauseArc   },
    };

    HRESULT                     hr = S_OK;
    int                         i = 0;

    TRACEF(&hr);

    m_pendingInternalState.m_actionState = m_targetInternalState.m_actionState;
    Assert(m_pendingInternalState.m_actionState != m_currentInternalState.m_actionState);
    Assert(m_targetInternalState.m_actionState != m_currentInternalState.m_actionState);

    if (!m_isMediaEnded)
    {
        //
        // Keep track of the last ActionState so that we can make sure we don't hit a
        // state that we didn't expect.
        //
        m_lastActionState = m_actualState.m_actionState;

        for(i = 0; i < COUNTOF(sc_stateTransitionTable); i++)
        {
            if (   sc_stateTransitionTable[i].currentState == m_currentInternalState.m_actionState
                && sc_stateTransitionTable[i].pendingState == m_pendingInternalState.m_actionState)
            {
                LogAVDataM(
                    AVTRACE_LEVEL_INFO,
                    AVCOMP_STATEENGINE,
                    "Chose row i = %d, performing action",
                    i);

                if (sc_stateTransitionTable[i].actionMethod != NULL)
                {
                    IFC((this->*(sc_stateTransitionTable[i].actionMethod))());
                }

                break;
            }
        }

        if (i >= COUNTOF(sc_stateTransitionTable))
        {
            LogAVDataM(
                AVTRACE_LEVEL_ERROR,
                AVCOMP_STATEENGINE,
                "Unable to handle state arc [current = %!ActionState!, transition = %!ActionState!, player = %!ActionState!]",
                m_currentInternalState.m_actionState,
                m_pendingInternalState.m_actionState,
                m_actualState.m_actionState);

            RIP("Unable to handle state arc.");
            IFC(E_UNEXPECTED);
        }
    }
    //
    // If the media has finished, we just pretend that we've reached the state
    // we were trying for. We won't do anything until we're asked to seek back.
    //
    else
    {
        m_currentInternalState.m_actionState = m_pendingInternalState.m_actionState;
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Arc_WaitForActionState
//
//  Synopsis:
//      Wait for an action state change
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
Arc_WaitForActionState(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    if (m_isMediaEnded)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Media has ended, so we pretend to reach the requested action state");

        m_pendingInternalState.m_actionState = m_targetInternalState.m_actionState;
        m_currentInternalState.m_actionState = m_targetInternalState.m_actionState;

        IFC(m_nextSubArcMethodStack.PopAndCall());
    }
    //
    // If we haven't changed action states, even though we're waiting for a
    // change, then we reschedule ourselves
    //
    else if (m_actualState.m_actionState == m_lastActionState && m_lastActionState != m_waitForActionState)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Arc_WaitForActionState: current action state is %!ActionState!, but we're waiting for %!ActionState!.",
            m_actualState.m_actionState,
            m_waitForActionState);

        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));
    }
    //
    // If we haven't changed render states, even though we're waiting for a
    // change, then we reschedule ourselves
    //
    else if (  m_pSharedState->GetHasVideo()
            && (   (m_waitForActionState == ActionState::Play && !m_isEvrClockRunning)
                || (m_waitForActionState != ActionState::Play && m_isEvrClockRunning)))
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Arc_WaitForActionState: m_isEvrClockRunning is %d, but we're waiting for state %d.",
            m_isEvrClockRunning,
            m_waitForActionState);

        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));
    }
    //
    // if WMP is currently buffering, we reschedule ourselves because we need
    // to wait until WMP is not buffering
    //
    else if (m_actualState.m_actionState == ActionState::Buffer)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Arc_WaitForActionState: we're buffering");

        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));
    }
    //
    // If we're not at the action state we were waiting for,
    // then something is really wrong
    //
    else if (m_actualState.m_actionState != m_waitForActionState)
    {
        RIP("Unexpected actual action state");
        IFC(E_UNEXPECTED);
    }
    //
    // Otherwise we process the next sub-arc.
    //
    else
    {
        m_lastActionState = m_actualState.m_actionState;

        IFC(m_nextSubArcMethodStack.PopAndCall());
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PostSeek
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
PostSeek(
    void
    )
{
    TRACEF(NULL);

    //
    // We only reset the target seek to if it hasn't changed
    //
    if (m_targetState.m_seekTo == m_pendingInternalState.m_seekTo)
    {
        m_targetState.m_seekTo.m_isValid = false;
    }

    //
    // PostSeek may be called even though we didn't seek. For example, it
    // will be called if we ignore a seek request because we're already
    // within SEEK_GRANULARITY of the requested position.
    // If we've really done a seek, we're no longer finished playing media.
    // We set some properties to trigger media to restart
    //
    if (m_isMediaEnded && m_actualState.m_seekTo.m_isValid)
    {
        Assert(m_actualState.m_actionState == ActionState::Stop);
        m_isMediaEnded = false;
        m_currentInternalState.m_actionState = ActionState::Stop;
    }

    //
    // We indicate that we have finished our transition by resetting all seekto's to false.
    // seeking is special in that WMP doesn't (necessarily) remain at that position, so it
    // doesn't make sense to keep around the last seeked position
    //
    m_pendingInternalState.m_seekTo.m_isValid = false;
    m_currentInternalState.m_seekTo.m_isValid = false;
    m_targetInternalState.m_seekTo.m_isValid  = false;
    m_actualState.m_seekTo.m_isValid          = false;

    RRETURN(S_OK);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginStopToPauseArc
//
//  Synopsis:
//      This function handles the arc going from stop to pause, this must
//      transition through play. It is possible that this transition could cause
//      audio glitches so we mute the volume while we are playing.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginStopToPauseArc(
    void
    )
{
    HRESULT         hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Stop);
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);
    Assert(!m_pSharedState->GetHasVideo() || !m_isEvrClockRunning);

    if (m_isScrubbingEnabled)
    {
        //
        // Begin scrubbing or not and wait for it to complete.
        // StopToPauseArc_Done will be called at the end
        //
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ContinueStopToPauseArc));

        IFC(BeginScrubArc());
    }
    else
    {
        IFC(BeginNoScrubStopToPauseArc());
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginNoScrubStopToPauseArc
//
//  Synopsis:
//      Handles the stop to pause arc for the case where no scrubbing is desired
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginNoScrubStopToPauseArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;
    TRACEF(&hr);

    //
    // Save the current position so that we can restore it when we
    // finally reach the pause state.
    //
    m_targetInternalState.m_seekTo = 0;

    //
    // Mute so no glitches will be heard
    //
    m_targetInternalState.m_volume = 0;
    IFC(DoVolumeArc());

    //
    // Don't show frames to avoid visual glitches. We could show
    // a frame without any cost, but then we'd break the "not scrubbing"
    //
    m_presenterWrapper.EndScrub();
    m_presenterWrapper.BeginStopToPauseFreeze();
    m_needFlushWhenEndingFreeze = false;

    //
    // Wait until WMP has reached the play state,
    // then continue the scrubbing arc by calling ScrubArc_Pause
    //
    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(IsSupportedWmpReturn(pIControls->play()));
    m_waitForActionState = ActionState::Play;
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::NoScrubStopToPauseArc_Pause));
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));

Cleanup:
    ReleaseInterface(pIControls);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::NoScrubStopToPauseArc_Pause
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
NoScrubStopToPauseArc_Pause(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);


    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

    //
    // We may actually encounter media ended for very short media
    //
    if (m_isMediaEnded)
    {
        Assert(m_actualState.m_actionState == ActionState::Stop);

        //
        // The current action state will have been set to the pending action
        // state when the media finished.
        //
        Assert(m_currentInternalState.m_actionState == ActionState::Pause);

        //
        // We handle media ending by just pretending that we've
        // reached the pause state
        //
        IFC(StopToPauseArc_Done());
    }
    else
    {
        Assert(m_actualState.m_actionState == ActionState::Play);
        Assert(!m_pSharedState->GetHasVideo() || m_isEvrClockRunning);

        if (m_pSharedState->GetCanPause())
        {
            //
            // Wait for WMP to pause,
            // then continue the arc by calling NoScrubStopToPauseArc_Seek
            //
            IFC(m_pIWMPPlayer->get_controls(&pIControls));
            IFC(IsSupportedWmpReturn(pIControls->pause()));
            m_waitForActionState = ActionState::Pause;
            IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::NoScrubStopToPauseArc_Seek));
            IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));
        }
        else
        {
            //
            // If we can't pause, then we're done
            //
            IFC(StopToPauseArc_Done());
        }
    }


Cleanup:
    ReleaseInterface(pIControls);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::NoScrubStopToPauseArc_Seek
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
NoScrubStopToPauseArc_Seek(
    void
    )
{
    HRESULT         hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Pause);
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);
    Assert(!m_pSharedState->GetHasVideo() || !m_isEvrClockRunning);

    if (m_canSeek)
    {
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::StopToPauseArc_Done));

        //
        // We seeking using BeginSeekToArc (not BeginSeekToAndScrubArc -
        // that would lead to infinite recursion). When the seek is complete
        // we'll be done
        //
        IFC(BeginSeekToArc());
    }
    else
    {
        //
        // If we can't seek, then we can't scrub and we're finished our arc
        //
        IFC(StopToPauseArc_Done());
    }

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::StopToPauseArc_Done
//
//  Synopsis:
//      This function handles the end of the stop to pause arc
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
ContinueStopToPauseArc(
    void
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    if (m_currentInternalState.m_actionState == ActionState::Stop)
    {
        IFC(BeginNoScrubStopToPauseArc());
    }
    else
    {
        IFC(StopToPauseArc_Done());
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::StopToPauseArc_Done
//
//  Synopsis:
//      This function handles the end of the stop to pause arc
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
StopToPauseArc_Done(
    void
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

    //
    // If we can pause, then this is normal media and we should be in a paused
    // state
    //
    if (m_isMediaEnded)
    {
        Assert(m_actualState.m_actionState == ActionState::Stop);

        //
        // The current internal action state will have been set to pause when
        // the media ended.
        //
        Assert(m_currentInternalState.m_actionState == ActionState::Pause);
    }
    else if (m_pSharedState->GetCanPause())
    {
        Assert(m_actualState.m_actionState == ActionState::Pause);
        Assert(m_currentInternalState.m_actionState == ActionState::Stop);
    }
    //
    // If we can't pause, then this must be live streaming media. We are in a
    // fake pause state.
    //
    else
    {
        Assert(m_actualState.m_actionState == ActionState::Play);
        Assert(m_currentInternalState.m_actionState == ActionState::Stop);

        //
        // The volume is currently 0, but if we don't mask it to 0,
        // it will be restored
        //
        m_volumeMask = 0;

        //
        // tell the sample scheduler that we are now paused.
        // Scrubbing mode is incompatible with fake pause, so we end the
        // scrub (if there was one)
        //
        //
        m_presenterWrapper.EndScrub();
        m_presenterWrapper.BeginFakePause();
    }

    if (m_didSeek)
    {
        m_needFlushWhenEndingFreeze = true;
    }

    m_currentInternalState.m_actionState = ActionState::Pause;

    //
    // Tell the managed layer that we've finished prerolling
    //
    m_didPreroll = true;
    IFC(RaisePrerolledIfNecessary());

    //
    // No one should be waiting for us to complete
    //
    Assert(m_nextSubArcMethodStack.IsEmpty());

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginStopToPlayArc
//
//  Synopsis:
//      Start transitioning from the stop state to the play state
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginStopToPlayArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Stop);
    Assert(m_currentInternalState.m_actionState == ActionState::Stop);
    Assert(m_pendingInternalState.m_actionState == ActionState::Play);

    //
    // Since we're playing, we're no longer showing the cached scrub position,
    // so we need to invalidate it.
    //
    m_cachedScrubPosition.m_isValid = false;

    //
    // Tell the presenter to release any samples it may have been holding for
    // scrubbing purposes
    //
    m_presenterWrapper.EndScrub();
    m_presenterWrapper.EndStopToPauseFreeze(m_needFlushWhenEndingFreeze);

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(IsSupportedWmpReturn(pIControls->play()));

    //
    // Wait for WMP to play, then call StopToPlayArc_Done
    //
    m_waitForActionState = ActionState::Play;
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::StopToPlayArc_Done));
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));

Cleanup:
    ReleaseInterface(pIControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::StopToPlayArc_Done
//
//  Synopsis:
//      This function handles the end of the stop to play arc
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
StopToPlayArc_Done(
    void
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_pendingInternalState.m_actionState == ActionState::Play);
    if (!m_isMediaEnded)
    {
        Assert(m_actualState.m_actionState == ActionState::Play);
        Assert(m_currentInternalState.m_actionState == ActionState::Stop);
    }

    m_currentInternalState.m_actionState = ActionState::Play;

    m_didPreroll = true;
    IFC(RaisePrerolledIfNecessary());

    //
    // No one should be waiting for us to complete
    //
    Assert(m_nextSubArcMethodStack.IsEmpty());

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginPauseToStopArc
//
//  Synopsis:
//      Start transitioning from the pause state to the stop state
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginPauseToStopArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_currentInternalState.m_actionState == ActionState::Pause);
    Assert(m_pendingInternalState.m_actionState == ActionState::Stop);

    //
    // If we can pause than our actual action state is pause, otherwise
    // our actual action state is play
    //
    Assert(    (m_pSharedState->GetCanPause() && m_actualState.m_actionState == ActionState::Pause)
            || (!m_pSharedState->GetCanPause() && m_actualState.m_actionState == ActionState::Play));

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(IsSupportedWmpReturn(pIControls->stop()));

    //
    // Wait for us to reach the stopped state, then call Arc_ActionStateComplete
    //
    m_waitForActionState = ActionState::Stop;
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_ActionStateComplete));
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));

Cleanup:
    ReleaseInterface(pIControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginPauseToPlayArc
//
//  Synopsis:
//      Delegate to RealPauseToPlayArc_Play or FakePauseToPlayArc_Done
//      depending on whether or not this media can pause
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginPauseToPlayArc(
    void
    )
{
    //
    // Since we're playing, we're no longer showing the cached scrub position,
    // so we need to invalidate it.
    //
    m_cachedScrubPosition.m_isValid = false;

    if (m_pSharedState->GetCanPause())
    {
        RRETURN(RealPauseToPlayArc_Play());
    }
    else
    {
        RRETURN(FakePauseToPlayArc_Done());
    }
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::RealPauseToPlayArc_Play
//
//  Synopsis:
//      Start transitioning from the pause state to the play state
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
RealPauseToPlayArc_Play(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Pause);
    Assert(m_currentInternalState.m_actionState == ActionState::Pause);
    Assert(m_pendingInternalState.m_actionState == ActionState::Play);

    //
    // Tell the presenter to release any samples it may have been holding for
    // scrubbing purposes
    //
    m_presenterWrapper.EndScrub();
    m_presenterWrapper.EndStopToPauseFreeze(m_needFlushWhenEndingFreeze);

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(IsSupportedWmpReturn(pIControls->play()));

    //
    // Wait for WMP to reach the play state, then call Arc_ActionStateComplete
    //
    m_waitForActionState = ActionState::Play;
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_ActionStateComplete));
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));

Cleanup:
    ReleaseInterface(pIControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::FakePauseToPlayArc_Done
//
//  Synopsis:
//      Transition from fake pause to play. This means un-muting the volume and
//      telling the presenter to resume showing frames.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
FakePauseToPlayArc_Done(
    void
    )
{
    HRESULT         hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Play);
    Assert(m_currentInternalState.m_actionState == ActionState::Pause);
    Assert(m_pendingInternalState.m_actionState == ActionState::Play);

    //
    // restore the previous volume
    //
    m_volumeMask.m_isValid = false;
    m_targetInternalState.m_volume = m_targetState.m_volume;

    IFC(DoVolumeArc());

    //
    // Tell the presenter that we are out of frame freezing mode
    //
    m_presenterWrapper.EndScrub();
    m_presenterWrapper.EndFakePause();
    m_presenterWrapper.EndStopToPauseFreeze(m_needFlushWhenEndingFreeze);

    //
    // Indicate that we are done this arc
    //
    m_currentInternalState.m_actionState = ActionState::Play;

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpStateEngine::
BeginScrubArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;
    double          position    = 0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Pause || m_actualState.m_actionState == ActionState::Stop);
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

    //
    // We don't assert m_pSharedState->GetHasVideo because scrubbing may be invoked as the
    // initial transition, before we know whether or not we have video
    //
    Assert(!m_pSharedState->GetHasVideo() || !m_isEvrClockRunning);

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(pIControls->get_currentPosition(&position));

    if (m_cachedScrubPosition.m_isValid && fabs(m_cachedScrubPosition.m_value - position) < SEEK_GRANULARITY)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Not scrubbing because we're already showing the correct frame");

        IFC(m_nextSubArcMethodStack.PopAndCall());
    }
    else
    {
        //
        // Save the current position so that we can restore it when we
        // finally reach the pause state.
        //
        m_targetInternalState.m_seekTo = position;

        //
        // Cache the last position scrubbed so that we don't repeat a scrub twice
        //
        m_cachedScrubPosition = position;

        //
        // Mute so no glitches will be heard
        //
        m_targetInternalState.m_volume = 0;
        IFC(DoVolumeArc());

        m_didReceiveScrubSample = false;

        //
        // Put the presenter in scrubbing mode, take it out of
        // show no samples mode, which it may have been in
        //
        m_presenterWrapper.EndStopToPauseFreeze(m_needFlushWhenEndingFreeze);
        m_presenterWrapper.BeginScrub();

        IFC(IsSupportedWmpReturn(pIControls->play()));

        //
        // ScrubArc_Pause won't actually do anything until we have the scrub sample
        //
        IFC(CWmpStateEngine::ScrubArc_Pause());
    }

Cleanup:
    ReleaseInterface(pIControls);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpStateEngine::
ScrubArc_Pause(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;
    Assert(m_stateThreadId == GetCurrentThreadId());

    TRACEF(&hr);

    if (m_isMediaEnded)
    {
        //
        // If the media ended then we're done scrubbing.
        // m_targetInternalState.m_seekTo is set so we can
        // resume our original playstate/position. We don't
        // attempt to play -> pause -> seek again because
        // that would likely throw us into an infinite loop.
        //
        IFC(ScrubArc_Seek());
    }
    else if (!m_didReceiveScrubSample)
    {
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ScrubArc_Pause));
    }
    else if (m_actualState.m_actionState == ActionState::Buffer)
    {
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ScrubArc_Pause));
    }
    else
    {
        Assert(m_actualState.m_actionState == ActionState::Play);
        Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

        if (m_pSharedState->GetCanPause())
        {
            //
            // Wait for WMP to pause,
            // then continue scrubbing arc by calling ScrubArc_Seek
            //
            IFC(m_pIWMPPlayer->get_controls(&pIControls));
            IFC(IsSupportedWmpReturn(pIControls->pause()));
            m_waitForActionState = ActionState::Pause;
            IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ScrubArc_Seek));
            IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));
        }
        else
        {
            //
            // If we can't pause, then we're done scrubbing
            //
            IFC(m_nextSubArcMethodStack.PopAndCall());
        }
    }

Cleanup:
    ReleaseInterface(pIControls);
    RRETURN(hr);
}

HRESULT
CWmpStateEngine::
ScrubArc_Seek(
    void
    )
{
    HRESULT         hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);
    Assert(m_actualState.m_actionState == (m_isMediaEnded? ActionState::Stop : ActionState::Pause ));

    //
    // We don't assert m_pSharedState->GetHasVideo because scrubbing may be invoked as the
    // initial transition, before we know whether or not we have video
    //
    Assert(!m_pSharedState->GetHasVideo() || !m_isEvrClockRunning);

    if (m_canSeek)
    {
        //
        // We seeking using BeginSeekToArc (not BeginSeekToAndScrubArc -
        // that would lead to infinite recursion). When the seek is complete
        // we'll be done scrubbing
        //
        IFC(BeginSeekToArc());
    }
    else
    {
        //
        // If we can't seek, then we're done scrubbing.
        // If there is someone waiting for scrubbing to complete, call them.
        //
        IFC(m_nextSubArcMethodStack.PopAndCall());
    }

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginPlayToStopArc
//
//  Synopsis:
//      Start transitioning from the play state to the stop state
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginPlayToStopArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Play);
    Assert(m_currentInternalState.m_actionState == ActionState::Play);
    Assert(m_pendingInternalState.m_actionState == ActionState::Stop);

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(IsSupportedWmpReturn(pIControls->stop()));

    //
    // Wait for WMP to stop, then call Arc_ActionStateComplete
    //
    m_waitForActionState = ActionState::Stop;
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_ActionStateComplete));
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::Arc_WaitForActionState));


Cleanup:
    ReleaseInterface(pIControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::BeginPlayToPauseArc
//
//  Synopsis:
//      Start transitioning from the play state to the pause state.
//      Delegates to either PlayToRealPauseArc_Pause or PlayToFakePauseArc_Done
//      depending on whether or not this media can pause.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginPlayToPauseArc(
    void
    )
{
    if (m_pSharedState->GetCanPause())
    {
        RRETURN(PlayToRealPauseArc_Pause());
    }
    else
    {
        RRETURN(PlayToFakePauseArc_Done());
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PlayToRealPauseArc_Pause
//
//  Synopsis:
//      Start transitioning from the play state to pause state. We tell WMP to
//      pause and wait for it to complete.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
PlayToRealPauseArc_Pause(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Play);
    Assert(m_currentInternalState.m_actionState == ActionState::Play);
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(IsSupportedWmpReturn(pIControls->pause()));

    //
    // Wait for WMP to pause, then call PlayToRealPauseArc_Done
    //
    m_waitForActionState = ActionState::Pause;
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::PlayToRealPauseArc_Done));
    IFC(Arc_WaitForActionState());

Cleanup:
    ReleaseInterface(pIControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpStateEngine::
PlayToRealPauseArc_Done(
    void
    )
{
    HRESULT         hr = S_OK;
    double          position = 0.0;
    IWMPControls    *pIControls = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Pause);
    Assert(m_currentInternalState.m_actionState == ActionState::Play);
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

    IFC(m_pIWMPPlayer->get_controls(&pIControls));
    IFC(pIControls->get_currentPosition(&position));

    m_cachedScrubPosition = position;

    IFC(Arc_ActionStateComplete());

Cleanup:
    ReleaseInterface(pIControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PlayToFakePauseArc_Done
//
//  Synopsis:
//      Transition from play to fake pause. This just involves muting and
//      setting the pause time on the presenter, so this completes synchronously
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
PlayToFakePauseArc_Done(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_actualState.m_actionState == ActionState::Play);
    Assert(m_currentInternalState.m_actionState == ActionState::Play);
    Assert(m_pendingInternalState.m_actionState == ActionState::Pause);

    //
    // Fake pause is just masking the volume to 0 and telling the surface
    // renderer not to show new frames
    //
    Assert(!m_volumeMask.m_isValid);
    m_volumeMask = 0;
    m_targetInternalState.m_volume = 0;
    IFC(DoVolumeArc());

    //
    // Scrubbing mode is incompatible with fake pause, so we end the
    // scrub (if there was one)
    //
    m_presenterWrapper.EndScrub();
    m_presenterWrapper.BeginFakePause();

    //
    // Indicate that we are done this arc
    //
    m_currentInternalState.m_actionState = ActionState::Pause;

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::Arc_ActionStateComplete
//
//  Synopsis:
//      This function handles the arcs that don't need any special
//      post-processing
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
Arc_ActionStateComplete(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_waitForActionState == m_pendingInternalState.m_actionState);

    //
    // If we've been waiting for Play, then it's possible we've reached
    // Play, then immediately ended. This has been seen to happen sometimes
    // while attempting to play images. We handle this by pretending to
    // be in the play state.
    //
    if (m_waitForActionState == ActionState::Play && m_isMediaEnded)
    {
        Assert(m_actualState.m_actionState == ActionState::Stop);
    }
    else
    {
        Assert(m_actualState.m_actionState == m_waitForActionState);
    }

    m_currentInternalState.m_actionState = m_pendingInternalState.m_actionState;

    //
    // If anyone was waiting for the action state to complete, then we call them
    //
    IFC(m_nextSubArcMethodStack.PopAndCall());

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::UpdateCanPause
//
//  Synopsis:
//      Decide whether the given media can be paused. Stores the result in
//      m_pSharedState
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
UpdateCanPause(
    void
    )
{
    HRESULT hr = S_OK;
    IWMPControls *pControls = NULL;
    BSTR bstr = NULL;
    VARIANT_BOOL canPause = VARIANT_FALSE;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // m_canPause is only guaranteed to be valid if m_didPreroll is true.
    // Otherwise we need to update it here. We can only do that if our actual
    // state is Play
    //
    if (!m_didPreroll)
    {
        Assert(m_actualState.m_actionState == ActionState::Play);

        IFC(m_pIWMPPlayer->get_controls(&pControls));

        bstr = SysAllocString(L"pause");

        IFCOOM(bstr);

        IFC(pControls->get_isAvailable(bstr, &canPause));

        SetCanPause(!!canPause);
    }
    else
    {
        SetCanPause(m_canPause);
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "CanPause=%d",
        m_pSharedState->GetCanPause());

    hr = S_OK;

Cleanup:

    SysFreeString(bstr);
    ReleaseInterface(pControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::InternalCanSeek
//
//  Synopsis:
//      Decide whether the given media can be seeked. Stores the result in
//      m_canSeek
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
InternalCanSeek(
    void
    )
{
    HRESULT hr = S_OK;
    IWMPControls *pControls = NULL;
    BSTR bstr = NULL;
    VARIANT_BOOL canSeek = VARIANT_FALSE;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    IFC(m_pIWMPPlayer->get_controls(&pControls));

    bstr = SysAllocString(L"currentPosition");

    IFCOOM(bstr);

    IFC(pControls->get_isAvailable(bstr, &canSeek));

    m_canSeek = !!canSeek;

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "CanSeek=%d",
        m_canSeek);

    hr = S_OK;

Cleanup:

    SysFreeString(bstr);
    ReleaseInterface(pControls);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

HRESULT
CWmpStateEngine::
UpdateHasAudioForWmp11(
    void
    )
{
    HRESULT hr = S_OK;
    IWMPMedia   *pMedia = NULL;
    IWMPMedia3  *pMedia3 = NULL;
    BSTR        type = NULL;
    long        items = 0 ;
    VARIANT     var = { 0 };
    bool        hasAudio = false;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    VariantInit(&var);

    if (m_useRenderConfig)
    {
        IFC(m_pIWMPPlayer->get_currentMedia(&pMedia));
        IFC(pMedia->QueryInterface(&pMedia3));

        IFC(SysAllocStringCheck(L"Streams", &type));

        IFC(pMedia3->getAttributeCountByType(type, NULL, &items));

        for (long i = 0; i < items; i++)
        {
            IFC(VariantClear(&var));

            IFC(pMedia3->getItemInfoByType(type, NULL, i, &var));

            if (var.vt != VT_BSTR)
            {
                RIP("Expected bstr");
                IFC(E_FAIL);
            }

            if (wcscmp(var.bstrVal, L"audio") == 0)
            {
                hasAudio = true;
                break;
            }
        }

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "HasAudio (as determined through IWMPMedia3) = %!bool!",
            hasAudio);

        SetHasAudio(hasAudio);
    }

    hr = S_OK;

Cleanup:
    ReleaseInterface(pMedia);
    ReleaseInterface(pMedia3);

    IGNORE_HR(VariantClear(&var));

    SysFreeString(type);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Function:
//      DoVolumeArc
//
//  Synopsis:
//      Sets the player volume (internally) for real. We use WMP units to save
//      on conversions when the volume is stored.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
DoVolumeArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPSettings    *pISettings  = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_pendingInternalState.m_volume = m_targetInternalState.m_volume;

    IFC(
        m_pIWMPPlayer->QueryInterface(
                            __uuidof(IWMPSettings),
                            reinterpret_cast<void**>(&pISettings)));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Setting volume to %d",
        m_pendingInternalState.m_volume);

    IFC(pISettings->put_volume(m_pendingInternalState.m_volume));

    //
    // Indicate that we finished the arc
    //
    m_currentInternalState.m_volume = m_pendingInternalState.m_volume;
    m_actualState.m_volume = m_pendingInternalState.m_volume;

    hr = S_OK;

Cleanup:
    ReleaseInterface(pISettings);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Function:
//      DoBalanceArc
//
//  Synopsis:
//      Sets the player balance (internally) for real. We use WMP units to save
//      on conversions when the balance is stored.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
DoBalanceArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPSettings    *pISettings  = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_pendingInternalState.m_balance = m_targetInternalState.m_balance;

    IFC(
        m_pIWMPPlayer->QueryInterface(
                            __uuidof(IWMPSettings),
                            reinterpret_cast<void**>(&pISettings)));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Setting balance to %f",
        m_pendingInternalState.m_balance);

    IFC(pISettings->put_balance(m_pendingInternalState.m_balance));

    //
    // Indicate that we finished the arc
    //
    m_currentInternalState.m_balance = m_pendingInternalState.m_balance;
    m_actualState.m_balance = m_pendingInternalState.m_balance;

    hr = S_OK;

Cleanup:
    ReleaseInterface(pISettings);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Function:
//      DoRateArc
//
//  Synopsis:
//      Sets the player balance (internally) for real. We use WMP units to save
//      on conversions when the balance is stored.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
DoRateArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPSettings    *pISettings  = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    m_pendingInternalState.m_rate = m_targetInternalState.m_rate;

    IFC(
        m_pIWMPPlayer->QueryInterface(
                            __uuidof(IWMPSettings),
                            reinterpret_cast<void**>(&pISettings)));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Setting rate to %f",
        m_pendingInternalState.m_rate);

    IFC(IsSupportedWmpReturn(pISettings->put_rate(m_pendingInternalState.m_rate)));


    //
    // Indicate that we finished the arc
    //
    m_currentInternalState.m_rate = m_pendingInternalState.m_rate;
    m_actualState.m_rate = m_pendingInternalState.m_rate;

    hr = S_OK;

Cleanup:
    ReleaseInterface(pISettings);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Function:
//      BeginUrlArc
//
//  Synopsis:
//      Updates the url. This does not complete synchronously. We assume that we
//      are finished when we receive the wmposPlaylistOpenNoMedia event.
//      However, we don't find about all properties of the media until we
//      actually start playing it.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginUrlArc(
    void
    )
{
    HRESULT         hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    BSTR            bstrUrl = NULL;

    IFC(CopyHeapString(m_targetState.m_url, &m_pendingInternalState.m_url));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "opening %S",
        m_pendingInternalState.m_url);

    IFC(SysAllocStringCheck(m_pendingInternalState.m_url, &bstrUrl));

    IFC(IsSupportedWmpReturn(m_pIWMPPlayer->put_URL(bstrUrl)));

    //
    // When we have a new Url, the media length might change, so we need to
    // throw away our cached value.
    //
    SetMediaLength(0.0);

    m_cachedScrubPosition.m_isValid = false;

    //
    // Setting a new url resets the play state, sets the rate to 1,
    // and abandons pending transitions. We reset the pending and current
    // states so that we'll notice that we need to change these again.
    // We also need clear m_nextSubArcMethodStack to completely abandon
    // any arcs that we are in the middle of.
    //
    m_currentInternalState.m_actionState = ActionState::Stop;
    m_pendingInternalState.m_actionState = ActionState::Stop;

    m_currentInternalState.m_seekTo.m_isValid = false;
    m_pendingInternalState.m_seekTo.m_isValid = false;
    m_actualState.m_seekTo.m_isValid = false;

    m_currentInternalState.m_rate = 1;
    m_pendingInternalState.m_rate = 1;
    m_actualState.m_rate = 1;

    IFC(m_nextSubArcMethodStack.Clear());

    //
    // Opening a new url throws away the graph, so we need to reset the
    // evr state
    //
    m_isEvrClockRunning = false;

    //
    // If we're opening a different URL we know longer consider
    // WMP to be in an ended state
    //
    m_isMediaEnded = false;

    //
    // The new media may have different characteristics than the old one.
    //
    SetHasAudio(false);
    SetHasVideo(false);
    SetIsBuffering(false);
    SetCanPause(false);

    m_didPreroll = false;
    m_didRaisePrerolled = false;

    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ContinueUrlArc));

Cleanup:

    SysFreeString(bstrUrl);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Function:
//      ContinueUrlArc
//
//  Synopsis:
//      Handles the notification that the URL has been updated
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
ContinueUrlArc(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // If we've opened the file, then make a note of it
    //
    if (AreStringsEqual(m_actualState.m_url, m_pendingInternalState.m_url))
    {
        IFC(CopyHeapString(m_pendingInternalState.m_url, &m_currentInternalState.m_url));

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "opened %S",
            m_actualState.m_url);

        //
        // No other methods should be scheduled during a URL arc
        //
        Assert(m_nextSubArcMethodStack.IsEmpty());
    }
    //
    // Otherwise, schedule ourselves again
    //
    else
    {
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ContinueUrlArc));
    }

Cleanup:

    RRETURN(S_OK);
}


//+-----------------------------------------------------------------------------
//
//  Function:
//      BeginSeekToArc
//
//  Synopsis:
//      Starts seeking
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginSeekToArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls   *pControls = NULL;
    double          prevPosition = 0;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());
    Assert(m_targetInternalState.m_seekTo.m_isValid);

    //
    // This may be true if we started a seek while stopped previously. In that
    // case we wouldn't get a PositionChange notification until we started
    // playing, so we don't wait for it. We need to reset it here so we will
    // wait for the seek to complete if necessary and so that we don't mistake
    // the stale position for the new position to which we are seeking.
    //
    m_actualState.m_seekTo.m_isValid = false;

    m_pendingInternalState.m_seekTo = m_targetInternalState.m_seekTo;
    m_didSeek = false;

    if (!m_canSeek)
    {
        LogAVDataM(
            AVTRACE_LEVEL_WARNING,
            AVCOMP_STATEENGINE,
            "Ignoring seek to unseekable media");

        IFC(PostSeek());

        IFC(m_nextSubArcMethodStack.PopAndCall());
    }
    else
    {
        IFC(m_pIWMPPlayer->get_controls(&pControls));

        if (!m_isMediaEnded)
        {
            IFC(IsSupportedWmpReturn(pControls->get_currentPosition(&prevPosition)));
        }
        else
        {
            prevPosition = m_mediaLength;

            //
            // For some reason WMP resets the rate to 1. We note this so that we can
            // ping WMP again to set the rate to whatever is desired.
            //
            m_actualState.m_rate = 1;
            m_currentInternalState.m_rate = 1;
            m_pendingInternalState.m_rate = 1;

            m_actualState.m_actionState = ActionState::Stop;
            m_currentInternalState.m_actionState = ActionState::Stop;
            m_pendingInternalState.m_actionState = ActionState::Stop;
        }

        //
        // Don't process seeks that are within our threshold
        //
        if (fabs(m_pendingInternalState.m_seekTo.m_value - prevPosition) < SEEK_GRANULARITY)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_STATEENGINE,
                "Not seeking to position %.4f because WMP is already at position %.4f",
                m_pendingInternalState.m_seekTo.m_value,
                prevPosition);

            IFC(PostSeek());

            IFC(m_nextSubArcMethodStack.PopAndCall());
        }
        else
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_STATEENGINE,
                "Seeking to position %.4f position",
                m_pendingInternalState.m_seekTo.m_value);

            IFC(pControls->put_currentPosition(m_pendingInternalState.m_seekTo.m_value));

            if (FAILED(IsSupportedWmpReturn(hr)))
            {
                LogAVDataM(
                    AVTRACE_LEVEL_ERROR,
                    AVCOMP_STATEENGINE,
                    "Ignoring seek failure - m_actualState.m_actionState: %!ActionState!",
                    m_actualState.m_actionState);

                IFC(PostSeek());

                IFC(m_nextSubArcMethodStack.PopAndCall());
            }
            else
            {
                //
                // Seek completes synchronously sometimes, so we must call FinishSeekToArc
                // rather than just pushing it onto the stack
                //
                IFC(FinishSeekToArc());
            }
        }
    }

Cleanup:
    ReleaseInterface(pControls);
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::FinishSeekToArc
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
FinishSeekToArc(
    void
    )
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);

    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // This method should only be called when a seek is pending
    //
    Assert(m_pendingInternalState.m_seekTo.m_isValid);

    //
    // We don't do anything unless we've been notified of reaching a position
    //
    if (m_actualState.m_seekTo.m_isValid)
    {
        m_didSeek = true;

        //
        // One case where we can reach the wrong position is if we try to seek beyond
        // the end of the file. In such a case, WMP will seek to the end, rather
        // than past it, so it's safe to ignore it. Bug we log it just in case.
        //
        if (m_actualState.m_seekTo.m_value != m_pendingInternalState.m_seekTo.m_value)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_STATEENGINE,
                "Seek took us to position %f, though we requested %f",
                m_actualState.m_seekTo.m_value,
                m_pendingInternalState.m_seekTo.m_value);
        }

        IFC(PostSeek());

        IFC(m_nextSubArcMethodStack.PopAndCall());
    }
    //
    // We may not get PositionChange notifications if we're stopped
    //
    else if (m_actualState.m_actionState == ActionState::Stop)
    {
        m_didSeek = true;

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_STATEENGINE,
            "Seek happened during stopped state - not waiting for PositionChanged event");

        IFC(PostSeek());

        IFC(m_nextSubArcMethodStack.PopAndCall());
    }
    //
    // Otherwise we schedule ourselves to be run again next time around
    //
    else
    {
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::FinishSeekToArc));
    }

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Function:
//      BeginSeekToAndScrubArc
//
//  Synopsis:
//      Starts seeking
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
BeginSeekToAndScrubArc(
    void
    )
{
    HRESULT         hr = S_OK;
    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // Begin seeking. When done, ContinueSeekToAndScrubArc will be called
    //
    IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::ContinueSeekToAndScrubArc));
    IFC(BeginSeekToArc());

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::ContinueSeekToAndScrubArc
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
ContinueSeekToAndScrubArc(
    void
    )
{
    HRESULT         hr = S_OK;
    IWMPControls    *pIWMPControls = NULL;
    IWMPControls2   *pIWMPControls2 = NULL;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    //
    // This method should only be called after seeking is complete
    //
    Assert(!m_targetInternalState.m_seekTo.m_isValid);
    Assert(!m_pendingInternalState.m_seekTo.m_isValid);
    Assert(!m_currentInternalState.m_seekTo.m_isValid);
    Assert(!m_actualState.m_seekTo.m_isValid);

    //
    // Scrubbing only applies when we are paused and when we have video.
    //
    if (   m_actualState.m_actionState == ActionState::Pause
        && m_targetState.m_actionState == ActionState::Pause
        && m_targetInternalState.m_actionState == ActionState::Pause
        && m_pendingInternalState.m_actionState == ActionState::Pause
        && m_currentInternalState.m_actionState == ActionState::Pause
        && m_pSharedState->GetHasVideo()
        && m_isScrubbingEnabled)
    {
        //
        // We need to do this so that we can be sure the EVR is in a paused state
        //
        m_waitForActionState = ActionState::Pause;
        IFC(m_nextSubArcMethodStack.Push(&CWmpStateEngine::BeginScrubArc));
        IFC(Arc_WaitForActionState());
    }
    //
    // If we can't scrub, and somebody's waiting, we call them
    //
    else
    {
        IFC(m_nextSubArcMethodStack.PopAndCall());
    }

Cleanup:
    ReleaseInterface(pIWMPControls);
    ReleaseInterface(pIWMPControls2);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::MapWmpStateEngine
//
//  Synopsis:
//      Maps WMP states to our internal states for controlling media.
//
//------------------------------------------------------------------------------
/*static*/
Optional<ActionState::Enum>
CWmpStateEngine::
MapWmpStateEngine(
    __in                        WMPPlayState                playerState
    )
{
    static const struct
    {
        WMPPlayState                wmpState;
        ActionState::Enum           ourState;
    }
    aPlayStateMap[] =
    {
        {   wmppsPaused,        ActionState::Pause      },
        {   wmppsPlaying,       ActionState::Play       },
        {   wmppsReady,         ActionState::Stop       },
        {   wmppsStopped,       ActionState::Stop       },
        {   wmppsBuffering,     ActionState::Buffer     },
        {   wmppsWaiting,       ActionState::Buffer     }
    };

    Optional<ActionState::Enum>   ourState;

    for(int i = 0; i < COUNTOF(aPlayStateMap); i++)
    {
        if (aPlayStateMap[i].wmpState == playerState)
        {
            ourState = Optional<ActionState::Enum>(aPlayStateMap[i].ourState);
            break;
        }
    }

    return ourState;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::MediaFinished
//
//  Synopsis:
//      Called when media finishes.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
MediaFinished()
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    m_isMediaEnded = true;

    //
    // We only raise the media ended event if we're supposed to be playing. This
    // prevents false notifications if, for example, we hit the end of media
    // during a scrub.
    //
    if (m_targetState.m_actionState == ActionState::Play)
    {
        IFC(RaiseEvent(AVMediaEnded));
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::RaiseEvent
//
//  Synopsis:
//      It's non-trivial to raise an event without causing a race condition or
//      possible deadlock so we abstract the code out into a separate method.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
RaiseEvent(
    __in    AVEvent     event,
    __in    HRESULT     failureHr
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    //
    // It's possible for WMP to give us events after Avalon has told us to
    // shutdown. In this case we just throw the events away.
    //
    if (!m_isShutdown)
    {
        IFC(m_pMediaInstance->GetMediaEventProxy().RaiseEvent(event, failureHr));
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::PlayerReachedActionStatePlay
//
//  Synopsis:
//      This is called when the player reaches a play state. There are certain
//      properties that we can query on the player at this point that we can't
//      (or that are meaningless) until the media actually starts playing.
//
//      Must be called from WMPs STA thread
//
//------------------------------------------------------------------------------
void
CWmpStateEngine::
PlayerReachedActionStatePlay(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);
    Assert(m_stateThreadId == GetCurrentThreadId());

    IFC(UpdateCanPause());
    IFC(InternalCanSeek());
    IFC(UpdateHasAudioForWmp11());
    IFC(UpdateHasVideoForWmp11());
    IFC(UpdateMediaLength());

    IFC(UpdateNaturalWidth());
    IFC(UpdateNaturalHeight());

Cleanup:

    EXPECT_SUCCESS(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SignalSelf
//
//  Synopsis:
//      Signal the state thread to run us again as a state-item.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SignalSelf(
    void
    )
{
    HRESULT     hr = S_OK;
    bool        isShutdown = false;
    TRACEF(&hr);


    isShutdown = (WaitForSingleObject(m_isShutdownEvent, 0) == WAIT_OBJECT_0);

    if (isShutdown)
    {
        //
        // The managed code should never see this unless they call
        // something after shutdown. The EvrPresenter may see this
        // though.
        //
        IFC(MF_E_SHUTDOWN);
    }

    IFC(AddItem(this));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SubArcMethodStack::SubArcMethodItem::Create
//
//  Synopsis:
//      Create a new SubArcMethodItem
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CWmpStateEngine::SubArcMethodStack::SubArcMethodItem::
Create(
    SubArcMethod method,
    SubArcMethodItem **ppItem
    )
{
    HRESULT hr = S_OK;

    *ppItem = new SubArcMethodItem;

    IFCOOM(*ppItem);

    (*ppItem)->m_method = method;

Cleanup:
    RRETURN(hr);
}

CWmpStateEngine::SubArcMethodStack::
SubArcMethodStack(
    UINT            uiID
    ) : m_pCWmpStateEngine(NULL),
        m_uiID(uiID)
{
    IGNORE_HR(Clear());
}

CWmpStateEngine::SubArcMethodStack::
~SubArcMethodStack(
    void
    )
{
    m_pCWmpStateEngine = NULL; // Not ref-counted

    IGNORE_HR(Clear());
}

void
CWmpStateEngine::SubArcMethodStack::
SetStateEngine(
    CWmpStateEngine *pCWmpStateEngine
    )
{
    Assert(m_pCWmpStateEngine == NULL);
    m_pCWmpStateEngine = pCWmpStateEngine; // Not ref-counted
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SubArcMethodStack::Push
//
//  Synopsis:
//      Push a SubArcMethod onto the stack
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::SubArcMethodStack::
Push(
    SubArcMethod    method
    )
{
    HRESULT hr = S_OK;
    SubArcMethodItem *pItem = NULL;

    TRACEF(&hr);

    IFC(SubArcMethodItem::Create(method, &pItem));

    m_stack.AddHead(pItem);

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SubArcMethodStack::PopAndCall
//
//  Synopsis:
//      If the stack is not empty, pop and call the method at the head.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::SubArcMethodStack::
PopAndCall(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    if (!m_stack.IsEmpty())
    {
        SubArcMethodItem *pItem = m_stack.UnlinkHead();

        SubArcMethod method = pItem->m_method;
        delete pItem;

        IFC((m_pCWmpStateEngine->*method)());
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SubArcMethodStack::
//
//  Synopsis:
//      Remove all items from the SubArcMethod stack
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::SubArcMethodStack::
Clear(
    void
    )
{
    TRACEF(NULL);

    while (!m_stack.IsEmpty())
    {
        SubArcMethodItem *pItem = m_stack.UnlinkHead();
        delete pItem;
    }

    RRETURN(S_OK);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::IsEmpty
//
//  Synopsis:
//      Return whether the set of methods is empty.
//
//------------------------------------------------------------------------------
bool
CWmpStateEngine::SubArcMethodStack::
IsEmpty(
    void
    ) const
{
    return !!m_stack.IsEmpty();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CWmpStateEngine::SetSafeFOrScripting
//
//  Synopsis:
//      Set the given media player to be run safe for scripting.
//
//------------------------------------------------------------------------------
HRESULT
CWmpStateEngine::
SetSafeForScripting(
    __in        IWMPPlayer       *pIWMPMedia
    )
{
    HRESULT         hr = S_OK;

    IObjectSafety   *pIObjectSafety = NULL;

    IFC(pIWMPMedia->QueryInterface(__uuidof(IObjectSafety), reinterpret_cast<void **>(&pIObjectSafety)));

    //
    // IDispatch actually applies to all derived interfaces from the Ocx, it is
    // what a scripting engine would be using. We use the native interfaces
    // instead, but, this request still applies.
    //
    // Note - this doesn't actually do anything in any of our scenarios that we
    // can discern, but, for future versions of the Ocx, this might change
    // behavior, so, it is important that we do this.
    //
    IFC(
        pIObjectSafety->SetInterfaceSafetyOptions(
            __uuidof(IDispatch),
            INTERFACESAFE_FOR_UNTRUSTED_CALLER | INTERFACESAFE_FOR_UNTRUSTED_DATA,
            INTERFACESAFE_FOR_UNTRUSTED_CALLER | INTERFACESAFE_FOR_UNTRUSTED_DATA));

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_STATEENGINE,
        "Set media player to be safe for untrusted callers and data");

Cleanup:

    EXPECT_SUCCESS(hr);

    ReleaseInterface(pIObjectSafety);

    RRETURN(hr);
}

HRESULT
CWmpStateEngine::
RaisePrerolledIfNecessary(
    void
    )
{
    HRESULT hr = S_OK;

    if (m_didPreroll && !m_didRaisePrerolled)
    {
        IFC(UpdateCanPause());
        IFC(InternalCanSeek());
        IFC(UpdateHasAudioForWmp11());
        IFC(UpdateHasVideoForWmp11());
        IFC(UpdateMediaLength());

        IFC(UpdateNaturalWidth());
        IFC(UpdateNaturalHeight());

        IFC(RaiseEvent(AVMediaPrerolled));

        m_didRaisePrerolled = true;
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


