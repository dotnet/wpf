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
//      Provide support for having a set of samples in a circular buffer and
//      having managing the samples. We anticipate queue lengths will remain
//      quite small, so, using a circular vector is probably the cheapest and
//      fastest alternative.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "SampleQueue.tmh"

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue constructor
//
//  Synopsis:
//      Starts instantiates a sample queue. The initial state is that the
//      current view is view 0, and that all the samples in the queue are
//      invalid.
//
//------------------------------------------------------------------------------
SampleQueue::
SampleQueue(
    __in    UINT        uiID
    ) : m_uiID(uiID),
        m_viewState(0),
        m_pRenderDevice(NULL),
        m_pMixerDevice(NULL),
        m_deviceType(static_cast<D3DDEVTYPE>(-1)),
        m_pIVideoMediaType(NULL),
        m_continuityNumber(0)
{
    TRACEF(NULL);

    ZeroMemory(&m_apISamples, sizeof(m_apISamples));
    ZeroMemory(&m_stateViews, sizeof(m_stateViews));

    StateViewLogicalSample    fields =
    {
        0,                      // Current view is view 0
        {
            msc_fieldMask,      // Mixer thread doesn't have a view
            msc_fieldMask       // Composition thread doesn't have a view
        },
        0                       // Continuity number is 0.
    };

    m_viewState = TranslateViewState(fields);

    //
    // Set the first state view so that noone has a sample and all of the times
    // are set to invalid.
    //
    m_stateViews[0].compositionSample = kInvalidSample;
    m_stateViews[0].mixerSample = kInvalidSample;

    for(int i = 0; i < kSamples; i++)
    {
        m_stateViews[0].sampleTimes[i] = kInvalidTime;
    }
}

SampleQueue::
~SampleQueue(
    void
    )
{
    TRACEF(NULL);

    for(int i = 0; i < COUNTOF(m_apISamples); i++)
    {
        ReleaseSample(&m_apISamples[i]);
    }

    ReleaseInterface(m_pRenderDevice);
    ReleaseInterface(m_pMixerDevice);
    ReleaseInterface(m_pIVideoMediaType);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::Init
//
//  Synopsis:
//      Initializes the sample queue.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
Init(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEF(&hr);

    IFC(m_mediaLock.Init());

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::ChangeMediaType
//
//  Synopsis:
//      Called when the media type (call also be called when the media type is
//      cleared). We discard all of our samples at this point.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
ChangeMediaType(
    __in_opt    IMFVideoMediaType   *pIVideoMediaType
        // The new media type, or NULL if it is being reset.
    )
{
    TRACEF(NULL);

    IMFVideoMediaType   *pIOldVideoMediaType = NULL;

    {
        CGuard<CCriticalSection>    guard(m_mediaLock);

        pIOldVideoMediaType = m_pIVideoMediaType;
        m_pIVideoMediaType = pIVideoMediaType;

        if (m_pIVideoMediaType)
        {
            m_pIVideoMediaType->AddRef();
        }

        //
        // Make sure that whenever the mediatype changes, we
        // get rid of old samples allocated with the previous
        // media type.
        //
        m_continuityNumber++;
    }

    ReleaseInterface(pIOldVideoMediaType);

    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::InvalidateDevice
//
//  Synopsis:
//      Sets a new device for the samples. This doesn't immediately cause us to
//      reallocate the samples, but, if we find a sample on the mixer side whose
//      device does not match the current device, we will reallocate it.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
InvalidateDevice(
    __in    CD3DDeviceLevel1    *pRenderDevice,
        // The new avalon render device wrapper
    __in    CD3DDeviceLevel1    *pMixerDevice,
        // The corresponding avalon mixer device wrapper
    __in    D3DDEVTYPE          deviceType
        // The type of we are using, either D3DDEVTYPE_HAL, or D3DDEVTYPE_SW
    )
{
    HRESULT             hr = S_OK;
    CD3DDeviceLevel1    *pRenderRelease = NULL;
    CD3DDeviceLevel1    *pMixerRelease = NULL;

    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "InvalidateDevice(0x%p, 0x%p)",
        pRenderDevice,
        pMixerDevice);

    {
        CGuard<CCriticalSection>    guard(m_mediaLock);

        pRenderRelease = m_pRenderDevice;
        SetInterface(m_pRenderDevice, pRenderDevice);

        pMixerRelease = m_pMixerDevice;
        SetInterface(m_pMixerDevice, pMixerDevice);

        m_deviceType = deviceType;

        m_continuityNumber++;
    }

    ReleaseInterface(pRenderRelease);
    ReleaseInterface(pMixerRelease);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::GetMixSample
//
//  Synopsis:
//      Gets a sample that the mixer can use. We will only choose a sample that
//      the composition engine will not need again. Always called from the mixer
//      thread.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
GetMixSample(
    __in        LONGLONG            currentTime,
        // The current time
    __deref_out IMFSample           **ppISample
        // The returned sample
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView = { 0 };
    bool                      viewApplied = false;
    BYTE                      sampleToUse = kInvalidSample;
    DBG_CODE(bool             unexpectedSampleTimeFound = false);

    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    do
    {
        BYTE        sampleNotInUseByComposition = kInvalidSample;
        BYTE        nextCompositionSample = kInvalidSample;
        LONGLONG    nextCompositionTime = kReservedForCompositionTime;

        DBG_CODE(unexpectedSampleTimeFound = false);

        //
        // Clear any error conditions that might apply. The state could always
        // have changed so radically when we come arounda again that a previous
        // error condition is now erroneous.
        //
        if (FAILED(hr))
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_SAMPLEQUEUE,
                "GetMixSample discarding error 0x%x",
                hr);
        }

        hr = S_OK;

        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        StateView &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        DBG_CODE(startStateView = mixerView);

        //
        // We could have left a request for composition to notify us of a mix sample, this
        // is current irrelevant.
        //
        mixerView.mixerSample = kInvalidSample;

        //
        // We want to find the sample that is less than or equal to the current time, and then
        // we want to replace any sample strictly less than *this*, the reason for this is that
        // we want to only ever invalidate samples that are strictly older than any sample composition
        // might want to pick.
        //
        for(BYTE i = 0; i < kSamples; i++)
        {
            LONGLONG    sampleTime = mixerView.sampleTimes[i];
            DBG_CODE(unexpectedSampleTimeFound = !IsExpectedSampleTime(sampleTime) || unexpectedSampleTimeFound);

            if (sampleTime > nextCompositionTime && sampleTime <= currentTime && IsPositiveSampleTime(sampleTime))
            {
                nextCompositionTime = sampleTime;
                nextCompositionSample = i;
            }
        }

        Assert(IsPositiveSampleTime(nextCompositionTime) || nextCompositionTime == kReservedForCompositionTime);

        //
        // Now, we have a thread-safe snapped view, find the smallest sample that has
        // a time smaller than the invalidation time. We need to keep track of this
        // sample, because of there are duplicate frames with identical sample times,
        // we are allowed to invalidate one of them. We have encountered bugs in the
        // EVR that cause us to get samples with identical times. Invalidating other
        // samples allows us to remain robust against such bugs.
        //
        LONGLONG    smallestSampleTime = gc_invalidTimerTime;

        for(BYTE i = 0; i < kSamples; i++)
        {
            LONGLONG sampleTime = mixerView.sampleTimes[i];
            DBG_CODE(unexpectedSampleTimeFound = !IsExpectedSampleTime(sampleTime) || unexpectedSampleTimeFound);

            //
            // First we can't choose a sample if composition is currently using it.
            //
            if (mixerView.compositionSample != i)
            {
                //
                // Second, the sample time must be smaller than or equal to the
                // invalidation time, but different from the next composition
                // sample, or invalid.
                // We don't ever choose samples with kReservedForCompositionTime.
                //
                if (   (   IsPositiveSampleTime(sampleTime)
                        && sampleTime <= nextCompositionTime
                        && nextCompositionSample != i)
                    || sampleTime == kInvalidTime)

                {
                    if (sampleTime < smallestSampleTime)
                    {
                        smallestSampleTime = sampleTime;

                        sampleNotInUseByComposition = i;
                    }
                }
            }
        }

        //
        // If we have a valid sample that is not in use by composition, then we will use
        // that sample preferentially
        //
        if (IsValidSampleIndex(sampleNotInUseByComposition))
        {
            mixerView.mixerSample = sampleNotInUseByComposition;
            sampleToUse = sampleNotInUseByComposition;
        }
        //
        // Otherwise, we don't have a sample we can use (right now).
        //
        else
        {
            hr = MF_E_NO_VIDEO_SAMPLE_AVAILABLE;

            //
            // If composition is using a stale sample, then we want to be
            // notified when composition returns that sample
            //
            if (   IsValidSampleIndex(mixerView.compositionSample)
                && mixerView.compositionSample != nextCompositionSample)
            {
                //
                // We signal composition that we would like to use this particular sample. (Composition
                // will signal us through the Composition Done event when they release the sample).
                //
                mixerView.mixerSample = mixerView.compositionSample;
            }
        }

        DBG_CODE(endStateView = mixerView);

        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    //
    // OK, Dump the two state views.
    //
    DBG_CODE(DumpState("GetMixSample", startStateView, endStateView, currentTime));

    IFCN(hr);

    //
    // Validate that the device associated with the sample is the same as the
    // device that allocated the sample. If it isn't we have to allocate a new
    // sample.
    //
    IFC(ValidateAndGetMixSample(sampleToUse, ppISample));

Cleanup:

    DBG_CODE(Assert(!unexpectedSampleTimeFound));

    if (hr != MF_E_NO_VIDEO_SAMPLE_AVAILABLE)
    {
        EXPECT_SUCCESS(hr);
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::ReturnMixSample
//
//  Synopsis:
//      Returns the sample that has been acquired by the mixer. The sample queue
//      also can find the sample, the reason why the sample is passed in is to
//      ensure that we can retrieve its time early and also to check that the
//      sample and the queues notion of the sample are the same (if there are
//      different it indicates a very serious problem).
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
ReturnMixSample(
    __in        LONGLONG            currentTime,
        // The current time
    __in        IMFSample           *pISample
        // The sample being returned
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    BYTE                      sampleToUse = kInvalidSample;

    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    LONGLONG    sampleTime = 0;

    IFC(pISample->GetSampleTime(&sampleTime));
    Assert(IsPositiveSampleTime(sampleTime));

    do
    {
        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        StateView  &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        DBG_CODE(startStateView = mixerView);

        sampleToUse = mixerView.mixerSample;

        //
        // Assert that the sample that is being passed back in is in fact the mixer sample.
        //
        Assert(IsValidSampleIndex(sampleToUse));
        Assert(m_apISamples[sampleToUse] == pISample);

        //
        // Indicate that we are relinquishing the mixer sample.
        //
        mixerView.mixerSample = kInvalidSample;

        //
        // Reset the sample time.
        //
        mixerView.sampleTimes[sampleToUse] = sampleTime;

        for(int i = 0; i < kSamples; i++)
        {
            if (mixerView.sampleTimes[i] == kReservedForCompositionTime)
            {
                mixerView.sampleTimes[i] = kInvalidTime;
            }
        }

        DBG_CODE(endStateView = mixerView);

        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    DBG_CODE(DumpState("ReturnMixSample", startStateView, endStateView, currentTime));

Cleanup:

    if (FAILED(hr))
    {
        RIP("This should never ever fail, the only failure indicates a serious race condition.");
    }

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::GetNextSampleTime
//
//  Synopsis:
//      Returns the sample time after the current time (if there is such a
//      sample). If all of the samples are in the past, then we return
//      gc_invalidTimerTime.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
GetNextSampleTime(
    __in        LONGLONG            currentTime,
        // The current time
    __out       LONGLONG            *pLastTime,
        // The time of the sample that should currently be displaying
    __out       LONGLONG            *pNextTime
        // The time of the sample after the current sample.
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;

    do
    {
        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        //
        // Need to figure out when we next need to execute.
        //
        CalculateNextTime(sampleView, currentTime, pLastTime, pNextTime);

        //
        // We need to apply the view because the continuity numbers ensure that the original
        // view we acquired in GetStateView hasn't been trashed by another thread using that
        // as their view.
        //
        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    DBG_CODE(DumpTime("GetNextSampleTime", currentTime));

Cleanup:

    if (FAILED(hr))
    {
        RIP("This should never ever fail, the only failure indicates a serious race condition.");
    }

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::GetSmallestSampleTime
//
//  Synopsis:
//      Returns the smallest sample time in the sample queue. This can be useful
//      for cases where the clock is suddenly reset on a clock change and we
//      have to reset our sample state.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
GetSmallestSampleTime(
    __out       LONGLONG            *pSmallestTime
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    LONGLONG                  smallestTime = gc_invalidTimerTime;

    do
    {
        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        const StateView  &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        for(int i = 0; i < kSamples; i++)
        {
            if (mixerView.sampleTimes[i] < smallestTime)
            {
                smallestTime = mixerView.sampleTimes[i];
            }
        }

        //
        // We need to apply the view because the continuity numbers ensure that the original
        // view we acquired in GetStateView hasn't been trashed by another thread using that
        // as their view.
        //
        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "GetSmallestSampleTime returned %I64d",
        smallestTime);

    *pSmallestTime = smallestTime;

Cleanup:

    if (FAILED(hr))
    {
        RIP("This should never ever fail, the only failure indicates a serious race condition.");
    }

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::SignalFlush
//
//  Synopsis:
//      Signals that all of the current samples should be discarded, except for
//      one that we reserve for composition. Flushing can be necessary when we
//      are going backwards in time (for example).
//
//------------------------------------------------------------------------------
void
SampleQueue::
SignalFlush(
    __in        LONGLONG            currentTime
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    BYTE                      saveForcompositionSample = kInvalidSample;

    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    do
    {
        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        StateView  &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        DBG_CODE(startStateView = mixerView);

        //
        // We don't rechoose the composition sample because if we did we could
        // end up with two samples "reserved" for composition - the one that composition
        // is currently using and the kReservedForCompositionTime sample
        //
        saveForcompositionSample = ChooseCompositionSample(false, true, currentTime, mixerView);

        //
        // Set all of the sample times to invalid (this also has the side effect that
        // we will pre-roll a number of samples when next we are requested to stream
        // them.
        //
        for(int i = 0; i < kSamples; i++)
        {
            mixerView.sampleTimes[i] = kInvalidTime;
        }

        //
        // ...except for the sample that we save for composition - set this one to
        // kReservedForCompositionSample
        //
        if (IsValidSampleIndex(saveForcompositionSample))
        {
            mixerView.sampleTimes[saveForcompositionSample] = kReservedForCompositionTime;
        }

        DBG_CODE(endStateView = mixerView);

        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "SignalFlush(%I64d)", currentTime);

    DBG_CODE(DumpSamples(startStateView, endStateView));

Cleanup:

    if (FAILED(hr))
    {
        RIP("This should never ever fail, the only failure indicates a serious race condition.");
    }

    EXPECT_SUCCESS(hr);

    IGNORE_HR(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::GetCompositionSample
//
//  Synopsis:
//      Returns the composition sample (if any) that is correct to show for the
//      current time. Always called from the composition thread.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
GetCompositionSample(
    __in            bool                rechooseSample,
    __in            LONGLONG            currentTime,
        // The current time
    __deref_out     IMFSample           **ppISample
        // The next sample to return
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    BYTE                      sampleToUse = kInvalidSample;

    DBG_CODE(LONGLONG         grabbedSampleTime = 0);
    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "GetCompositionSample(%d, %I64d)",
        rechooseSample,
        currentTime);

    do
    {
        sampleToUse = kInvalidSample;

        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::CompositionThread, &sampleView));

        StateView &compositionView = m_stateViews[sampleView.inUseView[SampleThreads::CompositionThread]];

        DBG_CODE(startStateView = compositionView);

        sampleToUse = ChooseCompositionSample(rechooseSample, false, currentTime, compositionView);

        if (IsValidSampleIndex(sampleToUse))
        {
            DBG_CODE(grabbedSampleTime = compositionView.sampleTimes[sampleToUse]);

            compositionView.compositionSample = sampleToUse;
        }

        DBG_CODE(endStateView = compositionView);

        viewApplied = ApplyStateView(SampleThreads::CompositionThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "GetCompositionSample - currrent time [%I64d]",
        currentTime);

    DBG_CODE(DumpSamples(startStateView, endStateView));

    if (IsValidSampleIndex(sampleToUse) && NULL != m_apISamples[sampleToUse])
    {
#if DBG
        {
            LONGLONG sampleTime = 0;
            if (SUCCEEDED(m_apISamples[sampleToUse]->GetSampleTime(&sampleTime)))
            {
                if (!(sampleTime == grabbedSampleTime || !IsPositiveSampleTime(grabbedSampleTime)))
                {
                    LogAVDataM(
                        AVTRACE_LEVEL_ERROR,
                        AVCOMP_SAMPLEQUEUE,
                        "GetCompositionSample - found incorrect sample time [%I64d], expected [%I64d]",
                        sampleTime,
                        grabbedSampleTime);
                }
            }
            else
            {
                LogAVDataM(
                    AVTRACE_LEVEL_ERROR,
                    AVCOMP_SAMPLEQUEUE,
                    "GetCompositionSample - GetSampleTime failed");
            }
        }
#endif // DBG

        *ppISample = m_apISamples[sampleToUse];
        m_apISamples[sampleToUse]->AddRef();
    }
    else
    {
        IFCN(WGXERR_AV_NOREADYFRAMES);
    }

Cleanup:

    if (hr != WGXERR_AV_NOREADYFRAMES)
    {
        EXPECT_SUCCESS(hr);
    }

    RRETURN(hr);
}

HRESULT
SampleQueue::
RechooseCompositionSampleFromMixerThread(
    __in            LONGLONG            currentTime
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    BYTE                      sampleToUse = kInvalidSample;

    DBG_CODE(LONGLONG         grabbedSampleTime = 0);
    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "ReChooseCompositionSampleFromMixerThread(%I64d)",
        currentTime);

    do
    {
        sampleToUse = kInvalidSample;

        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        StateView &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        DBG_CODE(startStateView = mixerView);

        sampleToUse = ChooseCompositionSample(true, false, currentTime, mixerView);

        if (IsValidSampleIndex(sampleToUse))
        {
            DBG_CODE(grabbedSampleTime = mixerView.sampleTimes[sampleToUse]);

            mixerView.compositionSample = sampleToUse;
        }

        DBG_CODE(endStateView = mixerView);

        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "ReChooseCompositionSampleFromMixerThread - currrent time [%I64d]",
        currentTime);

    DBG_CODE(DumpSamples(startStateView, endStateView));

Cleanup:
    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::PauseCompositionSample
//
//  Synopsis:
//      Reserves the composition sample (if any) that is correct to show for the
//      current time. Always called from the mixer thread.
//
//------------------------------------------------------------------------------
void
SampleQueue::
PauseCompositionSample(
    __in            LONGLONG        currentTime,
    __in            bool            allowForwardSamples
    )
{
    HRESULT hr = S_OK;

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    BYTE                      sampleToUse = kInvalidSample;

    DBG_CODE(LONGLONG         grabbedSampleTime = 0);
    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    TRACEF(&hr);

    do
    {
        sampleToUse = kInvalidSample;

        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        StateView &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        DBG_CODE(startStateView = mixerView);

        sampleToUse = ChooseCompositionSample(false, allowForwardSamples, currentTime, mixerView);

        if (IsValidSampleIndex(sampleToUse))
        {
            DBG_CODE(grabbedSampleTime = mixerView.sampleTimes[sampleToUse]);

            mixerView.compositionSample = sampleToUse;
        }
        else
        {
            mixerView.compositionSample = kNoPauseSample;
        }

        DBG_CODE(endStateView = mixerView);

        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "PauseCompositionSample - currrent time [%I64d]",
        currentTime);

    DBG_CODE(DumpSamples(startStateView, endStateView));

Cleanup:
    if (FAILED(hr))
    {
        RIP("This should never ever fail, the only failure indicates a serious race condition.");
    }

    EXPECT_SUCCESS(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::UnpauseCompositionSample
//
//  Synopsis:
//      Unreserves the composition sample if it is reserved. Always called from
//      the mixer thread.
//
//------------------------------------------------------------------------------
void
SampleQueue::
UnpauseCompositionSample(
    void
    )
{
    HRESULT hr = S_OK;
    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;

    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    TRACEF(&hr);

    do
    {
        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::MixerThread, &sampleView));

        StateView &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

        DBG_CODE(startStateView = mixerView);

        //
        // If the composition sample is currently kNoPauseSample, which means no frames
        // will be shown, we set it to kInvalidSample, so that a frame will be shown the
        // next time a composition pass happens.
        //
        if (mixerView.compositionSample == kNoPauseSample)
        {
            mixerView.compositionSample = kInvalidSample;
        }

        DBG_CODE(endStateView = mixerView);

        viewApplied = ApplyStateView(SampleThreads::MixerThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "UnpauseCompositionSample");

    DBG_CODE(DumpSamples(startStateView, endStateView));

Cleanup:
    if (FAILED(hr))
    {
        RIP("This should never ever fail, the only failure indicates a serious race condition.");
    }

    EXPECT_SUCCESS(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::ReturnCompositionSample
//
//  Synopsis:
//      Returns the sample that the composition engine acquired. If the mixer
//      requested a frame that composition was busy with (a very unlikely
//      occurence unless composition is taking a very long time) then we will
//      return that the mixer must be signalled.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
ReturnCompositionSample(
    __out           bool                *pSignalMixer
        // If true, we returned a frame that the mixer wanted, it should be
        // signalled.
    )
{
    HRESULT                   hr          = S_OK;
    TRACEF(&hr);

    StateViewLogicalSample    sampleView  = { 0 };
    bool                      viewApplied = false;
    bool                      signalMixer = false;

    DBG_CODE(StateView        startStateView);
    DBG_CODE(StateView        endStateView);

    do
    {
        signalMixer = false;

        //
        // Can bail out of this loop on entry, after this call, ApplyStateView *must*
        // be called or we will be left with a view allocated to the mixer that cannot
        // be recovered.
        //
        IFC(GetStateView(SampleThreads::CompositionThread, &sampleView));

        StateView   &compositionView = m_stateViews[sampleView.inUseView[SampleThreads::CompositionThread]];

        DBG_CODE(startStateView = compositionView);

        //
        // We want to signal the mixer if we are returning a sample that it has indicated it
        // wants (note, this also means that we are late on a sample because this sample
        // was a valid candidate for invalidation).
        //
        if (   compositionView.mixerSample == compositionView.compositionSample
            && IsValidSampleIndex(compositionView.mixerSample))
        {
            signalMixer = true;
        }

        //
        // Clear the composition sample
        //
        compositionView.compositionSample = kInvalidSample;

        DBG_CODE(endStateView = compositionView);

        viewApplied = ApplyStateView(SampleThreads::CompositionThread, sampleView);
    }
    while(!viewApplied);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "ReturnCompositionSample");

    DBG_CODE(DumpSamples(startStateView, endStateView));

    //
    // Wake up the mixer, composition has returned a sample that it wanted.
    //
    *pSignalMixer = signalMixer;

Cleanup:

    if (FAILED(hr))
    {
        RIP("We should never fail to return a composition sample. This indicates a very serious problem.");
    }

    RRETURN(hr);
}

//
// Private methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::GetStateView
//
//  Synopsis:
//      Allocates a state view for the requesting thread, copies the contents
//      of the one view into the other and returns a structure representing
//      the stateview samples to the caller (this holds which view has been
//      allocated for them as well as what the continuity number is.
//
//      * NOTE: This method assumes only two contending threads - mixer and
//      composition
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
GetStateView(
    __in        SampleThreads::Enum         thread,
        // The thread that is requesting the state view.
    __out       StateViewLogicalSample      *pStateView
        // The returned state view
    )
{
    HRESULT                 hr = S_OK;

    StateViewLogicalSample  stateView = { 0 };
    LONG                    readState = kInvalidView;
    LONG                    interlockState = m_viewState;

    Assert(thread < SampleThreads::NumberOfThreads);
    if (thread >= SampleThreads::NumberOfThreads)
    {
        RIP("Unexpected paramter to GetStateView");
        IFC(E_UNEXPECTED);
    }

    do
    {
        readState = interlockState;

        stateView = TranslateViewState(readState);

        //
        // Check to see whether the current thread is already using a field mask.
        //
        if (stateView.inUseView[thread] != msc_fieldMask)
        {
            RIP("The same thread came in twice and asked for a view, the sample queue can only handle two contending threads (Mixer and Composition)");
            IFC(E_UNEXPECTED);
        }

        BYTE    nextView = NextView(stateView.currentView);

        //
        // If the next view is currently in use by the other thread, then get the view after that
        //
        if (nextView == stateView.inUseView[SampleThreads::CompositionThread - thread])
        {
            nextView = NextView(nextView);
        }

        //
        // Assign this to the in-use view.
        //
        stateView.inUseView[thread] = nextView;

        //
        // We don't advance the continuity number when confirming a view, but it is used in writing
        // a new view (it prevents us hitting the ABA' problem).
        //
        LONG    writeState = TranslateViewState(stateView);

        interlockState =
            InterlockedCompareExchange(
                &m_viewState,
                writeState,
                readState);
    }
    while(readState != interlockState);

    //
    // Now that we have a view, copy the view data over from the current view, this will be
    // used by the calling thread to determine the next state view.
    //
    UINT inUseView = stateView.inUseView[thread];
    Assert(inUseView <= SampleThreads::NumberOfThreads && stateView.currentView <= SampleThreads::NumberOfThreads);
    if (inUseView <= SampleThreads::NumberOfThreads && stateView.currentView <= SampleThreads::NumberOfThreads)
    {
        m_stateViews[inUseView] = m_stateViews[stateView.currentView];
    }

    stateView.continuityNumber++;

    *pStateView = stateView;

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::ApplyStateView
//
//  Synopsis:
//      Applies the state view to the sample queue. The continuity numbers must
//      be correct. If the view could not be applied because a different view
//      has been applied in the meantime (continuity numbers do not match).
//
//  Return:
//      true if the view was applied, false if it was not. If the return is
//      false. the caller is expected to loop back and get the next state view.
//
//------------------------------------------------------------------------------
bool
SampleQueue::
ApplyStateView(
    __in        SampleThreads::Enum         thread,
        // Which state view is being applied
    __in        StateViewLogicalSample      basedOnStateView
        // What the state view is based on
    )
{
    StateViewLogicalSample  stateView      = { 0 };
    LONG                    readState      = kInvalidView;
    LONG                    interlockState = m_viewState;
    bool                    appliedState   = true;

    do
    {
        appliedState = true;

        readState = interlockState;

        stateView = TranslateViewState(readState);

        //
        // Now advance this read state view's continuity number, these two numbers must match or we cannot
        // apply the state (means that another thread applied their state, so we have to go back and get
        // another state view.
        //
        stateView.continuityNumber++;

        if (basedOnStateView.continuityNumber != stateView.continuityNumber)
        {
            appliedState = false;
        }

        if (appliedState)
        {
            //
            // Make the current view be the thread view
            //
            stateView.currentView = basedOnStateView.inUseView[thread];
        }

        //
        // mark our view as invalid
        //
        stateView.inUseView[thread] = msc_fieldMask;

        LONG    writeState = TranslateViewState(stateView);

        interlockState =
            InterlockedCompareExchange(
                &m_viewState,
                writeState,
                readState);
    }
    while(readState != interlockState);

    return appliedState;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::AllocateSample
//
//  Synopsis:
//      Allocates a sample and returns it to the caller bases on the current
//      media type.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
AllocateSample(
    __out           IMFSample           **ppISample
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    CMFMediaBuffer  *pMediaBuffer = NULL;
    IMFSample       *pISample     = NULL;

    if (!m_pIVideoMediaType)
    {
        IFC(WGXERR_AV_NOMEDIATYPE);
    }

    IFC(
        CMFMediaBuffer::Create(
            m_uiID,
            m_continuityNumber,
            m_pIVideoMediaType->GetVideoFormat()->videoInfo.dwWidth,
            m_pIVideoMediaType->GetVideoFormat()->videoInfo.dwHeight,
            FormatFromMediaType(m_pIVideoMediaType),
            m_pRenderDevice,
            m_pMixerDevice,
            m_deviceType,
            &pMediaBuffer));

    IFC(CAVLoader::GetEVRLoadRefAndCreateMedia(&pISample));

    IFC(pISample->AddBuffer(pMediaBuffer));

    IFC(pISample->SetSampleTime(0));

    IFC(pISample->SetSampleDuration(0));

    *ppISample = pISample;
    pISample = NULL;

Cleanup:

    ReleaseSample(&pISample);

    ReleaseInterface(pMediaBuffer);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::ReleaseSample
//
//  Synopsis:
//      Releases the sample and the associated EVR load reference.
//
//------------------------------------------------------------------------------
/*static*/
void
SampleQueue::
ReleaseSample(
    __deref_inout   IMFSample           **ppISample
        // The sample to be released.
    )
{
    if (*ppISample)
    {
        ReleaseInterface(*ppISample);

        IGNORE_HR(CAVLoader::ReleaseEVRLoadRef());
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::CalculateNextTime
//
//  Synopsis:
//      Calculates the next time that we need to be called back. This will be
//      the time of the next sample after currentTime, or the latest sample time
//      if all the sample times are behind currentTime. If there are no valid
//      samples we will return gc_invalidTimerTime.
//
//      NOTE - must be called from the mixer thread!
//
//------------------------------------------------------------------------------
void
SampleQueue::
CalculateNextTime(
    __in        StateViewLogicalSample      sampleView,
        // The logical view sample.
    __in        LONGLONG                    currentTime,
        // The current time
    __out       LONGLONG                    *pLastTime,
        // The largest sample time less than or equal to currentTime
    __out       LONGLONG                    *pNextTime
        // The smallest sample time greater than or equal to currentTime
    ) const
{
    *pLastTime = kInvalidTime;
    *pNextTime = gc_invalidTimerTime;

    const StateView &mixerView = m_stateViews[sampleView.inUseView[SampleThreads::MixerThread]];

    // The time of the next sample is the smallest sample time greater than the current time
    // The time of the last sample is the greatest sample time smaller than our current time
    //
    for(int i = 0; i < kSamples; i++)
    {
        //
        // If the sample time is greater than our current time and smaller than the next time
        // then that becomes our next time.
        //
        if (   mixerView.sampleTimes[i] > currentTime
            && mixerView.sampleTimes[i] < *pNextTime)
        {
            *pNextTime = mixerView.sampleTimes[i];

        }

        if (   mixerView.sampleTimes[i] <= currentTime
            && mixerView.sampleTimes[i] >  *pLastTime)
        {
            *pLastTime = mixerView.sampleTimes[i];
        }
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::ValidateAndGetMixSample
//
//  Synopsis:
//      Checks to see if the given sample exists and if it does that it matches
//      the current device and continuity number for the queue. If it doesn't
//      yet exist, or doesn't match then a new sample is allocated, inserted
//      into the array and returned, otherwise, the existing sample is returned.
//
//------------------------------------------------------------------------------
HRESULT
SampleQueue::
ValidateAndGetMixSample(
    __in        BYTE                        sampleToUse,
    __out       IMFSample                   **ppISample
    )
{
    HRESULT                   hr = S_OK;
    TRACEF(&hr);

    CD3DDeviceLevel1          *pD3DDevice  = NULL;
    IMFSample                 *pINewSample = NULL;
    IMFSample                 *pIReleaseSample = NULL;
    IMFMediaBuffer            *pIMediaBuffer = NULL;
    CMFMediaBuffer            *pMFMediaBuffer = NULL;

    {
        if (m_apISamples[sampleToUse])
        {
            IFC(m_apISamples[sampleToUse]->GetBufferByIndex(0, &pIMediaBuffer));

            IFC(
                pIMediaBuffer->QueryInterface(
                    IID_CMFMediaBuffer,
                    reinterpret_cast<void **>(&pMFMediaBuffer)));

            IFC(pMFMediaBuffer->GetDevice(&pD3DDevice));
        }

        {
            CGuard<CCriticalSection> guard(m_mediaLock);

            //
            // We want to reallocate the sample if we have a different
            // continuity number since this indicates that we have had our
            // media type or device invalidated after this call.
            //
            if (NULL == pMFMediaBuffer || pMFMediaBuffer->GetContinuity() != m_continuityNumber)
            {
                LogAVDataM(
                    AVTRACE_LEVEL_INFO,
                    AVCOMP_SAMPLEQUEUE,
                    "D3DDevice %p, MixerDevice %p",
                    pD3DDevice,
                    m_pMixerDevice);

                if (pMFMediaBuffer)
                {
                    LogAVDataM(
                        AVTRACE_LEVEL_INFO,
                        AVCOMP_SAMPLEQUEUE,
                        "Buffer Continuity %d, MixerContinuity %d",
                        pMFMediaBuffer->GetContinuity(),
                        m_continuityNumber);
                }

                THR(hr = AllocateSample(&pINewSample));

                if (SUCCEEDED(hr))
                {
                    pIReleaseSample = m_apISamples[sampleToUse];
                    m_apISamples[sampleToUse] = pINewSample;
                    m_apISamples[sampleToUse]->AddRef();
                }
            }
            else
            {
                pINewSample = m_apISamples[sampleToUse];
                pINewSample->AddRef();
            }
        }

        IFC(hr);
    }

    *ppISample = pINewSample;
    pINewSample = NULL;

Cleanup:

    ReleaseInterface(pD3DDevice);
    ReleaseSample(&pINewSample);
    ReleaseSample(&pIReleaseSample);
    ReleaseInterface(pMFMediaBuffer);
    ReleaseInterface(pIMediaBuffer);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::DumpState
//
//  Synopsis:
//      Dumps the state for the given method. Only called from debug code.
//
//------------------------------------------------------------------------------
void
SampleQueue::
DumpState(
    __in    PCSTR               method,
    __in    const StateView     &startStateView,
    __in    const StateView     &endStateView,
    __in    LONGLONG            currentTime
    )
{
    DumpTime(method, currentTime);
    DumpSamples(startStateView, endStateView);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::DumpSamples
//
//  Synopsis:
//      Dumps the sample times for the start and end state view.
//
//------------------------------------------------------------------------------
void
SampleQueue::
DumpSamples(
    __in    const StateView     &startStateView,
    __in    const StateView     &endStateView
    )
{
    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "StartView : Times [%I64d, %I64d, %I64d], Composition [%d], Mixer [%d]",
        startStateView.sampleTimes[0],
        startStateView.sampleTimes[1],
        startStateView.sampleTimes[2],
        startStateView.compositionSample,
        startStateView.mixerSample);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "EndView : Times [%I64d, %I64d, %I64d], Composition [%d], Mixer [%d]",
        endStateView.sampleTimes[0],
        endStateView.sampleTimes[1],
        endStateView.sampleTimes[2],
        endStateView.compositionSample,
        endStateView.mixerSample);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::DumpTime
//
//  Synopsis:
//      Dumps the time for the method
//
//------------------------------------------------------------------------------
void
SampleQueue::
DumpTime(
    __in    PCSTR               method,
    __in    LONGLONG            currentTime
    )
{
    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_SAMPLEQUEUE,
        "%s - current time [%I64d]",
        method,
        currentTime);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      SampleQueue::DumpTime
//
//  Synopsis:
//      Get the sample index for the next composition sample
//
//------------------------------------------------------------------------------
BYTE
SampleQueue::
ChooseCompositionSample(
    __in    bool                rechooseSample,
    __in    bool                allowForwardSamples,
    __in    LONGLONG            currentTime,
    __in    const StateView     &stateView
    )
{
    LONGLONG    smallestTimeDelta = gc_invalidTimerTime;
    BYTE        sampleToUse = kInvalidSample;
    BYTE        reservedSample = kInvalidSample;
    BYTE        bestForwardSample = kInvalidSample;
    LONGLONG    bestForwardDelta = LONGLONG_MIN;

    //
    // If the current composition sample is kInvalidSample, or we've been told
    // to pick a new composition sample, then we attempt to
    // find a valid sample for composition
    //
    if (rechooseSample || stateView.compositionSample == kInvalidSample)
    {
        //
        // We search through the sample and find the one that is less than our time
        // but closest to it (and that is not in use by the mixer).
        //
        for(BYTE i = 0; i < kSamples; i++)
        {
            //
            // We can't use a sample that the mixer has reserved. If the
            // composition sample and mixer sample are the same, it means
            // composition has the sample reserved, and the mixer is waiting
            // for the sample
            //
            if (stateView.mixerSample != i || stateView.compositionSample == i)
            {
                LONGLONG sampleTime = stateView.sampleTimes[i];

                if (IsPositiveSampleTime(sampleTime))
                {
                    LONGLONG thisDelta = currentTime - sampleTime;

                    //
                    // Can choose any sample that is this time forward.
                    //
                    if (thisDelta >= 0 && thisDelta < smallestTimeDelta)
                    {
                        sampleToUse = i;
                        smallestTimeDelta = thisDelta;
                    }
                    else if (thisDelta < 0 && thisDelta > bestForwardDelta)
                    {
                        bestForwardSample = i;
                        bestForwardDelta = thisDelta;
                    }
                }
                else if (sampleTime == kReservedForCompositionTime)
                {
                    reservedSample = i;
                }
            }
        }

        if (!IsValidSampleIndex(sampleToUse))
        {
            if (allowForwardSamples && IsValidSampleIndex(bestForwardSample))
            {
                sampleToUse = bestForwardSample;
            }
            else
            {
                sampleToUse = reservedSample;
            }
        }
    }
    //
    // ...otherwise the current composition sample is valid and we choose it,
    // or the current composition sample is kNoPauseSample, which means the
    // render state is paused and there wasn't a valid sample at the time of
    // the pause. We purposely avoid choosing a sample to avoid a glitch.
    // (or we may want to pick the earliest sample in this case)
    //
    else
    {
        sampleToUse = stateView.compositionSample;
    }

    return sampleToUse;
}

