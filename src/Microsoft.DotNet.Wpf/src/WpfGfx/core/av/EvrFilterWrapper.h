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

MtExtern(CEvrFilterWrapper);

class CEvrFilterWrapper : IMediaSeeking
{
public:

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CEvrFilterWrapper));

    static
    HRESULT
    Create(
        __in        UINT                id,
        __deref_out CEvrFilterWrapper   **ppCEvrFilterWrapper
        );

    void
    SwitchToInnerIMediaSeeking(
        void
        );

    //
    // IUnknown
    //
    STDMETHOD_(ULONG, AddRef)(void);
    STDMETHOD_(ULONG,Release)(void);
    STDMETHOD(QueryInterface)(__in REFIID riid, __deref_out void **ppvObject);

    //
    // IMediaSeeking
    //
    STDMETHOD(GetCapabilities)(
        __out       DWORD       *pCapabilities
        );

    STDMETHOD(CheckCapabilities)(
        __in        DWORD       *pCapabilities
        );

    STDMETHOD(SetTimeFormat)(
        __in        const GUID  *pFormat
        );

    STDMETHOD(GetTimeFormat)(
        __out       GUID        *pFormat
        );

    STDMETHOD(IsUsingTimeFormat)(
        __in        const GUID  *pFormat
        );

    STDMETHOD(IsFormatSupported)(
        __in        const GUID  *pFormat
        );

    STDMETHOD(QueryPreferredFormat)(
        __out       GUID        *pFormat
        );

    STDMETHOD(ConvertTimeFormat)(
        __out       LONGLONG    *pTarget,
        __in_opt    const GUID  *pTargetFormat,
        __in        LONGLONG    Source,
        __in_opt    const GUID  *pSourceFormat
        );

    STDMETHOD(SetPositions)(
        __in_opt    LONGLONG    *pCurrent,
        __in        DWORD       CurrentFlags,
        __in_opt    LONGLONG    *pStop,
        __in        DWORD       StopFlags
        );

    STDMETHOD(GetPositions)(
        __out_opt   LONGLONG    *pCurrent,
        __out_opt   LONGLONG    *pStop
        );

    STDMETHOD(GetCurrentPosition)(
        __out       LONGLONG    *pCurrent
        );

    STDMETHOD(GetStopPosition)(
        __out       LONGLONG    *pStop
        );

    STDMETHOD(SetRate)(
        __in        double      dRate
        );

    STDMETHOD(GetRate)(
        __out       double      *pdRate
        );

    STDMETHOD(GetDuration)(
        __out       LONGLONG    *pDuration
        );

    STDMETHOD(GetAvailable)(
        __out_opt   LONGLONG    *pEarliest,
        __out_opt   LONGLONG    *pLatest
        );

    STDMETHOD(GetPreroll)(
        __out       LONGLONG    *pllPreroll
        );

private:

    CEvrFilterWrapper(
        __in        UINT                    uiID
        );

    virtual
    ~CEvrFilterWrapper(
        );

    HRESULT
    Init(
        );

    CCriticalSection        m_stateLock;
    UINT                    m_uiID;
    LONG                    m_cRef;
    IUnknown                *m_pINonDelegatingUnknown;
    IMediaSeeking           *m_pIMediaSeeking;
    bool                    m_useInnerIMediaSeeking;

    typedef CGuard<CCriticalSection>    CriticalSectionGuard_t;
};

