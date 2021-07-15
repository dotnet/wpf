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

class MediaInstance;

class CompositionNotifier
{
public:

    CompositionNotifier(
        void
        );

    ~CompositionNotifier(
        void
        );

    HRESULT
    Init(
        __in    MediaInstance   *pMediaInstance
        );

    HRESULT
    RegisterResource(
        __in    CMilSlaveVideo  *pCMilSlaveVideo
        );

    void
    UnregisterResource(
        __in    CMilSlaveVideo  *pCMilSlaveVideo
        );

    void
    NotifyComposition(
        void
        );

    void
    InvalidateLastCompositionSampleTime(
        void
        );

    void
    NeedUIFrameUpdate(
        void
        );

private:
    //
    // Cannot copy or assign a CompositionNotifier
    //
    CompositionNotifier(
        __in const CompositionNotifier        &
        );

    CompositionNotifier &
    operator=(
        __in const CompositionNotifier &
        );

    UINT                m_uiID;
    MediaInstance       *m_pMediaInstance;

    //
    // CompositionNotifier is generally accessed by the media
    // thread and sometimes by the composition thread.
    //
    CCriticalSection    m_lock;
    bool                m_outstandingUIFrame;

    UniqueList<CMilSlaveVideo*>    m_registeredResources;
};

