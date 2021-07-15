// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "CompositionNotifier.tmh"

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// CompositionNotifier implementation
//
CompositionNotifier::
CompositionNotifier(
    void
    ) : m_uiID(0),
        m_pMediaInstance(NULL),
        m_outstandingUIFrame(false)
{
}

CompositionNotifier::
~CompositionNotifier(
    void
    )
{
    //
    // Not ref-counted
    //
    m_pMediaInstance = NULL;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CompositionNotifier::Init
//
//  Synopsis:
//      Initialize the CompositionNotifier
//
//------------------------------------------------------------------------------
HRESULT
CompositionNotifier::
Init(
    __in    MediaInstance   *pMediaInstance
    )
{
    HRESULT hr = S_OK;
    m_uiID = pMediaInstance->GetID();
    TRACEF(&hr);

    //
    // Not ref-counted
    //
    m_pMediaInstance = pMediaInstance;

    IFC(m_lock.Init());

Cleanup:
    RRETURN(hr);
}

HRESULT
CompositionNotifier::
RegisterResource(
    __in    CMilSlaveVideo  *pCMilSlaveVideo
    )
{
    HRESULT                     hr = S_OK;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "RegisterResource(%p)",
        pCMilSlaveVideo);

    CGuard<CCriticalSection>    guard(m_lock);

    IFC(m_registeredResources.AddHead(pCMilSlaveVideo));

    hr = S_OK; // ignore S_FALSE, which means already added

Cleanup:
    RRETURN(hr);
}

void
CompositionNotifier::
UnregisterResource(
    __in    CMilSlaveVideo  *pCMilSlaveVideo
    )
{
    TRACEF(NULL);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_PRESENTER,
        "UnregisterResource(%p)",
        pCMilSlaveVideo);

    CGuard<CCriticalSection>    guard(m_lock);

    m_registeredResources.Remove(pCMilSlaveVideo);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CompositionNotifier::NotifyComposition
//
//  Synopsis:
//      Request a new composition pass. If we don't have a video resource
//      currently, we'll notify composition as soon as we get one.
//
//------------------------------------------------------------------------------
void
CompositionNotifier::
NotifyComposition(
    void
    )
{
    TRACEF(NULL);

    UniqueList<CMilSlaveVideo*>::Node   *pCurrent = NULL;
    UniqueList<CMilSlaveVideo*>::Node   *pNext = NULL;
    bool                                displayUIFrame = false;

    {
        CGuard<CCriticalSection>    guard(m_lock);

        pCurrent = m_registeredResources.GetHead();

        while (pCurrent != NULL)
        {
            pNext = pCurrent->GetNext();

            LogAVDataM(
                AVTRACE_LEVEL_INFO,
                AVCOMP_PRESENTER,
                "Notifying resource: %p",
                pCurrent);

            displayUIFrame = !pCurrent->instance->NewFrame() || displayUIFrame;

            pCurrent = pNext;
        }

        if (m_outstandingUIFrame)
        {
            m_outstandingUIFrame = false;
            displayUIFrame = true;
        }
    }


    if (displayUIFrame)
    {
        m_pMediaInstance->GetMediaEventProxy().RaiseEvent(AVMediaNewFrame);
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CompositionNotifier::InvalidateLastCompositionSampleTime
//
//  Synopsis:
//
//------------------------------------------------------------------------------
void
CompositionNotifier::
InvalidateLastCompositionSampleTime(
    void
    )
{
    TRACEF(NULL);

    UniqueList<CMilSlaveVideo*>::Node   *pCurrent = NULL;
    UniqueList<CMilSlaveVideo*>::Node   *pNext = NULL;

    {
        CGuard<CCriticalSection>    guard(m_lock);

        pCurrent = m_registeredResources.GetHead();

        while (pCurrent != NULL)
        {
            pNext = pCurrent->GetNext();

            pCurrent->instance->InvalidateLastCompositionSampleTime();

            pCurrent = pNext;
        }
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CompositionNotifier::NeedUIFrameUpdate
//
//  Synopsis:
//      Requests an update for the UI frame.
//
//------------------------------------------------------------------------------
void
CompositionNotifier::
NeedUIFrameUpdate(
    void
    )
{
    CGuard<CCriticalSection>    guard(m_lock);

    m_outstandingUIFrame = true;
}


