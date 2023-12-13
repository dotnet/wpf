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
//      Provide a structure that holds samples and provides a very light
//      (non-contending and lockless) way to handle and retrieve samples.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#pragma once

namespace SampleThreads
{
    enum Enum
    {
        MixerThread,
        CompositionThread,
        NumberOfThreads
    };
}

class SampleQueue
{
public:

    SampleQueue(
        __in    UINT        uiID
        );

    ~SampleQueue(
        void
        );

    HRESULT
    Init(
        void
        );

    HRESULT
    ChangeMediaType(
        __in_opt    IMFVideoMediaType   *pIVideoMediaType
        );

    HRESULT
    InvalidateDevice(
        __in        CD3DDeviceLevel1    *pRenderDevice,
        __in        CD3DDeviceLevel1    *pMixerDevice,
        __in        D3DDEVTYPE          deviceType
        );

    HRESULT
    GetMixSample(
        __in        LONGLONG            currentTime,
        __deref_out IMFSample           **ppISample
        );

    HRESULT
    ReturnMixSample(
        __in        LONGLONG            currentTime,
        __in        IMFSample           *pISample
        );

    HRESULT
    GetNextSampleTime(
        __in        LONGLONG            currentTime,
        __out       LONGLONG            *pPrevTime,
        __out       LONGLONG            *pNextTime
        );

    HRESULT
    GetSmallestSampleTime(
        __out       LONGLONG            *pSmallestTime
        );

    void
    SignalFlush(
        __in        LONGLONG            currentTime
        );

    HRESULT
    GetCompositionSample(
        __in            bool                rechooseSample,
        __in            LONGLONG            currentTime,
        __deref_out     IMFSample           **ppISample
        );

    HRESULT
    RechooseCompositionSampleFromMixerThread(
        __in            LONGLONG            currentTime
        );

    void
    PauseCompositionSample(
        __in            LONGLONG            currentTime,
        __in            bool                allowForwardSamples
        );

    void
    UnpauseCompositionSample(
        void
        );

    HRESULT
    ReturnCompositionSample(
        __out           bool                *pSignalMixer
        );

private:

    enum
    {
        kSamples    = 3,
        kViewFields = SampleThreads::NumberOfThreads + 2,
        kInvalidSample = static_cast<BYTE>(-1),
        kNoPauseSample = static_cast<BYTE>(-2),
        kInvalidView   = -1,
        kInvalidTime   = -1,
        kReservedForCompositionTime = -2
    };

    //
    // Cannot copy or assign a SampleQueue.
    //
    SampleQueue(
        __in const SampleQueue &
        );

    SampleQueue &
    operator=(
        __in const SampleQueue &
        );

    struct StateViewLogicalSample
    {
        BYTE        currentView;
        BYTE        inUseView[SampleThreads::NumberOfThreads];
        BYTE        continuityNumber;
    };

    struct StateView
    {
        LONGLONG    sampleTimes[kSamples];
        BYTE        compositionSample;
        BYTE        mixerSample;
    };

    HRESULT
    GetStateView(
        __in        SampleThreads::Enum         thread,
        __out       StateViewLogicalSample      *pStateView
        );

    bool
    ApplyStateView(
        __in        SampleThreads::Enum         thread,
        __in        StateViewLogicalSample      basedOnStateView
        );

    HRESULT
    AllocateSample(
        __out       IMFSample           **ppISample
        );

    void
    CalculateNextTime(
        __in        StateViewLogicalSample      sampleView,
        __in        LONGLONG                    currentTime,
        __out       LONGLONG                    *pLastTime,
        __out       LONGLONG                    *pNextTime
        ) const;

    HRESULT
    ValidateAndGetMixSample(
        __in        BYTE                        sampleToUse,
        __out       IMFSample                   **ppISample
        );

    static inline
    StateViewLogicalSample
    TranslateViewState(
        __in    LONG                        viewState
        );

    static inline
    LONG
    TranslateViewState(
        __in    StateViewLogicalSample      logicalSample
        );

    static inline
    BYTE
    NextView(
        __in    BYTE                        view
        );

    static
    void
    ReleaseSample(
        __deref_inout   IMFSample           **ppISample
        );

    void
    DumpState(
        __in    PCSTR               method,
        __in    const StateView     &startStateView,
        __in    const StateView     &endStateView,
        __in    LONGLONG            currentTime
        );

    void
    DumpSamples(
        __in    const StateView     &startStateView,
        __in    const StateView     &endStateView
        );

    void
    DumpTime(
        __in    PCSTR               method,
        __in    LONGLONG            currentTime
        );

    BYTE
    ChooseCompositionSample(
        __in    bool                rechooseSample,
        __in    bool                allowForwardSamples,
        __in    LONGLONG            currentTime,
        __in    const StateView     &stateView
        );

    static inline
    bool
    IsPositiveSampleTime(
        __in    LONGLONG            sampleTime
        );

    static inline
    bool
    IsExpectedSampleTime(
        __in    LONGLONG            sampleTime
        );

    static inline
    bool
    IsValidSampleIndex(
        __in    BYTE                sampleIndex
        );


    UINT                m_uiID;

    LONG                m_viewState;
    StateView           m_stateViews[SampleThreads::NumberOfThreads + 1];

    //
    // The following objects are protected by the media lock.
    // The actual pointer values for m_apISamples is immutable.
    //
    //
    CCriticalSection    m_mediaLock;
    CD3DDeviceLevel1    *m_pRenderDevice;
    CD3DDeviceLevel1    *m_pMixerDevice;
    D3DDEVTYPE          m_deviceType;
    IMFVideoMediaType   *m_pIVideoMediaType;
    LONG                m_continuityNumber;
    IMFSample           *m_apISamples[kSamples];

    static const LONG msc_bitsPerField = 32 / kViewFields;
    static const LONG msc_fieldMask    = (1 << msc_bitsPerField) - 1;
};


#include "SampleQueue.inl"

