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
//      Provides support media specific support for proxying events up to
//      managed code.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#pragma once

MtExtern(EventItem);

class CMediaEventProxy
{
public:

    CMediaEventProxy(
        __in        UINT                    uiID,
        __in_opt        CEventProxy             *pCEventProxy
        );

    virtual
    ~CMediaEventProxy(
        );

    HRESULT
    Init(
        void
        );

    HRESULT
    RaiseEvent(
        __in AVEvent    eventType,
        __in HRESULT    failureHr = S_OK
        );

    HRESULT
    RaiseEvent(
        __in     AVEvent    eventType,
        __in_opt PCWSTR     type,
        __in_opt PCWSTR     param,
        __in     HRESULT    failureHr = S_OK
        );

    void
    Shutdown(
        void
        );

private:

    class EventItem : public CStateThreadItem,
                             CMILCOMBase
    {
    public:
        DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(EventItem));

        // Declares IUnknown functions
        DECLARE_COM_BASE;

        static
        HRESULT
        Create(
            __in        UINT                    id,
            __in        CEventProxy             *pCEventProxy,
            __in        AVEvent                 eventType,
            __in_opt    PCWSTR                  type,
            __in_opt    PCWSTR                  param,
            __in        HRESULT                 failureHr,
            __deref_out EventItem               **ppEventItem
            );

    protected:
        //
        // CMILCOMBase
        //
        STDMETHOD(HrFindInterface)(
            __in_ecount(1) REFIID riid,
            __deref_out void **ppv
            );

        //
        // CStateThreadItem
        //
        __override
        void
        Run(
            void
            );

    private:
        EventItem(
            __in        UINT                    uiID,
            __in        CEventProxy             *pCEventProxy,
            __in        AVEvent                 eventType,
            __in        HRESULT                 failureHr
            );

        virtual
        ~EventItem(
            );

        HRESULT
        Init(
            __in_opt    PCWSTR                  type,
            __in_opt    PCWSTR                  param
            );

        //
        // Cannot copy or assign a CMediaEventProxy
        // 
        EventItem(
            __in const EventItem &
            );

        EventItem &
        operator=(
            __in const EventItem &
            );

        UINT                    m_uiID;
        CEventProxy             *m_pCEventProxy;
        AVEvent                 m_eventType;
        PWSTR                   m_type;
        PWSTR                   m_param;
        HRESULT                 m_failureHr;
    };

    enum
    {
        maximumEventPacketSize = 4096
    };

    //
    // Cannot copy or assign a CMediaEventProxy
    // 
    CMediaEventProxy(
        __in const CMediaEventProxy &
        );

    CMediaEventProxy &
    operator=(
        __in const CMediaEventProxy &
        );

    UINT                    m_uiID;
    CEventProxy             *m_pCEventProxy;
    CStateThread            *m_pEventThread;
    CCriticalSection        m_stateLock;

    typedef CGuard<CCriticalSection>    CriticalSectionGuard_t;
};

