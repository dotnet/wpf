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

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem constructor
//
//  Synopsis:
//      Instantiates the object
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
WmpStateEngineProxyItem<Class, Datatype>::
WmpStateEngineProxyItem(
    __in    UINT                uiID,
    __in    CWmpStateEngine     *pCWmpStateEngine,
    __in    Class               *pClass,
    __in    Method              method,
            Datatype            data
    ) : m_uiID(uiID),
        m_pCWmpStateEngine(pCWmpStateEngine),
        m_pClass(pClass),
        m_method(method),
        m_data(data),
        m_callCompletedEvent(NULL),
        m_result(S_OK)
{
    AddRef();
    m_pCWmpStateEngine->AddRef();
    m_pClass->AddRef();
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem destructor
//
//  Synopsis:
//      Cleans up resources
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
WmpStateEngineProxyItem<Class, Datatype>::
~WmpStateEngineProxyItem(
    void
    )
{
    ReleaseInterface(m_pCWmpStateEngine);
    ReleaseInterface(m_pClass);

    if (m_callCompletedEvent)
    {
        CloseHandle(m_callCompletedEvent);
        m_callCompletedEvent = NULL;
    }
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem::Run, CStateThreadItem
//
//  Synopsis:
//      Called from the state thread
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
__override
void
WmpStateEngineProxyItem<Class, Datatype>::
Run(
    void
    )
{
    HRESULT hr = S_OK;

    THR(hr = (m_pClass->*m_method)(m_data));

    //
    // Store the result for the caller
    //
    m_result = hr;

    //
    // Notify the calling thread that we've finished.
    //
    if (!SetEvent(m_callCompletedEvent))
    {
        RIP("The only way an event can fail to be signalled is if the handle has become invalid through a Close");
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem::Cancel, CStateThreadItem
//
//  Synopsis:
//      Called from the state thread
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
__override
void
WmpStateEngineProxyItem<Class, Datatype>::
Cancel(
    void
    )
{
    //
    // This will only be called if the state thread is being shutdown. Shutdown
    // always come through the UI thread or the garbage collector thread.
    // In each case, the UI thread can't be waiting for us to complete.
    // So we'll return MF_E_SHUTDOWN to the caller.
    //
    m_result = MF_E_SHUTDOWN;

    ReleaseInterface(m_pCWmpStateEngine);
    ReleaseInterface(m_pClass);

    //
    // Notify the calling thread that we've finished.
    //
    if (!SetEvent(m_callCompletedEvent))
    {
        RIP("The only way an event can fail to be signalled is if the handle has become invalid through a Close");
    }
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem::Cancel, CStateThreadItem
//
//  Synopsis:
//      Called from the state thread
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
__override
bool
WmpStateEngineProxyItem<Class, Datatype>::
IsAnOwner(
    __in    IUnknown    *pIUnknown
    )
{
    HRESULT     hr = S_OK;
    IUnknown    *pIStateEngineUnknown = NULL;
    bool        isAnOwner = false;

    IFC(m_pCWmpStateEngine->QueryInterface(IID_IUnknown, reinterpret_cast<void**>(&pIStateEngineUnknown)));

    if (pIStateEngineUnknown == pIUnknown)
    {
        isAnOwner = true;
    }

Cleanup:
    ReleaseInterface(pIStateEngineUnknown);

    EXPECT_SUCCESSINL(hr);

    return isAnOwner;
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem::CallMethod
//
//  Synopsis:
//      Initializes, waits to get the data, and returns the data
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
HRESULT
WmpStateEngineProxyItem<Class, Datatype>::
CallMethod(
    __in    bool            waitForCompletion
    )
{
    HRESULT hr = S_OK;

    //
    // Make ourselves secure against multiple calls
    //
    if (!m_callCompletedEvent)
    {
        m_callCompletedEvent
            = CreateEvent(
                    NULL,                   // Security Attributes
                    TRUE,                   // Manual Reset
                    FALSE,                  // Initial State is not signalled
                    NULL);                  // Name

        if (NULL == m_callCompletedEvent)
        {
            hr = GetLastErrorAsFailHR();

            goto Cleanup;
        }
    }

    //
    // Schedule ourselves to be run on the state thread.
    //
    IFC(m_pCWmpStateEngine->AddItem(this));

    //
    // Wait until we've been run
    //
    if (waitForCompletion)
    {
        if (WaitForSingleObject(m_callCompletedEvent, INFINITE) != WAIT_OBJECT_0)
        {
            RIP("An object can always be waited for unless not created or closed");
        }

        //
        // If we've been told to wait for completion, then we'll give up our references
        // as soon as we're finished running. That way the references' destructors
        // won't be run on the state thread, which can lead to deadlocks.
        //
        ReleaseInterface(m_pCWmpStateEngine);
        ReleaseInterface(m_pClass);
    }


    //
    // Tell the caller about any errors that happened
    //
    IFC(m_result);

Cleanup:
    if (hr != MF_E_SHUTDOWN)
    {
        EXPECT_SUCCESSINL(hr);
    }

    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxyItem::HrFindInterface, CMILCOMBase
//
//  Synopsis:
//      Helper method for implementing QueryInterface
//
//------------------------------------------------------------------------------
template <typename Class, typename Datatype>
STDMETHODIMP
WmpStateEngineProxyItem<Class, Datatype>::
HrFindInterface(
    __in REFIID riid,
    __deref_out void **ppv
    )
{
    RRETURN(E_NOINTERFACE);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxy::CallMethod
//
//  Synopsis:
//      Helper functions
//
//------------------------------------------------------------------------------
template <typename Class, typename Method, typename Datatype>
HRESULT
WmpStateEngineProxy::
CallMethod(
    __in        UINT                uiID,
    __in        CWmpStateEngine     *pCWmpStateEngine,
    __in        Class               *pClass,
    __in        Method              method,
                Datatype            data
    )
{
    HRESULT hr = S_OK;

    WmpStateEngineProxyItem<Class, Datatype> *proxy
        = new WmpStateEngineProxyItem<Class, Datatype>(
            uiID,
            pCWmpStateEngine,
            pClass,
            method,
            data);

    IFCOOM(proxy);

    IFC(proxy->CallMethod(true));

Cleanup:
    ReleaseInterface(proxy);

    EXPECT_SUCCESSINLID(uiID, hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      WmpStateEngineProxy::AsyncCallMethod
//
//  Synopsis:
//      Helper functions
//
//------------------------------------------------------------------------------
template <typename Class, typename Method, typename Datatype>
HRESULT
WmpStateEngineProxy::
AsyncCallMethod(
    __in        UINT                uiID,
    __in        CWmpStateEngine     *pCWmpStateEngine,
    __in        Class               *pClass,
    __in        Method              method,
                Datatype            data
    )
{
    HRESULT hr = S_OK;

    WmpStateEngineProxyItem<Class, Datatype> *proxy
        = new WmpStateEngineProxyItem<Class, Datatype>(
            uiID,
            pCWmpStateEngine,
            pClass,
            method,
            data);

    IFCOOM(proxy);

    IFC(proxy->CallMethod(false));

Cleanup:
    ReleaseInterface(proxy);

    EXPECT_SUCCESSINLID(uiID, hr);
    RRETURN(hr);
}

