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
//      Provide support for having a single reference counted appartment that
//      runs the various WMP Ocx threads.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#pragma once

MtExtern(CStateThreadItem);
MtExtern(CStateThread);

class CStateThread;

class CStateThreadItem : public IUnknown,
                         public ListNodeT<CStateThreadItem>
{
public:

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CStateThreadItem));

    friend CStateThread;

protected:

    CStateThreadItem(
        void
        );

    ~CStateThreadItem(
        void
        );

    HRESULT
    Init(
        void
        );

    virtual
    void
    Cancel(
        void
        );

    virtual
    bool
    IsAnOwner(
        __in    IUnknown    *pIUnknown
        );

    virtual
    void
    Run(
        void
        ) = 0;

private:

    //
    // Cannot copy or assign a CStateThreadItem
    //
    CStateThreadItem(
        __in    const CStateThreadItem &
        );

    CStateThreadItem &
    operator=(
        __in    const CStateThreadItem &
        );

    bool                    m_isQueued;
};

class CStateThread : public IUnknown
{
public:

    static
    HRESULT
    Initialize(
        void
        );

    static
    void
    FinalShutdown(
        void
        );

    static
    HRESULT
    CreateApartmentThread(
        __deref_out     CStateThread       **ppStateThread
        );

    static
    HRESULT
    CreateEventThread(
        __deref_out     CStateThread       **ppStateThread
        );

    DECLARE_METERHEAP_CLEAR(ProcessHeap, Mt(CStateThread));

    //
    // Custom IUknown implementation
    //
    STDMETHOD(QueryInterface)(
        __in        REFIID      riid,
        __deref_out void        **ppv
        );

    STDMETHOD_(ULONG, AddRef)();

    STDMETHOD_(ULONG, Release)();

    HRESULT
    AddItem(
        __in    CStateThreadItem           *pStateThreadItem
        );

    bool
    ReleaseItem(
        __in    CStateThreadItem           *pStateThreadItem
        );

    DWORD
    GetThreadId(
        void
        ) const;

    void
    CancelAllItemsWithOwner(
        __in    IUnknown    *pIOwner
        );

private:

    static
    HRESULT
    CreateStateThread(
        __in            CCriticalSection    *pCritSec,
        __deref_inout   CStateThread        **ppGlobalThread,
        __deref_out     CStateThread        **ppStateThread
        );

    CStateThread(
        __in    CCriticalSection    *pLock,
        __in    CStateThread        **ppGlobalReference

        );

    ~CStateThread(
        void
        );

    HRESULT
    Init(
        void
        );

    static
    DWORD __stdcall
    ThreadThunk(
        __in    void        *pThat
        );

    HRESULT
    Thread(
        void
        );

    void
    ThreadLoop(
        void
        );

    //
    // Cannot copy or assign an Apartment Thread
    //
    CStateThread(
        __in const CStateThread &
        );

    CStateThread &
    operator=(
        __in const CStateThread &
        );

    HRESULT
    WaitForInitialization(
        void
        );

    LONG                        m_cRef;
    HANDLE                      m_thread;
    HANDLE                      m_signalEvent;
    HANDLE                      m_initializedEvent;
    HRESULT                     m_initHR;
    List<CStateThreadItem>      m_items;
    bool                        m_processingItems;
    DWORD                       m_threadId;
    CCriticalSection            *m_pLock;
    CStateThread                **m_ppGlobalReference;


    typedef CGuard<CCriticalSection>    CCriticalSectionGuard_t;
    typedef CUnGuard<CCriticalSection>  CCriticalSectionUnGuard_t;

    static CCriticalSection      ms_apartmentLock;
    static CStateThread          *ms_pApartmentThread;

    static CCriticalSection      ms_eventLock;
    static CStateThread          *ms_pEventThread;
};

