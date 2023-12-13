// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "SampleScheduler.tmh"

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// SampleScheduler implementation
//
SampleScheduler::
SampleScheduler(
    __in    MediaInstance   *pMediaInstance,
    __in    CWmpStateEngine *pCWmpStateEngine
    ) : m_uiID(pMediaInstance->GetID()),
        m_pMediaInstance(NULL),
        m_pCWmpStateEngine(NULL),
        m_sampleQueue(pMediaInstance->GetID()),

        //
        // We start out in frame freeze mode because the clock is not started
        // and we want to always be in frame freeze mode when the clock is
        // not started.
        //
        m_isFrameFreezeMode(1),
        m_doWaitForNewMixSample(false),
        m_isFlushed(true),
        m_isClockStarted(false),
        m_isScrubbing(false),
        m_isFakePause(false),
        m_isStopToPauseFreeze(false),
        m_lastCompositionSampleTime(-1LL),
        m_pIMixSample(NULL),
        m_lastSampleTime(-1LL),
        m_nextSampleTime(-1LL),
        m_lastCompositionNotificationTime(-1LL),
        m_nextCompositionNotificationTime(-1LL),
        m_perFrameInterval(msc_defaultPerFrameInterval)
{
    SetInterface(m_pMediaInstance, pMediaInstance);
    SetInterface(m_pCWmpStateEngine, pCWmpStateEngine);
}

SampleScheduler::
~SampleScheduler(
    void
    )
{
    ReleaseInterface(m_pMediaInstance);
    ReleaseInterface(m_pCWmpStateEngine);
    ReleaseInterface(m_pIMixSample);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::Init
//
//  Synopsis:
//      Initialize the SampleScheduler
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
Init(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_mixerLock.Init());

    IFC(m_compositionLock.Init());

    IFC(m_sampleQueue.Init());

Cleanup:
    RRETURN(hr);
}

HRESULT
SampleScheduler::
GetCompositionSample(
    __in        LONGLONG        currentTime,
    __inout     LONGLONG        *pLastCompositionSampleTime,
    __deref_out CMFMediaBuffer  **ppCMFMediaBuffer,
    __out       BOOL            *pIsNewFrame
    )
{
    HRESULT         hr                          = S_OK;
    IMFSample       *pIMFSample                 = NULL;
    LONGLONG        sampleTime                  = 0LL;

    TRACEF(&hr);

    IFCN(GetCompositionSampleFromQueue(
            currentTime,
            &pIMFSample
        ));

    IFC(pIMFSample->GetSampleTime(&sampleTime));

    IFC(ConvertSampleToMediaBuffer(pIMFSample, ppCMFMediaBuffer));

    //
    // We have to return if this is the first time we've given out this frame
    // so that the UCE can decide whether or not to mark us as dirty.
    //
    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        if (*pLastCompositionSampleTime != sampleTime)
        {
            *pIsNewFrame = TRUE;

            //
            // We need to keep track of the last composition sample time given out
            // to _each_ resource so that we know whether or not we're returning a
            // new sample (in which case we have to call NotifyOnChanged)
            //
            *pLastCompositionSampleTime = sampleTime;
        }
        else
        {
            *pIsNewFrame = FALSE;
        }

        //
        // We need to keep track of the last composition sample time given out
        // to _any_ resource so that we know which sample to use in the event of
        // a pause.
        //
        m_lastCompositionSampleTime = sampleTime;
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "GetCompositionSample - Returning sample with sample time: %I64d",
        sampleTime);

Cleanup:
    ReleaseInterface(pIMFSample);

    if (hr != WGXERR_AV_NOREADYFRAMES)
    {
        EXPECT_SUCCESS(hr);
    }

    RRETURN(hr);
}


HRESULT
SampleScheduler::
ReturnCompositionSample(
    __out       bool            *pShouldSignalMixer
    )
{
    HRESULT         hr                          = S_OK;

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        //
        // If we're scrubbing, then we need to hold onto the sample. We also hold onto the
        // sample if we're paused. Otherwise we free it to make room in the queue.
        //
        if (!m_isFrameFreezeMode)
        {
            IFC(m_sampleQueue.ReturnCompositionSample(pShouldSignalMixer));
        }
    }

Cleanup:
    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::ClockPaused
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
ClockPaused(
    __in    LONGLONG    currentTime
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        m_sampleQueue.PauseCompositionSample(currentTime, true);
    }

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        if (m_isClockStarted)
        {
            m_isClockStarted = false;
            m_isFrameFreezeMode++;

            //
            // We don't specify m_doWaitForNewMixSample. It'll be false
            // unless the state engine has set it otherwise, in which
            // case we do want to wait for a new mix sample.
            //
        }
    }

    m_pMediaInstance->GetCompositionNotifier().NotifyComposition();

    RRETURN(S_OK);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::ClockStarted
//
//  Synopsis:
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
ClockStarted(
    void
    )
{
    TRACEF(NULL);

    bool isFrameFreezeMode = false;

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        if (!m_isClockStarted)
        {
            m_isClockStarted = true;
            Assert(m_isFrameFreezeMode > 0);
            m_isFrameFreezeMode--;
        }

        isFrameFreezeMode = m_isFrameFreezeMode > 0;
    }

    //
    // We only unpause the composition sample if we're no longer in frame freeze
    // mode
    //
    if (!isFrameFreezeMode)
    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        m_sampleQueue.UnpauseCompositionSample();
    }

    RRETURN(S_OK);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::GetMixSample
//
//  Synopsis:
//      Called by EvrPresenter when it wants to call ProcessOutput on the mixer
//      If we already have a sample (because the last ProcessOutput failed)
//      then we just return that one. Otherwise we request a new one from the
//      queue.
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
GetMixSample(
    __in        LONGLONG        currentTime,
    __out       IMFSample       **ppIMFSample
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        if (m_pIMixSample)
        {
            SetInterface(*ppIMFSample, m_pIMixSample);
        }
        else
        {
            IFCN(m_sampleQueue.GetMixSample(currentTime, &m_pIMixSample));

            SetInterface(*ppIMFSample, m_pIMixSample);
        }

        Assert(m_pIMixSample != NULL);
    }

Cleanup:
    if (hr != MF_E_NO_VIDEO_SAMPLE_AVAILABLE)
    {
        EXPECT_SUCCESS(hr);
    }
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::ReturnMixSample
//
//  Synopsis:
//      Called by EvrPresenter when ProcessOutput succeeds.
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
ReturnMixSample(
    __in        LONGLONG        currentTime
    )
{
    HRESULT         hr = S_OK;
    CMFMediaBuffer  *pCMFMediaBuffer = NULL;
    bool            didReceiveScrubSample = false;
    LONGLONG        newMixSampleTime = 0;
    IMFSample       *pIReleaseSample = NULL;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        //
        // Before we put the sample back into the sample queue, we want to make sure that we
        // mark the sample's content as invalid. This is useful if two composition passes happen
        // to select the same sample twice in a row and need to do some manipulation
        // (like transfer it to another device). It can avoid some expensive manipulations
        // in this case.
        //
        IFC(ConvertSampleToMediaBuffer(m_pIMixSample, &pCMFMediaBuffer));

        pCMFMediaBuffer->InvalidateCachedResources();

        IFC(m_pIMixSample->GetSampleTime(&newMixSampleTime));

        Assert(newMixSampleTime >= 0);

        IFC(m_sampleQueue.ReturnMixSample(currentTime, m_pIMixSample));

        pIReleaseSample = m_pIMixSample;
        m_pIMixSample = NULL;
    }

    //
    // If we were waiting for a new mix sample, we now have it. We notify the
    // state engine that we've scrubbed, set the composition sample to be
    // the one we just got, and notify composition.
    //
    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        if (m_doWaitForNewMixSample && m_isFlushed)
        {
            //
            // We have to clear the last composition sample time to ensure that
            // we tell composition about our new sample. Even if we have
            // received a Flush, which clear m_lastCompositionSampleTime,
            // m_lastCompositionSampleTime may have been set/ again a subsequent
            // BeginComposition
            //
            m_lastCompositionSampleTime = -1LL;

            m_doWaitForNewMixSample = false;

            didReceiveScrubSample = true;
        }

        m_isFlushed = false;

        m_pMediaInstance->GetCompositionNotifier().InvalidateLastCompositionSampleTime();
    }

    if (didReceiveScrubSample)
    {
        {
            CGuard<CCriticalSection> guard(m_mixerLock);

            IFC(m_sampleQueue.RechooseCompositionSampleFromMixerThread(newMixSampleTime));

            if (m_pCWmpStateEngine)
            {
                IFC(WmpStateEngineProxy::AsyncCallMethod(
                        m_uiID,
                        m_pCWmpStateEngine,
                        m_pCWmpStateEngine,
                        &CWmpStateEngine::ScrubSampleComposited,
                        0 /* placeholder */));
            }
        }

        m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
    }

Cleanup:
    ReleaseInterface(pIReleaseSample);
    ReleaseInterface(pCMFMediaBuffer);


    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::Flush
//
//  Synopsis:
//      Called by EvrPresenter when it wants to flush the sample queue.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
Flush(
    __in        LONGLONG        currentTime
    )
{
    //
    // We flush the samples from the queue ...
    //
    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        m_sampleQueue.SignalFlush(currentTime);
    }

    //
    // ... and then reset the sample times since
    // new sample times put in the queue won't relate
    // to the old times
    //
    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        m_lastCompositionSampleTime = -1LL;
        m_isFlushed = true;

        m_pMediaInstance->GetCompositionNotifier().InvalidateLastCompositionSampleTime();
    }

    {
        CGuard<CCriticalSection>    guard(m_mixerLock);

        m_lastSampleTime = -1LL;
        m_nextSampleTime = gc_invalidTimerTime;

        m_lastCompositionNotificationTime = -1LL;
        m_nextCompositionNotificationTime = gc_invalidTimerTime;
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::CalculateNextCallbackTime
//
//  Synopsis:
//      Called by EvrPresenter to determine the time it shoud request to be
//      called back at. EvrPresenter is must notify composition immediately
//      if *pNotifyCompositionNow is true.
//
//------------------------------------------------------------------------------
LONGLONG
SampleScheduler::
CalculateNextCallbackTime(
    __in        LONGLONG        currentTime
    )
{
    LONGLONG    nextCallbackTime = gc_invalidTimerTime;

    //
    // Update m_nextCompositionNotificationTime, m_lastCompositionSampleTime
    // as well as determine whether or not composition should be notified now
    //
    NotifyCompositionIfNecessary(currentTime);

    {
        CGuard<CCriticalSection>    guard(m_mixerLock);

        //
        // If there are no future samples, we still wake up again to call ProcessOutput.

        if (m_nextCompositionNotificationTime == gc_invalidTimerTime)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Decided to wake up at %I64d based on default per frame interval",
                currentTime + m_perFrameInterval);

            nextCallbackTime = currentTime + m_perFrameInterval;
        }
        else
        {
            nextCallbackTime = m_nextCompositionNotificationTime;
        }
    }


    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "GetNextCallbackTime(%I64d) : %I64d",
        currentTime,
        nextCallbackTime);

    return nextCallbackTime;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::NotifyCompositionIfNecessary
//
//  Synopsis:
//      Called by EvrPresenter if it may be time to notify composition
//      now. This also updates the sample time variables.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
NotifyCompositionIfNecessary(
    __in        LONGLONG        currentTime
    )
{
    //
    // Find the last and next sample times
    //
    {
        CGuard<CCriticalSection>    guard(m_mixerLock);

        m_sampleQueue.GetNextSampleTime(currentTime, &m_lastSampleTime, &m_nextSampleTime);

        //
        // Calculate the next notification time
        //
        m_nextCompositionNotificationTime = CalculateNextCompositionNotificationTime();

        //
        // If callers call us and we return true, they are required to notify composition,
        // so we update last composition notification time, and calculate the next one
        //
        if (currentTime > m_nextCompositionNotificationTime)
        {
            m_lastCompositionNotificationTime = m_nextCompositionNotificationTime;
            m_nextCompositionNotificationTime = CalculateNextCompositionNotificationTime();

            m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
        }
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::NewDevice
//
//  Synopsis:
//      Called when we are changing to a new D3D device.
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
InvalidateDevice(
    __in        CD3DDeviceLevel1    *pRenderDevice,
    __in        CD3DDeviceLevel1    *pMixerDevice,
    __in        D3DDEVTYPE          deviceType
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    IMFSample   *pIReleaseSample = NULL;

    {
        CGuard<CCriticalSection>    guard(m_mixerLock);

        pIReleaseSample = m_pIMixSample;
        m_pIMixSample = NULL;

        //
        // Need to discard our mix sample on device invalidation otherwise
        // ProcessOutput will repeatedly fail.
        //

        IFC(m_sampleQueue.InvalidateDevice(
                    pRenderDevice,
                    pMixerDevice,
                    deviceType));
    }

Cleanup:

    ReleaseInterface(pIReleaseSample);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::InvalidateMediaType
//
//  Synopsis:
//      Called when the media type of the stream has changed, this can also be
//      when we are informed of a new mixer through InitServicePointers.
//      We use this to get a new media type and set it.
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
ChangeMediaType(
    __in_opt    IMFVideoMediaType   *pIMediaType
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    IMFSample   *pIReleaseSample = NULL;

    if (pIMediaType)
    {
        if (   pIMediaType->GetVideoFormat()->videoInfo.FramesPerSecond.Numerator != 0
            && pIMediaType->GetVideoFormat()->videoInfo.FramesPerSecond.Denominator != 0)
        {
            m_perFrameInterval
                = MulDiv(
                        msc_frameIntervalMultiplier,
                        pIMediaType->GetVideoFormat()->videoInfo.FramesPerSecond.Denominator,
                        pIMediaType->GetVideoFormat()->videoInfo.FramesPerSecond.Numerator
                        );

            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Getting PerFrameInterval from video format %I64d : ",
                m_perFrameInterval);
        }
        else
        {
            // reset to default.
            m_perFrameInterval = msc_defaultPerFrameInterval;

            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Numerator/Denominator = %d/%d. PerFrameInterval unavailable on video format, using default : %I64d",
                pIMediaType->GetVideoFormat()->videoInfo.FramesPerSecond.Numerator,
                pIMediaType->GetVideoFormat()->videoInfo.FramesPerSecond.Denominator,
                m_perFrameInterval);
        }
    }

    {
        CGuard<CCriticalSection>    guard(m_mixerLock);

        //
        // Need to clear the mix sample for each change in media type.
        //
        pIReleaseSample = m_pIMixSample;
        m_pIMixSample = NULL;

        //
        // Need to release our mix sample when the media type changes or ProcessOutput
        // will repeatedly fail.
        //
        IFC(m_sampleQueue.ChangeMediaType(
                    pIMediaType
                    ));
    }

Cleanup:

    ReleaseInterface(pIReleaseSample);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//
// Methods called by the state engine
//

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::BeginScrub
//
//  Synopsis:
//      The state engine calls this when it wants to scrub - that is show and
//      hold the next frame that we get from the mixer.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
BeginScrub(
    void
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection>    guard(m_compositionLock);

        //
        // scrubbing is incompatible with
        // and fake pause
        //
        Assert(!m_isFakePause);

        if (!m_isScrubbing)
        {
            m_isScrubbing = true;
            m_isFrameFreezeMode++;
        }

        m_doWaitForNewMixSample = true;
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::EndScrub
//
//  Synopsis:
//      The state engine calls this when it is time for future samples to be
//      shown.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
EndScrub(
    void
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection>    guard(m_compositionLock);

        if (m_isScrubbing)
        {
            Assert(m_isFrameFreezeMode > 0);
            m_isFrameFreezeMode--;
            m_doWaitForNewMixSample = false;

            m_isScrubbing = false;
        }
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::BeginFakePause
//
//  Synopsis:
//      Puts the scheduler into frame freeze mode, but doesn't wait for the next
//      mix sample. Mix samples continue to be processed.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
BeginFakePause(
    void
    )
{
    LONGLONG    fakePauseTime = 0LL;

    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        Assert(!m_isFakePause);
        Assert(!m_isScrubbing); // incompatible with fake pause

        m_isFakePause = true;
        m_isFrameFreezeMode++;
        m_doWaitForNewMixSample = false;

        fakePauseTime = m_lastCompositionSampleTime;
    }

    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        m_sampleQueue.PauseCompositionSample(fakePauseTime, true);
    }

    m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::EndFakePause
//
//  Synopsis:
//      Takes the scheduler out of frame freeze mode and shows
//      the proper frame.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
EndFakePause(
    void
    )
{
    TRACEF(NULL);

    bool isFrameFreezeMode = false;

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        if (m_isFakePause)
        {
            Assert(m_isFrameFreezeMode > 0);
            m_isFrameFreezeMode--;
            m_doWaitForNewMixSample = false;

            m_isFakePause = false;
        }

        isFrameFreezeMode = m_isFrameFreezeMode > 0;
    }

    //
    // We only unpause the composition sample if we're no longer in frame freeze
    // mode
    //
    if (!isFrameFreezeMode)
    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        m_sampleQueue.UnpauseCompositionSample();
    }

    m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::BeginStopToPauseFreeze
//
//  Synopsis:
//      The state engine calls this when it wants to transition to pause without
//      scrubbing.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
BeginStopToPauseFreeze(
    void
    )
{
    bool        isAlreadyFrozen = false;
    LONGLONG    lastCompositionSampleTime = -1LL;

    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        Assert(!m_isScrubbing); // incompatible with stop to pause freeze

        isAlreadyFrozen = m_isStopToPauseFreeze;

        if (!isAlreadyFrozen)
        {
            m_isStopToPauseFreeze = true;
            m_isFrameFreezeMode++;
            m_doWaitForNewMixSample = false;

            lastCompositionSampleTime = m_lastCompositionSampleTime;
        }
    }

    //
    // We only pause the composition sample if we aren't already doing a stop
    // to pause freeze. If we are, then the composition sample will already be
    // paused.
    //
    if (!isAlreadyFrozen)
    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        m_sampleQueue.PauseCompositionSample(lastCompositionSampleTime, false);
    }

    m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::EndStopToPauseFreeze
//
//  Synopsis:
//      The state engine calls this when it's finished transitioning to pause
//      without scrubbing.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
EndStopToPauseFreeze(
    bool doFlush
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_compositionLock);

        if (m_isStopToPauseFreeze)
        {
            Assert(m_isFrameFreezeMode > 0);
            m_isFrameFreezeMode--;
            m_doWaitForNewMixSample = false;

            m_isStopToPauseFreeze = false;
        }
    }

    {
        CGuard<CCriticalSection> guard(m_mixerLock);

        if (doFlush)
        {
            m_sampleQueue.SignalFlush(gc_invalidTimerTime);
        }
        else
        {
            m_sampleQueue.UnpauseCompositionSample();
        }
    }

    m_pMediaInstance->GetCompositionNotifier().NotifyComposition();
}



//
// Private methods
//

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::GetCompositionSampleFromQueue
//
//  Synopsis:
//      When GetCompositionSample is called, we need to decide whether or not
//      we want to rechoose the composition sample. This only affects the case
//      when there isn't currently a sample allocated for composition.
//
//------------------------------------------------------------------------------
HRESULT
SampleScheduler::
GetCompositionSampleFromQueue(
    __in            LONGLONG        currentTime,
    __deref_out     IMFSample       **ppIMFSample
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    //
    // Snap a new frame from the sample queue
    //
    IFCN(m_sampleQueue.GetCompositionSample(
            !m_isFrameFreezeMode,
            currentTime,
            ppIMFSample
        ));

Cleanup:
    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::CalculateNextCompositionNotificationTime   (private)
//
//  Synopsis:
//      Calculates the next time to notify composition
//
//------------------------------------------------------------------------------
LONGLONG
SampleScheduler::
CalculateNextCompositionNotificationTime(
    void
    )
{
    LONGLONG    next = 0LL;

    //
    // If we haven't notified composition about the last sample,
    // then we pick that one
    //
    if (m_lastSampleTime > m_lastCompositionNotificationTime)
    {
        next = m_lastSampleTime;
    }
    //
    // Otherwise, if we haven't notified composition about the next sample,
    // then we pick that one
    //
    else if (m_nextSampleTime > m_lastCompositionNotificationTime)
    {
        next = m_nextSampleTime;
    }
    //
    // Otherwise we don't need to notify composition
    //
    else
    {
        next = gc_invalidTimerTime;
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "CalculateNextCompositionNotificationTime() : %I64d",
        next);

    return next;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleScheduler::AvalonShutdown
//
//  Synopsis:
//      Shuts down the Avalon-related functionalities of the sample scheduler
//      (as opposed to the EVR-related ones). This releases Avalon-related
//      pointers but holds onto EVR-related pointers. We need to keep processing
//      samples until the EVR tells us to shutdown or we may cause the
//      EVR to become non-responsive.
//
//------------------------------------------------------------------------------
void
SampleScheduler::
AvalonShutdown(
    void
    )
{
    CGuard<CCriticalSection> guard(m_mixerLock);

    ReleaseInterface(m_pCWmpStateEngine);
}


