// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "precomp.hpp"
#include "SharedState.tmh"

SharedState::
SharedState(
    void
    ) : m_length(0LL),
        m_width(0),
        m_height(0),
        m_isBuffering(false),
        m_canPause(false),
        m_hasAudio(false),
        m_hasVideo(false),
        m_downloadProgress(0.0),
        m_bufferingProgress(0.0),
        m_position(0LL)
{
}

HRESULT
SharedState::
Init(
    void
    )
{
    RRETURN(m_lock.Init());
}

UINT
SharedState::
GetNaturalWidth(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_width;
}

void
SharedState::
SetNaturalWidth(
    __in    UINT        width
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_width = width;
}

UINT
SharedState::
GetNaturalHeight(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_height;
}

void
SharedState::
SetNaturalHeight(
    __in    UINT        height
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_height = height;
}

bool
SharedState::
GetIsBuffering(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_isBuffering;
}

void
SharedState::
SetIsBuffering(
    __in    bool        isBuffering
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_isBuffering = isBuffering;
}

bool
SharedState::
GetCanPause(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_canPause;
}

void
SharedState::
SetCanPause(
    __in    bool        canPause
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_canPause = canPause;
}

bool
SharedState::
GetHasVideo(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_hasVideo;
}

void
SharedState::
SetHasVideo(
    __in    bool        hasVideo
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_hasVideo = hasVideo;
}

bool
SharedState::
GetHasAudio(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_hasAudio;
}

void
SharedState::
SetHasAudio(
    __in    bool        hasAudio
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_hasAudio = hasAudio;
}

LONGLONG
SharedState::
GetLength(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_length;
}

void
SharedState::
SetLength(
    __in    LONGLONG    length
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_length = length;
}

double
SharedState::
GetDownloadProgress(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_downloadProgress;
}

void
SharedState::
SetDownloadProgress(
    __in    double      downloadProgress
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_downloadProgress = downloadProgress;

    m_timedOutDownloadProgress.m_isValid = false;
}

double
SharedState::
GetBufferingProgress(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_bufferingProgress;
}

void
SharedState::
SetBufferingProgress(
    __in    double      bufferingProgress
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_bufferingProgress = bufferingProgress;

    m_timedOutBufferingProgress.m_isValid = false;
}

LONGLONG
SharedState::
GetPosition(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    return m_position;
}

void
SharedState::
SetPosition(
    __in    LONGLONG    position
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_position = position;

    m_timedOutPosition.m_isValid = false;
}

LONGLONG
SharedState::
GetTimedOutPosition(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    if (m_timedOutPosition.m_isValid)
    {
        return m_timedOutPosition.m_value;
    }
    else
    {
        return m_position;
    }
}

void
SharedState::
SetTimedOutPosition(
    __in    LONGLONG    position
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_timedOutPosition = position;
}

double
SharedState::
GetTimedOutDownloadProgress(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    if (m_timedOutDownloadProgress.m_isValid)
    {
        return m_timedOutDownloadProgress.m_value;
    }
    else
    {
        return m_downloadProgress;
    }
}

void
SharedState::
SetTimedOutDownloadProgress(
    __in    double      downloadProgress
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_timedOutDownloadProgress = downloadProgress;
}

double
SharedState::
GetTimedOutBufferingProgress(
    void
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    if (m_timedOutBufferingProgress.m_isValid)
    {
        return m_timedOutBufferingProgress.m_value;
    }
    else
    {
        return m_bufferingProgress;
    }
}

void
SharedState::
SetTimedOutBufferingProgress(
    __in    double      bufferingProgress
    )
{
    CGuard<CCriticalSection> guard(m_lock);

    m_timedOutBufferingProgress = bufferingProgress;
}

