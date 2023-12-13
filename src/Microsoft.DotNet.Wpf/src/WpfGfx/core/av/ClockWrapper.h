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

class EvrPresenter;

interface RenderClock
{
    virtual
    HRESULT
    GetRenderTime(
        __out       LONGLONG    *pCurrentTime,
        __out_opt   DWORD       *pContinuityKey = NULL
        ) = 0;
};

class TimerWrapper : public RenderClock
{
public:
    typedef
    HRESULT
    (EvrPresenter::*InvokeMethod)(
        __in    IMFAsyncResult      *pIResult
        );

    TimerWrapper(
        void
        );

    __override
    ~TimerWrapper(
        void
        );

    HRESULT
    Init(
        __in    UINT                uiID,
        __in    EvrPresenter        *pEvrPresenter,
        __in    InvokeMethod        method
        );

    __override
    void
    Shutdown(
        void
        );

    void
    SetUnderlyingClock(
        __in    IMFClock    *pIMFClock
        );

    IMFClock*
    GetUnderlyingClockNoAddRef(
        void
        );

    void
    SetUnderlyingTimer(
        __in    IMFTimer    *pIMFTimer
        );

    IMFTimer*
    GetUnderlyingTimerNoAddRef(
        void
        );

    void
    ClockStarted(
        void
        );

    void
    ClockPaused(
        void
        );

    void
    ClockStopped(
        void
        );

    HRESULT
    GetMixTime(
        __out       LONGLONG    *pCurrentTime,
        __out_opt   DWORD       *pContinuityKey = NULL
        );

    HRESULT
    GetRenderTime(
        __out       LONGLONG    *pCurrentTime,
        __out_opt   DWORD       *pContinuityKey = NULL
        );

    HRESULT SetTimer(
        __in        DWORD               continuityKey,
        __in        LONGLONG            clockTime
        );

private:
    class PresenterInvoker : public IMFAsyncCallback,
                             public CStateThreadItem
    {
    public:

        PresenterInvoker(
            void
            );

        ~PresenterInvoker(
            void
            );

        HRESULT
        Init(
            __in    UINT                uiID,
            __in    EvrPresenter        *pPresenter,
            __in    TimerWrapper        *pTimerWrapper,
            __in    InvokeMethod        method
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
        // IMFAsyncCallback
        //
        STDMETHOD(GetParameters)(
            __out_ecount(1)       DWORD* pdwFlags,
            __out_ecount(1)       DWORD* pdwQueue
            );

        STDMETHOD(Invoke)(
            __in        IMFAsyncResult* pResult
            );

    protected:

        __override
        void
        Run(
            void
            );

        __override
        bool
        IsAnOwner(
            __in    IUnknown    *pIUnknown
            );

    private:

        //
        // Cannot copy or assign a PresenterInvoker
        //
        PresenterInvoker(
            __in    const PresenterInvoker &
            );

        PresenterInvoker &
        operator=(
            __in    const PresenterInvoker &
            );

        UINT                m_uiID;
        EvrPresenter        *m_pEvrPresenter;
        TimerWrapper        *m_pTimerWrapper;
        InvokeMethod        m_method;
    };

    //
    // Cannot copy or assign a TimerWrapper
    //
    TimerWrapper(
        __in const TimerWrapper        &
        );

    TimerWrapper &
    operator=(
        __in const TimerWrapper &
        );

    HRESULT
    GetTime(
        __in        LONGLONG    defaultTime,
        __out       LONGLONG    *pCurrentTime,
        __out_opt   DWORD       *pContinuityKey = NULL
       );

    void
    CallbackOccurred(
        void
        );

    void
    CancelAndReleaseTimer(
        void
        );

    HRESULT
    DoCallbackThroughStateThread(
        void
        );

    CCriticalSection    m_lock;
    IMFClock            *m_pIMFClock;
    bool                m_isStarted;
    UINT                m_uiID;

    CStateThread        *m_pCStateThread;
    IMFTimer            *m_pIMFTimer;
    IUnknown            *m_pITimerKey;
    EvrPresenter        *m_pEvrPresenter;
    PresenterInvoker    m_presenterInvoker;
    LONGLONG            m_setTimerTime;
    bool                m_timerBeingSet;

    static const LONGLONG msc_timerAccuracy = 10000;
};


