// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "PresenterWrapper.tmh"

PresenterWrapper::
PresenterWrapper(
    __in    UINT        id
    ) : m_uiID(id),
        m_pEvrPresenter(NULL),
        m_isScrubbing(false),
        m_isFakePause(false),
        m_isStopToPauseFreeze(false)
{
    TRACEF(NULL);
}

PresenterWrapper::
~PresenterWrapper(
    void
    )
{
    TRACEF(NULL);
    ReleaseInterface(m_pEvrPresenter);
}

HRESULT
PresenterWrapper::
Init(
    void
    )
{
    RRETURN(m_lock.Init());
}

void
PresenterWrapper::
BeginScrub(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_isScrubbing = true;

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        pEvrPresenter->GetSampleScheduler().BeginScrub();
        ReleaseInterface(pEvrPresenter);
    }
}

void
PresenterWrapper::
EndScrub(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_isScrubbing = false;

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        pEvrPresenter->GetSampleScheduler().EndScrub();
        ReleaseInterface(pEvrPresenter);
    }
}

void
PresenterWrapper::
BeginFakePause(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);
        m_isFakePause = true;

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        pEvrPresenter->GetSampleScheduler().BeginFakePause();
        ReleaseInterface(pEvrPresenter);
    }
}

void
PresenterWrapper::
EndFakePause(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_isFakePause = false;

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        pEvrPresenter->GetSampleScheduler().EndFakePause();
        ReleaseInterface(pEvrPresenter);
    }
}

void
PresenterWrapper::
BeginStopToPauseFreeze(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_isStopToPauseFreeze = true;

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        pEvrPresenter->GetSampleScheduler().BeginStopToPauseFreeze();
        ReleaseInterface(pEvrPresenter);
    }
}

void
PresenterWrapper::
EndStopToPauseFreeze(
    bool doFlush
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        m_isStopToPauseFreeze = false;

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        pEvrPresenter->GetSampleScheduler().EndStopToPauseFreeze(doFlush);
        ReleaseInterface(pEvrPresenter);
    }
}

HRESULT
PresenterWrapper::
GetSurfaceRenderer(
    __deref_out_opt IAVSurfaceRenderer  **ppISurfaceRenderer
    )
{
    HRESULT         hr = S_OK;
    EvrPresenterObj *pEvrPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        IFC(pEvrPresenter->GetSurfaceRenderer(ppISurfaceRenderer));
    }

Cleanup:
    ReleaseInterface(pEvrPresenter);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

DWORD
PresenterWrapper::
DisplayWidth(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    DWORD           width = 0;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        width = pEvrPresenter->DisplayWidth();
        ReleaseInterface(pEvrPresenter);
    }

    return width;
}

DWORD
PresenterWrapper::
DisplayHeight(
    void
    )
{
    EvrPresenterObj *pEvrPresenter = NULL;
    DWORD           height = 0;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        SetInterface(pEvrPresenter, m_pEvrPresenter);
    }

    if (pEvrPresenter)
    {
        height = pEvrPresenter->DisplayHeight();
        ReleaseInterface(pEvrPresenter);
    }

    return height;
}

void
PresenterWrapper::
SetPresenter(
    __in    EvrPresenterObj     *pEvrPresenter
    )
{
    bool                isScrubbing = false;
    bool                isFakePause = false;
    bool                isStopToPauseFreeze = false;
    EvrPresenterObj     *pOldPresenter = NULL;
    TRACEF(NULL);

    {
        CGuard<CCriticalSection> guard(m_lock);

        SetInterface(pOldPresenter, m_pEvrPresenter);
        ReplaceInterface(m_pEvrPresenter, pEvrPresenter);

        isScrubbing = m_isScrubbing;
        isFakePause = m_isFakePause;
        isStopToPauseFreeze = m_isStopToPauseFreeze;
    }

    if (pEvrPresenter)
    {
        if (isScrubbing)
        {
            pEvrPresenter->GetSampleScheduler().BeginScrub();
        }

        if (isFakePause)
        {
            pEvrPresenter->GetSampleScheduler().BeginFakePause();
        }

        if (isStopToPauseFreeze)
        {
            pEvrPresenter->GetSampleScheduler().BeginStopToPauseFreeze();
        }
    }

    if (pOldPresenter)
    {
        pOldPresenter->AvalonShutdown();
        ReleaseInterface(pOldPresenter);
    }
}

