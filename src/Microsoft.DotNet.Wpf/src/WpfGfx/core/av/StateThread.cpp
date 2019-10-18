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
#include "precomp.hpp"
#include "StateThread.tmh"

MtDefine(CStateThread, Mem, "CStateThread");
MtDefine(CStateThreadItem, Mem, "CStateThreadItem");

/*static*/ CCriticalSection         CStateThread::ms_apartmentLock;
/*static*/ CStateThread             *CStateThread::ms_pApartmentThread = NULL;

/*static*/ CCriticalSection         CStateThread::ms_eventLock;
/*static*/ CStateThread             *CStateThread::ms_pEventThread = NULL;

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// CStateThreadItem implementation
//
CStateThreadItem::
CStateThreadItem(
    void
    ) : m_isQueued(false)
{
}

CStateThreadItem::
~CStateThreadItem(
    void
    )
{
}

HRESULT
CStateThreadItem::
Init(
    void
    )
{
    return S_OK;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThreadItem::Cancel
//
//  Synopsis:
//      Called if the item cannot be run. Empty by default. Not necessarily
//      called by the state thread. Subclasses may override this.
//
//------------------------------------------------------------------------------
/*virtual*/
void
CStateThreadItem::
Cancel(
    void
    )
{}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThreadItem::IsAnOwner
//
//  Synopsis:
//      Called by the state thread to determine whether or not to Cancel an
//      item. Subclasses should override this if they may need to be canceled.
//
//------------------------------------------------------------------------------
/*virtual*/
bool
CStateThreadItem::
IsAnOwner(
    __in    IUnknown    *pIUnknown
    )
{
    return false;
}

//+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
// CStateThread implementation
//
/*static*/
HRESULT
CStateThread::
Initialize(
    void
    )
{
    HRESULT hr = S_OK;

    IFC(ms_apartmentLock.Init());
    IFC(ms_eventLock.Init());

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::FinalShutdown
//
//  Synopsis:
//      Releases the global reference to the state thread. The state thread
//      should exit and be destroyed at this point. This function should only be
//      called upon DLL Process Detach.
//
//------------------------------------------------------------------------------
/*static*/
void
CStateThread::
FinalShutdown(
    void
    )
{
    CStateThread    *pApartmentThread = NULL;
    CStateThread    *pEventThread = NULL;

    if (ms_apartmentLock.IsValid())
    {
        CCriticalSectionGuard_t     guard(ms_apartmentLock);

        pApartmentThread = ms_pApartmentThread;
        ms_pApartmentThread = NULL;
    }

    if (ms_eventLock.IsValid())
    {
        CCriticalSectionGuard_t     guard(ms_eventLock);

        pEventThread = ms_pEventThread;
        ms_pEventThread = NULL;
    }

    ReleaseInterface(pApartmentThread);
    ReleaseInterface(pEventThread);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::CreateApartmentThread
//
//  Synopsis:
//      Creates a new appartment thread, of, if lifetime overlaps with an
//      existing appartment, retrieve the existing one.
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CStateThread::
CreateApartmentThread(
    __deref_out     CStateThread       **ppStateThread
        // Thre returned apparment thread
    )
{
    HRESULT         hr = S_OK;
    TRACEFID(0, &hr);

    IFC(CreateStateThread(&ms_apartmentLock, &ms_pApartmentThread, ppStateThread));

Cleanup:
    EXPECT_SUCCESSID(0, hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::CreateEventThread
//
//  Synopsis:
//      Creates a new appartment thread, of, if lifetime overlaps with an
//      existing appartment, retrieve the existing one.
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CStateThread::
CreateEventThread(
    __deref_out     CStateThread       **ppStateThread
        // Thre returned apparment thread
    )
{
    HRESULT         hr = S_OK;
    TRACEFID(0, &hr);

    IFC(CreateStateThread(&ms_eventLock, &ms_pEventThread, ppStateThread));

Cleanup:
    EXPECT_SUCCESSID(0, hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::CreateStateThread
//
//  Synopsis:
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CStateThread::
CreateStateThread(
    __in            CCriticalSection    *pCritSec,
    __deref_inout   CStateThread        **ppGlobalThread,
    __deref_out     CStateThread        **ppStateThread
        // The returned apartment thread
    )
{
    HRESULT         hr = S_OK;
    CStateThread    *pThread = NULL;

    TRACEFID(0, &hr);

    {
        CCriticalSectionGuard_t     guard(*pCritSec);

        if (*ppGlobalThread != NULL)
        {
            SetInterface(pThread, *ppGlobalThread);
        }
        else
        {
            pThread = new CStateThread(&ms_eventLock, &ms_pEventThread);
            IFCOOM(pThread);

            IFC(pThread->Init());

            SetInterface(*ppGlobalThread, pThread);
        }
    }

    *ppStateThread = pThread;
    pThread = NULL;

Cleanup:
    ReleaseInterface(pThread);

    EXPECT_SUCCESSID(0, hr);
    RRETURN(hr);
}

//
// IUnknown methods
//
//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::QueryInterface,      IUnknown
//
//  Synopsis:
//      This is a concrete class, it only supports IUnknown for reference counting
//      so QI can only return IUnknown
//
//------------------------------------------------------------------------------
STDMETHODIMP
CStateThread::
QueryInterface(
    __in        REFIID      riid,
    __deref_out void        **ppv
    )
{
    HRESULT     hr = S_OK;

    *ppv = NULL;

    if (riid == __uuidof(IUnknown))
    {
        *ppv = static_cast<IUnknown *>(this);
    }

    if (*ppv)
    {
        AddRef();
    }
    else
    {
        hr = E_NOINTERFACE;
    }

    return hr;
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::AddRef,      IUnknown
//
//  Synopsis:
//      Performs an AddRef, this call must grab the lock to keep its reference
//      count consistent as a global count on the global appartment thread
//      object
//
//------------------------------------------------------------------------------
STDMETHODIMP_(ULONG)
CStateThread::
AddRef(
    )
{
    ULONG   cRef = 0;

    {
        CCriticalSectionGuard_t guard(*m_pLock);

        cRef = m_cRef++;
    }

    return cRef;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::Release,      IUnknown
//
//  Synopsis:
//      Performs an AddRef, this call must grab the lock to keep its reference
//      count consistent as a global count on the global appartment thread
//      object
//
//------------------------------------------------------------------------------
STDMETHODIMP_(ULONG)
CStateThread::
Release(
    )
{
    ULONG   cRef = 0;
    CStateThread       *pDeleteStateThread = NULL;

    {
        CCriticalSectionGuard_t guard(*m_pLock);

        cRef = --m_cRef;

        if (!cRef)
        {
            pDeleteStateThread = *m_ppGlobalReference;
            *m_ppGlobalReference = NULL;
        }
    }

    if (pDeleteStateThread)
    {
        if (pDeleteStateThread->m_signalEvent)
        {
            CCriticalSectionGuard_t guard(*m_pLock);

            if (!SetEvent(pDeleteStateThread->m_signalEvent))
            {
                RIP("The only way an event can fail to be signalled is if the handle has become invalid through a Close");
            }
        }

        //
        // This will also wait on the thread.
        //
        delete pDeleteStateThread;
    }

    return cRef;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::AddItem
//
//  Synopsis:
//      Adds a new appartment thread item. Not the same item can be added
//      multiple times and it can add itself in its own callback. The call
//      guarantees that the item will be called back (once) by locking over the
//      m_isQueued member on the appartment thrad item.
//
//------------------------------------------------------------------------------
HRESULT
CStateThread::
AddItem(
    __in    CStateThreadItem           *pStateThreadItem
        // The appartment thread item to add
    )
{
    HRESULT hr = S_OK;

    TRACEFID(0, &hr);

    IFC(WaitForInitialization());

    //
    // This is mainly to handle the fact the CoInitialize might fail.
    //
    {
        CCriticalSectionGuard_t     guard(*m_pLock);

        if (!pStateThreadItem->m_isQueued)
        {
            m_items.AddTail(pStateThreadItem);

            pStateThreadItem->m_isQueued = true;

            pStateThreadItem->AddRef();

            if (!m_processingItems)
            {
                if (!SetEvent(m_signalEvent))
                {
                    RIP("Always must be able to set an event once instantiated");
                }
            }
        }
    }

Cleanup:

    EXPECT_SUCCESSID(0, hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::ReleaseItem
//
//  Synopsis:
//      Removes the given appartment thread item from this list. It is benign if
//      the item isn't in the list (the caller can check the return if they are
//      interested)
//
//  Returns:
//      true if the item was in fact in the list.
//
//------------------------------------------------------------------------------
bool
CStateThread::
ReleaseItem(
    __in    CStateThreadItem           *pStateThreadItem
    )
{
    TRACEFID(0, NULL);
    bool    found = false;

    {
        CCriticalSectionGuard_t   guard(*m_pLock);

        if (pStateThreadItem->m_isQueued)
        {
            m_items.Unlink(pStateThreadItem);

            found = true;
        }
    }

    if (found)
    {
        pStateThreadItem->Release();
    }

    return found;
}

DWORD
CStateThread::
GetThreadId(
    void
    ) const
{
    return m_threadId;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::CancelAllItemsWithOwner
//
//  Synopsis:
//      Removes all items from the queue and calls cancel on each one. Note that
//      this does not prevent future items from being added! The caller is
//      responsible for ensuring future items are not added. This should be done
//      before this function is called.
//
//------------------------------------------------------------------------------
void
CStateThread::
CancelAllItemsWithOwner(
    __in    IUnknown    *pIOwner
    )
{
    TRACEFID(0, NULL);
    List<CStateThreadItem>      toCancel;
    CStateThreadItem           *pCurrent = NULL;
    CStateThreadItem           *pNext = NULL;

    //
    // Build a list of items to cancel as we remove them from the
    // item list
    //
    {
        CCriticalSectionGuard_t   guard(*m_pLock);

        pCurrent = m_items.GetHead();

        while (pCurrent != NULL)
        {
            pNext = pCurrent->GetNext();

            if (pCurrent->IsAnOwner(pIOwner))
            {
                m_items.Unlink(pCurrent);

                toCancel.AddTail(pCurrent);
            }

            pCurrent = pNext;
        }
    }

    //
    // Iterate through the toCancel list and cancel each one.
    // We do this outside of a lock to avoid potential deadlocks
    //

    pCurrent = toCancel.GetHead();

    while (pCurrent != NULL)
    {
        toCancel.Unlink(pCurrent);

        pCurrent->Cancel();

        ReleaseInterface(pCurrent);

        pCurrent = toCancel.GetHead();
    }
}

//
// Private methods
//
CStateThread::
CStateThread(
    __in    CCriticalSection    *pLock,
    __in    CStateThread        **ppGlobalReference
    ) : m_cRef(1),
        m_thread(NULL),
        m_signalEvent(NULL),
        m_initializedEvent(NULL),
        m_initHR(S_OK),
        m_processingItems(false),
        m_threadId(0),
        m_pLock(pLock),
        m_ppGlobalReference(ppGlobalReference)
{
    TRACEFID(0, NULL);
}

CStateThread::
~CStateThread(
    void
    )
{
    Assert(m_items.IsEmpty());

    TRACEFID(0, NULL);

    Assert(GetCurrentThreadId() != m_threadId);

    //
    // This is what guarantees synchronizatoin when the last appartment thread is released. We
    // will wait for its corresponding thread to terminate.
    //
    if (m_thread)
    {
        if (WaitForSingleObject(m_thread, INFINITE) != WAIT_OBJECT_0)
        {
            RIP("Can always wait for a thread handle unless it has become invalid");
        }

        CloseHandle(m_thread);
    }

    if (m_signalEvent)
    {
        CloseHandle(m_signalEvent);
    }

    if (m_initializedEvent)
    {
        CloseHandle(m_initializedEvent);
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::Init
//
//  Synopsis:
//      Performs initialization for the appartment thread that can fail. We need
//      an event to signal that we are initialized, another for us to be
//      signalled to process our work and another for the thread itself
//
//------------------------------------------------------------------------------
HRESULT
CStateThread::
Init(
    void
    )
{
    HRESULT hr = S_OK;

    TRACEFID(0, &hr);

    m_signalEvent
        = CreateEvent(
                NULL,                   // Security Attributes
                TRUE,                   // Manual Reset
                FALSE,                  // Initial State is not signalled
                NULL);                  // Name

    if (NULL == m_signalEvent)
    {
        hr = GetLastErrorAsFailHR();

        goto Cleanup;
    }

    m_initializedEvent
        = CreateEvent(
                NULL,                   // Security Attributes
                TRUE,                   // Manual Reset
                FALSE,                  // Initial State is not signalled
                NULL);                  // Name

    if (NULL == m_initializedEvent)
    {
        hr = GetLastErrorAsFailHR();

        goto Cleanup;
    }

    DWORD   threadId = 0;

    m_thread
        = CreateThread(
                NULL,                   // Security Attributes
                0,                      // Use default Stack size
                CStateThread::ThreadThunk,
                this,
                0,                      // Default creation flags
                &threadId);

    if (NULL == m_thread)
    {
        hr = GetLastErrorAsFailHR();

        goto Cleanup;
    }

Cleanup:

    EXPECT_SUCCESSID(0, hr);

    RRETURN(hr);
}

/*static*/
DWORD __stdcall
CStateThread::
ThreadThunk(
    __in    void        *pThat
    )
{
    return
        Win32StatusFromHR(
            reinterpret_cast<CStateThread *>(pThat)->Thread());
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::Thread
//
//  Synopsis:
//      Run the state thread loop. This also performs any initialization that
//      the appartment thread might need. If the thread fails to start, then an
//      AddItem call on the appartment thread will fail and return an
//      appropriate error code.
//
//------------------------------------------------------------------------------
HRESULT
CStateThread::
Thread(
    void
    )
{
    HRESULT     hr = S_OK;
    bool        initialized = false;

    TRACEFID(0, &hr);

    IFC(CoInitialize(NULL));
    initialized = true;

    m_threadId = GetCurrentThreadId();

    if (!SetEvent(m_initializedEvent))
    {
        RIP("Once an event is created it can always be set unless closed");
    }

    ThreadLoop();

Cleanup:

    if (initialized)
    {
        CoUninitialize();
    }

    m_initHR = hr;

    //
    // In the case of failure, we still want to signal the calling thread
    // and return what the failure was.
    //
    if (!SetEvent(m_initializedEvent))
    {
        RIP("Once an event is created it can always be set unless closed");
    }


    EXPECT_SUCCESSID(0, hr);

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::ThreadLoop
//
//  Synopsis:
//      This sits in a call to MsgWaitForMultipleObjects. If its event is signalled
//      it will process any items that have been queued up against it. COM will
//      dispatch any calls to the appartment through the message loop.
//
//------------------------------------------------------------------------------
void
CStateThread::
ThreadLoop(
    void
    )
{
    HRESULT hr = S_OK;
    HANDLE  ahEvents[] = { m_signalEvent };
    bool    leave = false;

    TRACEFID(0, &hr);

    do
    {
        {
            //
            // We could have come back in around again after processing items
            // to a reference count that has returned to 0.
            //
            CCriticalSectionGuard_t guard(*m_pLock);

            if (m_cRef == 0)
            {
                leave = true;

                Assert(m_items.IsEmpty());
            }
        }

        if (!leave)
        {
            DWORD result = WAIT_ABANDONED;
            MSG   msg    = { 0 };

            //
            // We use PeekMessage because we are using MsgWaitForMultipleObjects
            // to check both if we are signalled by a state transition and
            // if there are normal window messages (required by WMP).
            //
            while(PeekMessage(&msg, NULL, 0, 0, PM_REMOVE))
            {
                if (msg.message == WM_QUIT)
                {
                    IFC(E_ABORT);
                }

                LogAVDataX(
                    AVTRACE_LEVEL_INFO,
                    AVCOMP_STATEENGINE,
                    "Translating and dispatching" #
                    " [,%p]",
                    this);

                TranslateMessage(&msg);
                DispatchMessage(&msg);
            }

            result
                = MsgWaitForMultipleObjects(
                        ARRAY_SIZE(ahEvents),
                        ahEvents,
                        FALSE,                  // Shouldn't wait for all
                        INFINITE,
                        QS_ALLINPUT);

            if (result == (WAIT_OBJECT_0 + ARRAY_SIZE(ahEvents)))
            {
                //
                // New messages have arrived.
                // Continue to the top of the always while loop to
                // dispatch them and resume waiting.
                //
                continue;
            }
            //
            // We might have been signalled, or we might have reached the time for the next action.
            //
            else if (result == WAIT_OBJECT_0 || result == WAIT_TIMEOUT)
            {
                CCriticalSectionGuard_t guard(*m_pLock);

                m_processingItems = true;

                //
                // We get signalled and terminate when our reference count hits 0
                //
                while(!m_items.IsEmpty())
                {
                    CStateThreadItem    *pItem = m_items.UnlinkHead();

                    pItem->m_isQueued = false;

                    {
                        CCriticalSectionUnGuard_t  unGuard(*m_pLock);

                        LogAVDataX(
                            AVTRACE_LEVEL_INFO,
                            AVCOMP_STATEENGINE,
                            "Running an item" #
                            " [,%p]",
                            this);

                        pItem->Run();

                        LogAVDataX(
                            AVTRACE_LEVEL_INFO,
                            AVCOMP_STATEENGINE,
                            "Item run" #
                            " [,%p]",
                            this);

                        pItem->Release();
                    }
                }

                LogAVDataX(
                    AVTRACE_LEVEL_INFO,
                    AVCOMP_STATEENGINE,
                    "Done running items - queue exhausted" #
                    " [,%p]",
                    this);

                m_processingItems = false;

                if (!ResetEvent(m_signalEvent))
                {
                    RIP("Must always be able to reset an event unless closed");
                }
            }
            else
            {
                RIP("Unexpected signal received in m_MsgWaitForMultipleObjects");

                hr = E_UNEXPECTED;
            }
        }
    }
    while(!leave);

Cleanup:
    EXPECT_SUCCESSID(0, hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CStateThread::WaitForinitialization
//
//  Synopsis:
//      Waits for the state thread to start up and initialize. If something
//      fails in the thread, the failure will be returned here.
//
//------------------------------------------------------------------------------
HRESULT
CStateThread::
WaitForInitialization(
    void
    )
{
    if (WaitForSingleObject(m_initializedEvent, INFINITE) != WAIT_OBJECT_0)
    {
        RIP("An object can always be waiting for unless not created or closed");
    }

    return m_initHR;
}


