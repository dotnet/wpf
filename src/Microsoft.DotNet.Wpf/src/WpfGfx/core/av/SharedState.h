// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#pragma once

//
// SharedState keeps track of the state that must be shared across the apartment
// thread and the UI thread. This object is aggregated by CWmpPlayer
//
class SharedState
{
public:

    SharedState(
        void
        );

    HRESULT
    Init(
        void
        );

    UINT
    GetNaturalWidth(
        void
        );

    void
    SetNaturalWidth(
        __in    UINT        width
        );

    UINT
    GetNaturalHeight(
        void
        );

    void
    SetNaturalHeight(
        __in    UINT        height
        );

    bool
    GetIsBuffering(
        void
        );

    void
    SetIsBuffering(
        __in    bool        isBuffering
        );

    bool
    GetCanPause(
        void
        );

    void
    SetCanPause(
        __in    bool        canPause
        );

    bool
    GetHasVideo(
        void
        );

    void
    SetHasVideo(
        __in    bool        hasVideo
        );

    bool
    GetHasAudio(
        void
        );

    void
    SetHasAudio(
        __in    bool        hasAudio
        );

    LONGLONG
    GetLength(
        void
        );

    void
    SetLength(
        __in    LONGLONG    length
        );

    double
    GetDownloadProgress(
        void
        );

    void
    SetDownloadProgress(
        __in    double      downloadProgress
        );

    double
    GetBufferingProgress(
        void
        );

    void
    SetBufferingProgress(
        __in    double      bufferingProgress
        );

    LONGLONG
    GetPosition(
        void
        );

    void
    SetPosition(
        __in    LONGLONG    position
        );

    LONGLONG
    GetTimedOutPosition(
        void
        );

    void
    SetTimedOutPosition(
        __in    LONGLONG    position
        );

    double
    GetTimedOutDownloadProgress(
        void
        );

    void
    SetTimedOutDownloadProgress(
        __in    double      downloadProgress
        );

    double
    GetTimedOutBufferingProgress(
        void
        );

    void
    SetTimedOutBufferingProgress(
        __in    double      bufferingProgress
        );

private:
    CCriticalSection    m_lock;
    UINT                m_uiID;

    LONGLONG            m_length;

    UINT                m_width;
    UINT                m_height;

    bool                m_isBuffering;
    bool                m_canPause;
    bool                m_hasVideo;
    bool                m_hasAudio;

    double              m_downloadProgress;
    double              m_bufferingProgress;
    LONGLONG            m_position;

    Optional<LONGLONG>  m_timedOutPosition;
    Optional<double>    m_timedOutDownloadProgress;
    Optional<double>    m_timedOutBufferingProgress;
};


