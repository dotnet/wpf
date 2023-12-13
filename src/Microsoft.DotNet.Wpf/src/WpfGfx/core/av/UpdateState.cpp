// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "UpdateState.tmh"

/*static*/
HRESULT
UpdateState::
Create(
    __in    MediaInstance       *pMediaInstance,
    __in    CWmpStateEngine     *pCWmpStateEngine,
    __in    UpdateState         **ppUpdateState
    )
{
    HRESULT         hr = S_OK;
    TRACEFID(pMediaInstance->GetID(), &hr);

    UpdateState     *pUpdateState = NULL;

    pUpdateState = new UpdateState(pMediaInstance, pCWmpStateEngine);

    IFCOOM(pUpdateState);

    IFC(pUpdateState->Init());

    *ppUpdateState = pUpdateState;
    pUpdateState = NULL;

Cleanup:
    ReleaseInterface(pUpdateState);

    EXPECT_SUCCESSID(pMediaInstance->GetID(), hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: UpdateState::OpenHelper
//
//  Synopsis: Opens an URL for playback
//
//------------------------------------------------------------------------------
void
UpdateState::
OpenHelper(
    __in LPCWSTR pwszURL
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    if (pwszURL != NULL)
    {
        m_targetActionState = ActionState::Pause;
    }
    else
    {
        m_targetActionState = ActionState::Stop;
    }

    m_targetOcx = true;

    //
    // We need to set the desired rate to 1 (or maybe lower would work even
    // better??) so that the media doesn't finish before we've managed to
    // preroll.
    //
    m_targetRate = 1.0;

    CopyHeapString(pwszURL, &m_targetUrl.m_value);
    m_targetUrl.m_isValid = true;

    m_doClose = false;

    m_didUrlChange = true;
}

//+-----------------------------------------------------------------------------
//
//  Member: UpdateState::SetRateHelper
//
//  Synopsis: Adjust playback speed
//
//------------------------------------------------------------------------------
void
UpdateState::
SetRateHelper(
    __in double dRate
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    if (dRate != 0)
    {
        m_targetActionState = ActionState::Play;
        m_targetRate = dRate;
    }
    else
    {
        m_targetActionState = ActionState::Pause;
    }
}

void
UpdateState::
SetTargetActionState(
    __in    ActionState::Enum   targetActionState
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_targetActionState = targetActionState;
}

void
UpdateState::
SetTargetVolume(
    __in    long                targetVolume
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_targetVolume = targetVolume;
}

void
UpdateState::
SetTargetBalance(
    __in    long                targetBalance
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_targetBalance = targetBalance;
}

void
UpdateState::
SetTargetSeekTo(
    __in    double              targetSeekTo
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_targetSeekTo = targetSeekTo;
}

void
UpdateState::
SetTargetIsScrubbingEnabled(
    __in    bool                isScrubbingEnabled
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_targetIsScrubbingEnabled = isScrubbingEnabled;
}

void
UpdateState::
UpdateTransients(
    void
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_doUpdateTransients = true;
}

void
UpdateState::
Close(
    void
    )
{
    TRACEF(NULL);

    CGuard<CCriticalSection> guard(m_lock);

    m_doClose = true;

    //
    // When the Close request gets executed on CWmpStateEngine, all of the
    // below will get reset. We reset them here, so we can distinguish whether
    // they were set before or after the Close request. If they were set
    // before, then we can ignore them. If they were set after, then we must
    // honour them.
    //
    m_targetActionState.m_isValid = false;
    m_targetOcx.m_isValid = false;
    m_targetUrl.m_isValid = false;
    m_targetUrl.m_value = NULL;
    m_targetRate.m_isValid = false;
    m_targetVolume.m_isValid = false;
    m_targetBalance.m_isValid = false;
    m_targetSeekTo.m_isValid = false;
    m_targetIsScrubbingEnabled.m_isValid = false;
}

HRESULT
UpdateState::
UpdateTransientsSync(
    __in    DWORD       timeOutInMilliseconds,
    __out   bool        *pDidTimeOut
    )
{
    HRESULT     hr = S_OK;
    DWORD       waitResult = WAIT_OBJECT_0;
    LONGLONG    requestTicket = 0LL;
    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_doUpdateTransients = true;
        m_lastRequest++;
        requestTicket = m_lastRequest;
    }

    if (!ResetEvent(m_waitEvent))
    {
        RIP("Can always reset an event unless it has become invalid");
    }

    IFC(m_pCWmpStateEngine->AddItem(this));

    waitResult = WaitForSingleObject(m_waitEvent, timeOutInMilliseconds);

    if (waitResult == WAIT_FAILED)
    {
        RIP("Can always wait for a thread handle unless it has become invalid");
    }
    else if (waitResult == WAIT_TIMEOUT)
    {
        LogAVDataM(
            AVTRACE_LEVEL_VERBOSE,
            AVCOMP_PLAYER,
            "Request timed out");

        *pDidTimeOut = true;
    }
    else if (waitResult == WAIT_OBJECT_0)
    {
        {
            CGuard<CCriticalSection> guard(m_lock);

            if (requestTicket >= m_lastUpdate)
            {
                LogAVDataM(
                    AVTRACE_LEVEL_VERBOSE,
                    AVCOMP_PLAYER,
                    "Event set from previous run - treating as timed out");

                *pDidTimeOut = true;
            }
            else
            {
                *pDidTimeOut = false;
            }

        }
    }
    else
    {
        RIP("Unexpected wait result");
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member: UpdateState::HrFindInterface, CMILCOMBase
//
//------------------------------------------------------------------------------
STDMETHODIMP
UpdateState::
HrFindInterface(
    __in_ecount(1)       REFIID  riid,
    __deref_out void    **ppvObject
    )
{
    RRETURN(E_NOINTERFACE);
}

//+-----------------------------------------------------------------------------
//
//  Member: UpdateState::Run, CStateThreadItem
//
//------------------------------------------------------------------------------
__override
void
UpdateState::
Run(
    void
    )
{
    HRESULT                     hr = S_OK;
    Optional<ActionState::Enum> targetActionState;
    Optional<bool>              targetOcx;
    Optional<double>            targetRate;
    Optional<PWSTR>             targetUrl;
    Optional<long>              targetVolume;
    Optional<long>              targetBalance;
    Optional<double>            targetSeekTo;
    Optional<bool>              targetIsScrubbingEnabled;

    bool                        didUrlChange = false;
    bool                        doUpdateTransients = false;
    bool                        doClose = false;

    LONGLONG                    requestTicket = 0LL;

    TRACEF(&hr);

    {
        CGuard<CCriticalSection> guard(m_lock);

        targetActionState = m_targetActionState;
        targetOcx = m_targetOcx;
        targetUrl = m_targetUrl;
        targetRate = m_targetRate;
        targetVolume = m_targetVolume;
        targetBalance = m_targetBalance;
        targetSeekTo = m_targetSeekTo;
        targetIsScrubbingEnabled = m_targetIsScrubbingEnabled;
        didUrlChange = m_didUrlChange;
        doUpdateTransients = m_doUpdateTransients;
        doClose = m_doClose;


        m_targetActionState.m_isValid = false;
        m_targetOcx.m_isValid = false;
        m_targetUrl.m_isValid = false;
        m_targetUrl.m_value = NULL;
        m_targetRate.m_isValid = false;
        m_targetVolume.m_isValid = false;
        m_targetBalance.m_isValid = false;
        m_targetSeekTo.m_isValid = false;
        m_targetIsScrubbingEnabled.m_isValid = false;
        m_didUrlChange = false;
        m_doUpdateTransients = false;
        m_doClose = false;

        requestTicket = m_lastRequest;
    }

    //
    // We have to do the Close first since that will reset the rest of the
    // state. Then we update the rest of the state with any requests that came
    // in after the Close request.
    //
    if (doClose)
    {
        IFC(m_pCWmpStateEngine->Close());
    }

    if (targetActionState.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetActionState(targetActionState.m_value));
    }

    if (targetOcx.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetOcx(targetOcx.m_value));
    }

    if (targetRate.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetRate(targetRate.m_value));
    }

    if (targetUrl.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetUrl(targetUrl.m_value));
    }

    if (targetVolume.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetVolume(targetVolume.m_value));
    }

    if (targetBalance.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetBalance(targetBalance.m_value));
    }

    if (targetSeekTo.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetSeekTo(targetSeekTo.m_value));
    }

    if (targetIsScrubbingEnabled.m_isValid)
    {
        IFC(m_pCWmpStateEngine->SetTargetIsScrubbingEnabled(targetIsScrubbingEnabled.m_value));
    }

    if (didUrlChange)
    {
        IFC(m_pCWmpStateEngine->InvalidateDidRaisePrerolled());
    }

    if (doUpdateTransients)
    {
        IFC(m_pCWmpStateEngine->UpdateDownloadProgress());
        IFC(m_pCWmpStateEngine->UpdateBufferingProgress());
        IFC(m_pCWmpStateEngine->UpdatePosition());
    }

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_lastUpdate = requestTicket;
    }

    if (!SetEvent(m_waitEvent))
    {
        RIP("Can always set an event unless it has become invalid");
    }

Cleanup:
    if (targetUrl.m_isValid)
    {
        delete[] targetUrl.m_value;
    }
    EXPECT_SUCCESS(hr);

    if (FAILED(hr))
    {
        IGNORE_HR(m_pMediaInstance->GetMediaEventProxy().RaiseEvent(AVMediaFailed, hr));
    }
}

HRESULT
UpdateState::
Init(
    void
    )
{
    HRESULT hr = S_OK;
    TRACEF(&hr);

    IFC(m_lock.Init());

    m_waitEvent
        = CreateEvent(
                NULL,                   // Security Attributes
                TRUE,                   // Manual Reset
                FALSE,                  // Initial State is not signalled
                NULL);                  // Name

    if (NULL == m_waitEvent)
    {
        IFC(GetLastErrorAsFailHR()); // guaranteed to fail
    }

Cleanup:
    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

UpdateState::
UpdateState(
    __in    MediaInstance   *pMediaInstance,
    __in    CWmpStateEngine *pCWmpStateEngine
    ) : m_uiID(pMediaInstance->GetID()),
        m_pMediaInstance(NULL),
        m_pCWmpStateEngine(NULL),
        m_didUrlChange(false),
        m_doUpdateTransients(false),
        m_doClose(false),
        m_waitEvent(NULL),
        m_lastRequest(0LL),
        m_lastUpdate(0LL)
{
    TRACEF(NULL);

    AddRef();
    SetInterface(m_pCWmpStateEngine, pCWmpStateEngine);
    SetInterface(m_pMediaInstance, pMediaInstance);

    m_targetUrl.m_value = NULL;
}

UpdateState::
~UpdateState(
    void
    )
{
    TRACEF(NULL);

    ReleaseInterface(m_pMediaInstance);
    ReleaseInterface(m_pCWmpStateEngine);

    if (m_waitEvent)
    {
        CloseHandle(m_waitEvent);
    }

    if (m_targetUrl.m_isValid)
    {
        delete[] m_targetUrl.m_value;
        m_targetUrl.m_value = NULL;
    }
}

