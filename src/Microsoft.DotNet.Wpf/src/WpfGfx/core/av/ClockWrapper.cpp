// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "ClockWrapper.tmh"

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::SetUnderlyingClock
//
//  Synopsis:
//      Set the underlying instance of IMFClock
//
//------------------------------------------------------------------------------
void
TimerWrapper::
SetUnderlyingClock(
    __in    IMFClock    *pIMFClock
    )
{
    TRACEF(NULL);

    IMFClock        *pIClockRelease = NULL;

    {
        CGuard<CCriticalSection>    guard(m_lock);

        pIClockRelease = m_pIMFClock;
        m_pIMFClock = pIMFClock;

        if (m_pIMFClock)
        {
            m_pIMFClock->AddRef();
        }
    }

    ReleaseInterface(pIClockRelease);
}

IMFClock*
TimerWrapper::
GetUnderlyingClockNoAddRef(
    void
    )
{
    return m_pIMFClock;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::ClockPaused
//
//  Synopsis:
//      Called when EvrPresenter gets an OnClockPause
//
//------------------------------------------------------------------------------
void
TimerWrapper::
ClockPaused(
    void
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection>    guard(m_lock);

        m_isStarted = false;
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::ClockStopped
//
//  Synopsis:
//      Called when EvrPresenter gets an OnClockStop
//
//------------------------------------------------------------------------------
void
TimerWrapper::
ClockStopped(
    void
    )
{
    TRACEF(NULL);

    {
        CGuard<CCriticalSection>    guard(m_lock);

        m_isStarted = false;
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::GetTime
//
//  Synopsis:
//      Gets the current time, taking into account the state
//
//------------------------------------------------------------------------------
HRESULT
TimerWrapper::
GetTime(
    __in        LONGLONG    defaultTime,
    __out       LONGLONG    *pCurrentTime,
    __out_opt   DWORD       *pContinuityKey
    )
{
    HRESULT         hr = S_OK;
    IMFClock        *pIMFClock = NULL;
    DWORD           continuityKey = 0;
    MFTIME          systemTime = 0;
    bool            isStarted = false;

    {
        CGuard<CCriticalSection>    guard(m_lock);

        pIMFClock = m_pIMFClock;

        if (pIMFClock)
        {
            pIMFClock->AddRef();
        }

        isStarted = m_isStarted;
    }

    if (pIMFClock && isStarted)
    {
        hr = pIMFClock->GetContinuityKey(&continuityKey);

        if (SUCCEEDED(hr))
        {
            hr = pIMFClock->GetCorrelatedTime(continuityKey, pCurrentTime, &systemTime);
        }

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_CLOCKWRAPPER,
            "GetTime() : [%I64d]",
            *pCurrentTime);

        //
        // Get Correlated time can periodically return S_FALSE.
        //
        if (hr == S_FALSE)
        {
            hr = S_OK;
        }
    }

    if (!pIMFClock || !isStarted || hr == MF_E_SHUTDOWN)
    {
        //
        // If we're started but we don't have a clock then we are in clockless mode, which means
        // we always return gc_invalidTimerTime for the current time.
        //
        if (isStarted && !pIMFClock)
        {
            *pCurrentTime = gc_invalidTimerTime;
        }
        else
        {
            *pCurrentTime = defaultTime;
        }

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_CLOCKWRAPPER,
            "GetTime() : [%I64d] (defaulted), clock? %d, isStarted? %d, shutdown? %d",
            *pCurrentTime, pIMFClock != NULL,
            isStarted,
            hr == MF_E_SHUTDOWN);

        if (hr == MF_E_SHUTDOWN)
        {
            hr = S_OK;
        }
    }

    IFC(hr);

    if (pContinuityKey)
    {
        *pContinuityKey = continuityKey;
    }


Cleanup:
    ReleaseInterface(pIMFClock);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::GetRenderTime
//
//  Synopsis:
//      Gets the current time, taking into account the state. If the time is
//      not available, the time defaults to gc_invalidTimerTime. This is the
//      correct default for render times as it means the rendering code will
//      grab the latest frame.
//
//------------------------------------------------------------------------------
HRESULT
TimerWrapper::
GetRenderTime(
    __out       LONGLONG    *pCurrentTime,
    __out_opt   DWORD       *pContinuityKey
    )
{
    RRETURN(GetTime(gc_invalidTimerTime, pCurrentTime, pContinuityKey));
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::GetMixTime
//
//  Synopsis:
//      Gets the current time, taking into account the state. If the time is
//      not available, the time defaults to 0. The is the correct default for
//      mix times as it means the mixing code will not discard frames since
//      0 is the earliest possible sample time.
//
//      The one exception is that if we are running in clockless mode
//      (RenderState::Started, but no clock) then we will return
//      gc_invalidTimerTime so that the mixing code will discard frames
//      whenever new ones are received.
//
//      We want to always display the latest frame received, so we don't want
//      to discard the latest frame. SampleQueue always discards the earliest
//      sample time so it's okay that we return gc_invalidTimerTime even though
//      this marks all samples valid for discard.
//
//------------------------------------------------------------------------------
HRESULT
TimerWrapper::
GetMixTime(
    __out       LONGLONG    *pCurrentTime,
    __out_opt   DWORD       *pContinuityKey
    )
{
    RRETURN(GetTime(0LL, pCurrentTime, pContinuityKey));
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// TimerWrapper implementation
//
TimerWrapper::
TimerWrapper(
    void
    ) : m_uiID(0),
        m_pIMFClock(NULL),
        m_isStarted(false),
        m_pIMFTimer(NULL),
        m_pITimerKey(NULL),
        m_setTimerTime(gc_invalidTimerTime),
        m_timerBeingSet(false)
{}

__override
TimerWrapper::
~TimerWrapper(
    void
    )
{
    Shutdown();

    ReleaseInterface(m_pCStateThread);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::Init
//
//  Synopsis:
//      Initialize the TimerWrapper
//
//------------------------------------------------------------------------------
HRESULT
TimerWrapper::
Init(
    __in    UINT                uiID,
    __in    EvrPresenter        *pEvrPresenter,
    __in    InvokeMethod        method
    )
{
    HRESULT hr = S_OK;
    m_uiID = uiID;
    TRACEF(&hr);

    IFC(m_lock.Init());

    IFC(m_presenterInvoker.Init(uiID, pEvrPresenter, this, method));

    IFC(CStateThread::CreateApartmentThread(&m_pCStateThread));

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::SetUnderlyingTimer
//
//  Synopsis:
//      Set the underlying instance of IMFTimer
//
//------------------------------------------------------------------------------
void
TimerWrapper::
SetUnderlyingTimer(
    __in    IMFTimer    *pIMFTimer
    )
{
    TRACEF(NULL);

    //
    // We need to cancel our old timer
    //
    CancelAndReleaseTimer();

    {
        CGuard<CCriticalSection>    guard(m_lock);

        if (pIMFTimer)
        {
            m_pIMFTimer = pIMFTimer;
            m_pIMFTimer->AddRef();
        }
    }
}

IMFTimer*
TimerWrapper::
GetUnderlyingTimerNoAddRef(
    void
    )
{
    return m_pIMFTimer;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::ClockStarted
//
//  Synopsis:
//      Called when EvrPresenter gets an OnClockStart
//      When the clock is restarted, we set the timer to wake up immediately, if
//      we we're waiting for a callback, but never got it.
//
//
//------------------------------------------------------------------------------
void
TimerWrapper::
ClockStarted(
    void
    )
{
    HRESULT         hr = S_OK;
    DWORD           continuityKey = 0;
    IMFClock        *pIMFClock = NULL;
    bool            doCallback = false;

    TRACEF(&hr);

    //
    // If a callback never happened because the clock was stopped, we'll
    // set the timer to callback immediately.
    //
    {
        CGuard<CCriticalSection>    guard(m_lock);

        m_isStarted = true;

        if (m_setTimerTime != gc_invalidTimerTime)
        {
            m_setTimerTime = gc_invalidTimerTime;
            doCallback = true;

            if (m_pIMFClock != NULL)
            {
                pIMFClock = m_pIMFClock;
                pIMFClock->AddRef();
            }
        }
    }

    if (doCallback && pIMFClock != NULL)
    {
        IFC(pIMFClock->GetContinuityKey(&continuityKey));

        IFC(SetTimer(
                continuityKey,
                0
            ));
    }
    //
    // If we're started but we don't have a clock, then we're in clockless mode.
    // We use the state thread to do the callback.
    //
    else if (doCallback)
    {
        IFC(DoCallbackThroughStateThread());
    }

Cleanup:
    ReleaseInterface(pIMFClock);

    EXPECT_SUCCESS(hr);
    //
    //
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::Shutdown
//
//  Synopsis:
//      Shutdown and release all IUnknowns
//
//------------------------------------------------------------------------------
__override
void
TimerWrapper::
Shutdown(
    void
    )
{
    IMFClock        *pIMFClockRelease = NULL;
    IMFTimer        *pIMFTimerRelease = NULL;
    IUnknown        *pITimerKeyRelease = NULL;
    TRACEF(NULL);

    if (m_lock.IsValid())
    {
        CancelAndReleaseTimer();

        {
            CGuard<CCriticalSection>    guard(m_lock);

            pIMFClockRelease = m_pIMFClock;
            m_pIMFClock = NULL;

            pIMFTimerRelease = m_pIMFTimer;
            m_pIMFTimer = NULL;

            pITimerKeyRelease = m_pITimerKey;
            m_pITimerKey = NULL;

            //
            // We need to cancel ourselves so that the state thread doesn't call
            // us back after we've been shutdown.
            //
            if (m_pCStateThread)
            {
                m_pCStateThread->CancelAllItemsWithOwner(static_cast<IMFAsyncCallback*>(&m_presenterInvoker));
            }

        }
    }
    else
    {
        Assert(!m_pIMFClock);
        Assert(!m_pIMFTimer);
        Assert(!m_pITimerKey);
        Assert(!m_pCStateThread);
    }

    ReleaseInterface(pIMFClockRelease);
    ReleaseInterface(pIMFTimerRelease);
    ReleaseInterface(pITimerKeyRelease);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::SetTimer
//
//  Synopsis:
//      Set the timer to call us back later.
//
//------------------------------------------------------------------------------
HRESULT
TimerWrapper::
SetTimer(
    __in        DWORD               continuityKey,
    __in        LONGLONG            clockTime
    )
{
    HRESULT     hr = S_OK;
    TRACEF(&hr);

    bool        shouldSetTime       = false;
    IMFTimer    *pIMFTimer          = NULL;
    IUnknown    *pITimerKey         = NULL;

    {
        CGuard<CCriticalSection>    guard(m_lock);

        //
        // If we're not started, we set m_setTimerTime and
        // wait for the clock to be started
        //
        if (!m_isStarted)
        {
            m_setTimerTime = 0;
        }
        //
        // If we already have the timer set to wake us up earlier than the
        // indicated time, then don't bother setting the timer.
        // gc_invalidTimerTime is special cased since this means to cancel
        // the timer
        //
        else if (m_setTimerTime > clockTime || clockTime == gc_invalidTimerTime)
        {
            m_setTimerTime = clockTime;

            shouldSetTime = !m_timerBeingSet;

            if (shouldSetTime)
            {
                m_timerBeingSet = true;
            }
        }


        if (shouldSetTime == false)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_CLOCKWRAPPER,
                "Decided not to set time - Current [%I64d], set [%I64d], timer being set [%d], m_isStarted [%d]",
                m_setTimerTime,
                clockTime,
                m_timerBeingSet,
                m_isStarted);
        }
    }

    while(shouldSetTime)
    {
        pIMFTimer          = NULL;
        pITimerKey         = NULL;

        {
            CGuard<CCriticalSection>    guard(m_lock);

            pITimerKey = m_pITimerKey;
            m_pITimerKey = NULL;

            pIMFTimer = m_pIMFTimer;

            //
            // Effectively, if the timer is NULL, then we can't set the time.
            //
            if (NULL == pIMFTimer)
            {
                shouldSetTime = false;
                m_timerBeingSet = false;
            }
            else
            {
                pIMFTimer->AddRef();
            }
        }

        if (shouldSetTime)
        {
            //
            // Cancel timer can fail, but, there is no way to reliably
            // set a timer without cancelling a timer. (The timer is
            // automatically cancelled when doing an invoke, but this
            // can clash with another timer invocation).
            //
            if (pITimerKey)
            {
                LogAVDataM(
                    AVTRACE_LEVEL_INFO,
                    AVCOMP_CLOCKWRAPPER,
                    "Cancelling timer.");

                pIMFTimer->CancelTimer(pITimerKey);
            }

            {
                CGuard<CCriticalSection>    guard(m_lock);

                //
                // If the timer was the same
                //
                if (m_pIMFTimer == pIMFTimer)
                {
                    //
                    // If the m_timeToSet variable is set to gc_invalidTimerTime,
                    // then it indicates that we want to cancel the timer.
                    //
                    if (m_setTimerTime != gc_invalidTimerTime)
                    {
                        LogAVDataM(
                            AVTRACE_LEVEL_INFO,
                            AVCOMP_CLOCKWRAPPER,
                            "Setting timer to %I64d",
                            m_setTimerTime + msc_timerAccuracy);

                        //
                        // We always add the minimum timer accuracy because otherwise,
                        // we can be called back before the time has quite elapsed.
                        //
                        THR(hr = m_pIMFTimer->SetTimer(
                                        continuityKey,
                                        m_setTimerTime + msc_timerAccuracy,
                                        &m_presenterInvoker,
                                        NULL,
                                        &m_pITimerKey));

                        //
                        // If this fails, we want to bail out, not fail to set
                        // a timer ever again.
                        //
                        m_timerBeingSet = false;
                        shouldSetTime = false;

                        //
                        // We can safely ignore shutdowns and clock stopped warnings.
                        // If we ever get a new clock and/or a start, we'll reset the
                        // timer as soon as the clock is started.
                        //
                        if (hr == MF_E_SHUTDOWN || hr == MF_S_CLOCK_STOPPED)
                        {
                             hr = S_OK;
                        }

                        IFC(hr);
                    }
                    else
                    {
                        //
                        // No need to set timer, it has been cancelled while we
                        // were in this loop.
                        //
                        m_timerBeingSet = false;
                        shouldSetTime = false;
                    }
                }
            }
        }
        //
        // If we can't schedule a callback due to a NULL timer then we get the
        // state thread to do the callback
        //
        else
        {
            IFC(DoCallbackThroughStateThread());
        }

        ReleaseInterface(pITimerKey);
        ReleaseInterface(pIMFTimer);

        if (shouldSetTime)
        {
            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_CLOCKWRAPPER,
                "Looping to set timer again. (Time was changed while in timer loop).");
        }
    }

Cleanup:

    ReleaseInterface(pITimerKey);
    ReleaseInterface(pIMFTimer);

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::CallbackOccurred
//
//  Synopsis:
//      This should be called first thing from the callback.
//
//------------------------------------------------------------------------------
void
TimerWrapper::
CallbackOccurred(
    void
    )
{
    CGuard<CCriticalSection>    guard(m_lock);

    //
    // This will also have the effect of cancelling any time set that is current occuring and it
    // guarantees that our next callback will set the time.
    //
    m_setTimerTime = gc_invalidTimerTime;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::CancelAndReleaseTimer     (private)
//
//  Synopsis:
//      Guarantees that the timer is released and cancelled, or shortly will, due
//      to a race with SignalMixer.
//
//------------------------------------------------------------------------------
void
TimerWrapper::
CancelAndReleaseTimer(
    void
    )
{
    TRACEF(NULL);

    IMFTimer        *pIReleaseTimer = NULL;
    IUnknown        *pIReleaseTimerKey = NULL;
    bool            settingTimer    = false;

    {
        CGuard<CCriticalSection>    guard(m_lock);

        pIReleaseTimer = m_pIMFTimer;
        m_pIMFTimer = NULL;

        pIReleaseTimerKey = m_pITimerKey;
        m_pITimerKey = NULL;
        settingTimer = m_timerBeingSet;

        //
        // The insures that if SignalMixer is running in another thread,
        // IT will cancel the timer and not set a new time.
        //
        m_setTimerTime = gc_invalidTimerTime;

        if (!settingTimer && pIReleaseTimerKey && pIReleaseTimer)
        {
            m_timerBeingSet = true;
        }
    }

    //
    // Fairly complex interaction here, if we remove the timer
    // while the timer being set loop is running and we remove
    // the timer, then it will do the final cancel timer. If
    // we shutdown and a timer was set otherwise though, we
    // want to cancel it.
    //
    if (!settingTimer && pIReleaseTimerKey && pIReleaseTimer)
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_CLOCKWRAPPER,
            "Cancelling timer in release service pointers.");

        (void)pIReleaseTimer->CancelTimer(pIReleaseTimerKey);

        CGuard<CCriticalSection> guard(m_lock);

        m_timerBeingSet = false;
    }

    ReleaseInterface(pIReleaseTimerKey);
    ReleaseInterface(pIReleaseTimer);
}

HRESULT
TimerWrapper::
DoCallbackThroughStateThread(
    void
    )
{
    HRESULT hr = S_OK;

    IFC(m_pCStateThread->AddItem(&m_presenterInvoker));

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}


//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// PresenterInvoker implementation
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::PresenterInvoker     ctor
//
//  Synopsis:
//      Creates a presenter invoker. This is a class that invokes the presenter
//      on a designated method when anything that takes the IMFAsyncCallback is
//      called.
//
//------------------------------------------------------------------------------
TimerWrapper::PresenterInvoker::
PresenterInvoker(
    void
    ) : m_uiID(0),
        m_pEvrPresenter(NULL),
        m_pTimerWrapper(NULL),
        m_method(NULL)
{}

TimerWrapper::PresenterInvoker::
~PresenterInvoker(
    void
    )
{
    //
    // Not reference counted
    //
    m_pEvrPresenter = NULL;
    m_pTimerWrapper = NULL;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      TimerWrapper::PresenterInvoker::Init
//
//  Synopsis:
//      Supplies the presenter and clock wrapper instance.
//
//------------------------------------------------------------------------------
HRESULT
TimerWrapper::PresenterInvoker::
Init(
    __in    UINT                uiID,
    __in    EvrPresenter        *pPresenter,
        // Presenter instance.
    __in    TimerWrapper        *pTimerWrapper,
        // TimerWrapper instance
    __in    InvokeMethod        method
        // callback method
    )
{
    m_uiID = uiID;

    //
    // Not reference counted
    //
    m_pEvrPresenter = pPresenter;
    m_pTimerWrapper = pTimerWrapper;
    m_method = method;

    return S_OK;
}

//
// IUnknown
//
STDMETHODIMP
TimerWrapper::PresenterInvoker::
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
        pv = static_cast<IUnknown *>(static_cast<IMFAsyncCallback *>(this));
    }
    else if (riid == __uuidof(IMFAsyncCallback))
    {
        pv = static_cast<IMFAsyncCallback *>(this);
    }

    if (pv)
    {
        if (ppvObject)
        {
            *ppvObject = pv;
            PresenterInvoker::AddRef();
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
TimerWrapper::PresenterInvoker::
AddRef(
    void
    )
{
    return static_cast<IMFVideoPresenter *>(m_pEvrPresenter)->AddRef();
}

STDMETHODIMP_(ULONG)
TimerWrapper::PresenterInvoker::
Release(
    void
    )
{
    return static_cast<IMFVideoPresenter *>(m_pEvrPresenter)->Release();
}

__override
void
TimerWrapper::PresenterInvoker::
Run(
    void
   )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    m_pTimerWrapper->CallbackOccurred();

    if (m_method)
    {
        IFC((m_pEvrPresenter->*m_method)(NULL));
    }
Cleanup:
    EXPECT_SUCCESS(hr);
    //
    //
}

__override
bool
TimerWrapper::PresenterInvoker::
IsAnOwner(
    __in    IUnknown    *pIUnknown
    )
{
    return static_cast<IUnknown*>(static_cast<IMFAsyncCallback*>(this)) == pIUnknown;
}

//
// IMFAsyncCallback
//
STDMETHODIMP
TimerWrapper::PresenterInvoker::
GetParameters(
    __out_ecount(1) DWORD* pdwFlags,
    __out_ecount(1) DWORD* pdwQueue
    )
{
    return S_OK;
}

STDMETHODIMP
TimerWrapper::PresenterInvoker::
Invoke(
    __in        IMFAsyncResult* pResult
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    m_pTimerWrapper->CallbackOccurred();

    if (m_method)
    {
        IFC((m_pEvrPresenter->*m_method)(pResult));
    }

Cleanup:
    //

    RRETURN(hr);
}



