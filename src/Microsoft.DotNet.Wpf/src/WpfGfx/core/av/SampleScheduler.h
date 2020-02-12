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
#pragma once

class CWmpStateEngine;

class SampleScheduler
{
public:

    SampleScheduler(
        __in    MediaInstance   *pMediaInstance,
        __in    CWmpStateEngine *pCWmpStateEngine
        );

    ~SampleScheduler(
        void
        );

    HRESULT
    Init(
        void
        );

    //
    // Stuff called by the presenter
    //

    HRESULT
    ClockStarted(
        void
        );

    HRESULT
    ClockPaused(
        __in        LONGLONG        currentTime
        );



    HRESULT
    GetCompositionSample(
        __in        LONGLONG        currentTime,
        __inout     LONGLONG        *pLastCompositionSampleTime,
        __deref_out CMFMediaBuffer  **ppCMFMediaBuffer,
        __out       BOOL            *pIsNewFrame
        );

    HRESULT
    ReturnCompositionSample(
        __out       bool            *pShouldSignalMixer
        );

    HRESULT
    GetMixSample(
        __in        LONGLONG        currentTime,
        __out       IMFSample       **ppIMFSample
        );

    HRESULT
    ReturnMixSample(
        __in        LONGLONG        currentTime
        );

    void
    Flush(
        __in        LONGLONG        currentTime
        );

    LONGLONG
    CalculateNextCallbackTime(
        __in        LONGLONG        currentTime
        );

    void
    NotifyCompositionIfNecessary(
        __in        LONGLONG        currentTime
        );

    HRESULT
    InvalidateDevice(
        __in        CD3DDeviceLevel1    *pRenderDevice,
        __in        CD3DDeviceLevel1    *pMixerDevice,
        __in        D3DDEVTYPE          deviceType
        );

    HRESULT
    ChangeMediaType(
        __in_opt    IMFVideoMediaType   *pIVideoMediaType
        );


    //
    // Stuff called by the state engine
    //
    void
    BeginScrub(
        void
        );

    void
    EndScrub(
        void
        );

    void
    BeginFakePause(
        void
        );

    void
    EndFakePause(
        void
        );

    void
    BeginStopToPauseFreeze(
        void
        );

    void
    EndStopToPauseFreeze(
        bool doFlush
        );

    void
    AvalonShutdown(
        void
        );

private:
    //
    // Cannot copy or assign a SampleScheduler
    //
    SampleScheduler(
        __in const SampleScheduler &
        );

    SampleScheduler &
    operator=(
        __in const SampleScheduler &
        );

    HRESULT
    GetCompositionSampleFromQueue(
        __in            LONGLONG        currentTime,
        __deref_out     IMFSample       **ppIMFSample
        );

    LONGLONG
    CalculateNextCompositionNotificationTime(
        void
        );


    //
    // immutable and internally locking variables
    //
    UINT                    m_uiID;
    MediaInstance           *m_pMediaInstance;
    CWmpStateEngine         *m_pCWmpStateEngine;
    SampleQueue             m_sampleQueue;


    //
    // Lock for variables generally accessed by the composition
    // thread and sometimes by the state thread and media thread
    //
    CCriticalSection        m_compositionLock;

    //
    // The timestamp on the last composition sample shown
    //
    LONGLONG                m_lastCompositionSampleTime;

    //
    // Frame freeze mode means don't give up composition samples. It's an
    // int rather than a bool because operations "addref" on frame freeze mode
    // by incrementing this variable
    //
    int                     m_isFrameFreezeMode;

    //
    // Frame freeze mode can either mean freeze with the best match of samples
    // currently in the queue or it can mean freeze on the next mix sample.
    // Either way we'll still grab the best match of samples currently in the
    // queue to start off with (otherwise we'd risk showing a random frame
    // while we wait for the next mix sample to arrive)
    //
    bool                    m_doWaitForNewMixSample;

    //
    // If we're waiting for a new mix sample, we don't want to consider any
    // pre-flush sample (we might get a sample, followed by a flush,
    // followed by the real sample that we want). m_isFlushed is true iff we
    // haven't received any new samples since we were last told to flush.
    //
    bool                    m_isFlushed;

    //
    // There are 4 "fancy" cases of scheduling we need to cover:
    //
    // 1. OnClockPause/Stop
    // m_isFrameFreezeMode = true
    // m_doWaitForNewMixSample = false
    //
    // 2. Scrubbing
    // m_isFrameFreezeMode = true
    // m_doWaitForNewMixSample = true
    //
    // 3. StopToPause arc without a scrub
    // (we might want to do this for seeks without scrub as well)
    // m_isFrameFreezeMode = true
    // m_doWaitForNewMixSample = false
    //
    // 4. FakePause
    // m_isFrameFreezeMode = true
    // m_doWaitForNewMixSample = false
    //
    //
    // Since OnClockPause can overlap with either Scrubbing or FakePause, we
    // represent m_isFrameFreezeMode as an integer that we increment
    // whenever someone requests a frame freeze for any reason.
    //

    //
    // Each of the following increments m_isFrameFreezeMode when it is set and
    // decrements when it is unset. We need to keep track of whether or not
    // each is set to avoid multiple increments/decrements We need to keep track
    // of whether or not the clock is started to decide when to
    // increment/decrement our m_isFrameFreezeMode ref on clock changes
    //
    bool                    m_isClockStarted;
    bool                    m_isScrubbing;
    bool                    m_isFakePause;
    bool                    m_isStopToPauseFreeze;

    //
    // Lock for variables not accessed by the composition thread
    //
    CCriticalSection        m_mixerLock;

    IMFSample               *m_pIMixSample;

    LONGLONG                m_lastSampleTime;
    LONGLONG                m_nextSampleTime;

    LONGLONG                m_lastCompositionNotificationTime;
    LONGLONG                m_nextCompositionNotificationTime;

    LONGLONG                m_perFrameInterval;

    static const LONGLONG msc_frameIntervalMultiplier = 10000000;
    static const LONGLONG msc_defaultPerFrameInterval = msc_frameIntervalMultiplier / 60;
};

