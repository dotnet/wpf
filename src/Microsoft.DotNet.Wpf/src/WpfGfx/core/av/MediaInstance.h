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

MtExtern(MediaInstance);

class MediaInstance : public CMILCOMBase
{
public:

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(MediaInstance));

    //
    // CMILCOMBase
    //
    DECLARE_COM_BASE;

    static
    HRESULT
    Create(
        __in            CEventProxy         *pCEventProxy,
        __deref_out     MediaInstance       **ppMediaInstance
        );

    HRESULT
    Init(
        void
        );

    UINT
    GetID(
        void
        ) const;

    inline
    CMediaEventProxy&
    GetMediaEventProxy(
        void
        );

    inline
    CompositionNotifier&
    GetCompositionNotifier(
        void
        );

protected:

    //
    // CMILCOMBase
    //
    STDMETHOD(HrFindInterface)(__in_ecount(1) REFIID riid, __deref_out void **ppv);

private:

    MediaInstance(
        __in        UINT        uiID,
        __in        CEventProxy *pCEventProxy
        );

    ~MediaInstance(
        void
        );

    //
    // Cannot copy or assign a MediaInstance
    //
    MediaInstance(
        __in    const MediaInstance &
        );

    MediaInstance &
    operator=(
        __in    const MediaInstance &
        );

    UINT                    m_uiID;
    CompositionNotifier     m_compositionNotifier;
    CMediaEventProxy        m_mediaEventProxy;

    static LONG             ms_id;
};

#include "MediaInstance.inl"

