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

MtExtern(EvrPresenter);

class CMFMediaBuffer;
class CDXVAManagerWrapper;
class CWmpStateEngine;

//
// RenderState
//
namespace RenderState
{
    enum Enum
    {
        Started,
        Stopped,
        Paused,
        RatePaused,
        Shutdown
    };
};

class EvrPresenter;

typedef RealComObject<EvrPresenter, NoDllRefCount> EvrPresenterObj;

//
// EvrPresenter
//
class EvrPresenter :
    public IMFVideoPresenter,
    public IMFVideoDeviceID,
    //public IMFClockRateSink,
    public IMFRateSupport,
    public IMFGetService,
    public IMFTopologyServiceLookupClient,
    public IMFVideoDisplayControl
{
public:

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(EvrPresenter));

    static
    HRESULT
    Create(
        __in    MediaInstance           *pMediaInstance,
        __in    UINT                    resetToken,
        __in    CWmpStateEngine         *pWmpStateEngine,
        __in    CDXVAManagerWrapper     *pDXVAManagerWrapper,
        __deref_out EvrPresenterObj     **ppEvrPresenter
        );

    HRESULT
    Init(
        void
        );

    //
    // IMFVideoPresenter
    //
    STDMETHOD(ProcessMessage)(
        MFVP_MESSAGE_TYPE eMessage,
        ULONG_PTR ulParam
        );

    STDMETHOD(GetCurrentMediaType)(
        __deref_out     IMFVideoMediaType   **ppIMediaType
        );

    //
    // IMFVideoDeviceID
    //
    STDMETHOD(GetDeviceID)(
        __out_ecount(1)           IID                 *pDeviceID
        );

    //
    // IMFTopologyServiceLookupClient
    //
    STDMETHOD(InitServicePointers)(
        __in        IMFTopologyServiceLookup        *pILookup
        );

    STDMETHOD(ReleaseServicePointers)(
        void
        );

    //
    // IMFClockStateSink methods
    //
    STDMETHOD(OnClockStart)(
        MFTIME SystemTime,
        MFTIME StartOffset
        );

    STDMETHOD(OnClockStop)(
        MFTIME SystemTime
        );

    STDMETHOD(OnClockPause)(
        MFTIME SystemTime
        );

    STDMETHOD(OnClockRestart)(
        MFTIME SystemTime
        );

    STDMETHOD(OnClockSetRate)(
        MFTIME SystemTime,
        float flRate
        );

    //
    // IMFRateSupport
    //
    STDMETHOD(GetSlowestRate)(
        MFRATE_DIRECTION    direction,
        BOOL                fAllowThinning,
        __out               float *pflRate
        );

    STDMETHOD(GetFastestRate)(
        MFRATE_DIRECTION    direction,
        BOOL                fAllowThinning,
        __out               float *pflRate
        );

    STDMETHOD(IsRateSupported)(
        BOOL    fAllowThinning,
        float   flRate,
        __out_opt float   *pflNearestRate
        );

    //
    // IMFGetService
    //
    STDMETHOD(GetService)(
        __in        REFGUID guidService,
        __in        REFIID  riid,
        __deref_out LPVOID  *ppvObject
        );

    //
    // IMFVideoDisplayControl
    //
    STDMETHOD(GetNativeVideoSize)(
        __inout_opt SIZE    *pszVideo,
        __inout_opt SIZE    *pszARVideo
        );

    STDMETHOD(GetIdealVideoSize)(
        __inout_opt SIZE    *pszMin,
        __inout_opt SIZE    *pszMax
        ) NOTIMPL_METHOD;

    STDMETHOD(SetVideoPosition)(
        __inout_opt const MFVideoNormalizedRect *pnrcSource,
        __inout_opt const LPRECT                prcDest
        );

    STDMETHOD(GetVideoPosition)(
        __inout_opt MFVideoNormalizedRect *pnrcSource,
        __out       LPRECT               prcDest
        );

    STDMETHOD(SetAspectRatioMode)(
        __in        DWORD                dwAspectRatioMode
        );

    STDMETHOD(GetAspectRatioMode)(
        __out       DWORD                *pdwAspectRatioMode
        );

    STDMETHOD(SetVideoWindow)(
        __in        HWND                 hwndVideo
        );

    STDMETHOD(GetVideoWindow)(
        __deref_out HWND                 *phwndVideo
        );

    STDMETHOD(RepaintVideo)(
        ) NOTIMPL_METHOD;

    STDMETHOD(GetCurrentImage)(
        __inout     BITMAPINFOHEADER    *pBih,
        __deref_out BYTE                **pDib,
        __out       DWORD               *pcbDib,
        __inout_opt LONGLONG            *pTimeStamp
        ) NOTIMPL_METHOD;

    STDMETHOD(SetBorderColor)(
        __in        COLORREF            Clr
        ) NOTIMPL_METHOD;

    STDMETHOD(GetBorderColor)(
        __out       COLORREF            *pClr
        ) NOTIMPL_METHOD;

    STDMETHOD(SetRenderingPrefs)(
        __in        DWORD               dwRenderFlags
        );

    STDMETHOD(GetRenderingPrefs)(
        __out       DWORD               *pdwRenderFlags
        );

    STDMETHOD(SetFullscreen)(
        __in        BOOL                fFullscreen
        );

    STDMETHOD(GetFullscreen)(
        __out       BOOL                *pfFullscreen
        );

    //
    // Normal public methods
    //
    HRESULT
    GetSurfaceRenderer(
        __deref_out IAVSurfaceRenderer      **ppIAVSurfaceRenderer
        );

    DWORD
    DisplayWidth(
        void
        );

    DWORD
    DisplayHeight(
        void
        );

    void
    AvalonShutdown(
        void
        );

    HRESULT
    SignalMixer(
        __in    DWORD                   continuityKey,
        __in    LONGLONG                timeToSignal
        );

    HRESULT
    CancelTimer(
        void
        );

    HRESULT
    NewMixerDevice(
        __in    CD3DDeviceLevel1        *pRenderDevice,
        __in    CD3DDeviceLevel1        *pMixerDevice,
        __in    D3DDEVTYPE              devType
        );

    HRESULT
    TimeCallback(
        __in    IMFAsyncResult          *pIAsyncResult
        );

    HRESULT
    FlushSamples(
        void
        );

    static inline
    bool
    IsSoftwareFallbackError(
        __in    HRESULT                     hr
        );

    static inline
    HRESULT
    TreatNonSoftwareFallbackErrorAsUnknownHardwareError(
        __in    HRESULT                     hr
        );

    static inline
    bool
    IsMandatorySoftwareFallbackError(
        __in    HRESULT                     hr
        );

    inline
    SampleScheduler &
    GetSampleScheduler(
        void
        );

protected:

    EvrPresenter(
        __in    MediaInstance           *pMediaInstance,
        __in    UINT                    resetToken,
        __in    CWmpStateEngine         *pWmpStateEngine,
        __in    CDXVAManagerWrapper     *pDXVAManagerWrapper
        );

    virtual
    ~EvrPresenter();

    void *
    GetInterface(
        __in    REFIID      riid
        );

private:

    //
    // Encapsulated class that provides only IAVSurface Renderer interface.
    // This class interacts with the Composition Engine and, as such, needs
    // to have a different set of locks and data to the ones used by the
    // EVR Presenter we supply to the EVR. To help enforce this separation,
    // the implementation is broken out into a separate class.
    //
    class AVSurfaceRenderer : public IAVSurfaceRenderer
    {
    public:

        AVSurfaceRenderer(
            __in    UINT                uiID,
            __in    CWmpStateEngine     *pWmpStateEngine
            );

        ~AVSurfaceRenderer(
            void
            );

        HRESULT
        Init(
            __in    EvrPresenter        *pEvrPresenter,
            __in    RenderClock         *pRenderClock
            );

        //
        // IUnknown
        //
        STDMETHOD(QueryInterface)(
            __in        REFIID      riid,
            __deref_out void        **ppvObject
            );

        STDMETHOD_(ULONG, AddRef)(
            void
            );

        STDMETHOD_(ULONG, Release)(
            void
            );

        //
        // IAVSurfaceRenderer
        //
        STDMETHOD(BeginComposition)(
            __in    CMilSlaveVideo  *pCaller,
            __in    BOOL            displaySetChanged,
            __in    BOOL            syncChannel,
            __inout LONGLONG        *pLastCompositionSampleTime,
            __out   BOOL            *pbFrameReady
            );

        STDMETHOD(BeginRender)(
            __in_ecount_opt(1) CD3DDeviceLevel1 *pDeviceLevel1,        // NULL OK (in SW)
            __deref_opt_out_ecount(1) IWGXBitmapSource **ppWGXBitmapSource
            );

        STDMETHOD(EndRender)(
            );

        STDMETHOD(EndComposition)(
            __in    CMilSlaveVideo  *pCaller
            );

        STDMETHOD(GetContentRect)(
            __out_ecount(1) MilPointAndSizeL *prcContent
            );

        STDMETHOD(GetContentRectF)(
            __out_ecount(1) MilPointAndSizeF *prcContent
            );

        HRESULT
        ChangeMediaType(
            __in_opt    IMFVideoMediaType       *pIVideoMediaType
            );

        void
        Shutdown(
            void
            );

        void
        SignalFallbackFailure(
            __in        HRESULT                 hr
            );

        inline
        CD3DDeviceLevel1 *
        CurrentRenderDevice(
            void
            );

    private:

        //
        // Cannot copy or assign an AVSurfaceRenderer
        //
        AVSurfaceRenderer(
            __in const AVSurfaceRenderer        &
            );

        AVSurfaceRenderer &
        operator=(
            __in const AVSurfaceRenderer &
            );

        HRESULT
        ChooseSample(
            __in        LONGLONG    currentTime,
            __in        bool        isPaused,
            __out       LONGLONG    *pThisSampleTime
            );

        HRESULT
        GetSWDevice(
            __deref_out CD3DDeviceLevel1        **ppD3DDevice
            );

        HRESULT
        GetHWDevice(
            __in        UINT                        adapter,
            __in        bool                        forceMultithreaded,
            __deref_out CD3DDeviceLevel1            **ppD3DDevice
            );

        HRESULT
        NewRenderDevice(
            __in    CD3DDeviceLevel1            *pNewRenderDevice
            );

        inline
        HRESULT
        FallbackToSoftwareIfNecessary(
            __in    HRESULT                     hr
            );

        HRESULT
        FallbackToSoftware(
            void
            );

        HRESULT
        SignalMixer(
            void
            );

        static inline
        bool
        IsTransientError(
            __in    HRESULT         hr
            );

        HRESULT
        AddCompositingResource(
            __in    CMilSlaveVideo  *pCMilSlaveVideo
            );

        void
        RemoveCompositingResource(
            __in    CMilSlaveVideo  *pCMilSlaveVideo
            );

        void
        DumpResourceList(
            void
            );

        HRESULT
        PostCompositionPassCleanup(
            void
            );

        //
        // This data is only touched by the composition thread (or is immutable).
        //
        UINT                m_uiID;
        UINT                m_ResetToken;
        EvrPresenter        *m_pEvrPresenter;
        RenderClock         *m_pRenderClock;
        CD3DDeviceLevel1    *m_pCurrentRenderDevice;
        CD3DDeviceLevel1    *m_pSoftwareDevice;
        CMFMediaBuffer      *m_pRenderedBuffer;
        CD3DDeviceLevel1    *m_pCompositionRenderDevice;
        bool                m_haveMultipleCompositionDevices;
        LONGLONG            m_deviceContinuity;
        LONGLONG            m_lastHardwareDeviceContinuity;


        //
        // Composition Lock is used for state that is generally accessed by the
        // composition thread and sometimes by the media thread.
        //
        CCriticalSection    m_compositionLock;
        DWORD               m_dwWidth;
        DWORD               m_dwHeight;
        CWmpStateEngine     *m_pWmpStateEngine;
        bool                m_isPaused;
        LONGLONG            m_lastSampleTime;
        HRESULT             m_fallbackFailure;
        LONGLONG            m_lastBeginCompositionTime;
        CDummySource        *m_pDummySource;
        UniqueList<CMilSlaveVideo*>    m_compositingResources;

        bool                m_syncChannel;

        //
        // Media Lock is used for state that is generally accessed by the media
        // thread and sometimes by the composition lock.
        //
        CCriticalSection    m_mediaLock;

        static const UINT                       msc_defaultAdapter = 0;
    };

    struct ProcessSamplesData
    {
        ProcessSamplesData(
            void
            );

        LONGLONG    nextTime;
        DWORD       continuityKey;
        HRESULT     fallbackFailure;
        bool        mediaFinished;
    };

    //
    // Cannot copy or assign a EvrPresenter.
    //
    EvrPresenter(
        __in    const EvrPresenter     &
        );

    EvrPresenter &
    operator=(
        __in    const EvrPresenter     &
        );

    HRESULT
    ClockStarted(
        void
        );

    HRESULT
    Flush(
        void
        );

    HRESULT
    ProcessInvalidateMediaType(
        void
        );

    HRESULT
    GetBestMediaType(
        __out       IMFMediaType    **ppIBestMediaType
        );

    HRESULT
    SetMediaType(
        __in_opt    IMFMediaType    *pIMediaType
        );

    HRESULT
    ProcessInputNotify(
        void
        );

    HRESULT
    InvalidateMediaType(
        void
        );

    HRESULT
    ProcessOneSample(
        __in    LONGLONG    currentTime
        );

    HRESULT
    ProcessSamples(
        __inout     ProcessSamplesData          *pProcessSamplesData,
        __in        LONGLONG                    currentTime = gc_invalidTimerTime
        );

    void
    ProcessSampleDataOutsideOfLock(
        __in        const ProcessSamplesData    &processSamplesData
        );

    HRESULT
    BeginStreaming(
        void
        );

    HRESULT
    EndStreaming(
        void
        );

    HRESULT
    EndOfStream(
        void
        );

    HRESULT
    Step(
        __in        DWORD               stepCount
        );

    HRESULT
    CancelStep(
        void
        );

    HRESULT
    ValidateMixerHasCorrectType(
        __in        IMFTransform        *pIMixer
        );

    void
    MediaFinished(
        void
        );

    HRESULT
    NotifyEvent(
        long EventCode,
        __in_opt LONG_PTR EventParam1,
        __in_opt LONG_PTR EventParam2
        );

    HRESULT
    NotifyStateEngineOfState(
        RenderState::Enum   state
        );

    void
    CancelAndReleaseTimer(
        void
        );

    static inline
    HRESULT
    CheckForShutdown(
        __in    RenderState::Enum       renderState
        );

    UINT                    m_uiID;
    UINT m_ResetToken;
    CDXVAManagerWrapper     *m_pDXVAManagerWrapper;
    MediaInstance           *m_pMediaInstance;
    HWND                    m_videoWindow;
    MFVideoNormalizedRect   m_nrcSource;
    RECT                    m_rcDest;

    CCriticalSection        m_csEntry;
    IMediaEventSink         *m_pIMediaEventSink;
    CWmpStateEngine         *m_pWmpStateEngine;
    IMFTransform            *m_pIMixer;
    IMFVideoMediaType       *m_pIVideoMediaType;
    RenderState::Enum       m_renderState;
    bool                    m_endStreaming;
    bool                    m_notifiedOfSample;
    LONGLONG                m_prevMixSampleTime;
    LONGLONG                m_finalSampleTime;

    TimerWrapper            m_timerWrapper;

    DWORD                   m_aspectRatioMode;

    //
    // The following instance members are internally locking
    //
    SampleScheduler         m_sampleScheduler;
    AVSurfaceRenderer       m_surfaceRenderer;

    static const LONGLONG   msc_timerAccuracy = 10000;
    static const float      msc_defaultMaxRate;
    static const float      msc_maxThinningRate;

    static const D3DFORMAT  msc_d3dFormatOrder[];
};

#include "EvrPresenter.inl"

