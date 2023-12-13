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
//      A simple wrapper around CEventProxy for sending media specific events.
//
//  $ENDTAG
//
//------------------------------------------------------------------------------
#include "precomp.hpp"
#include "mediaeventproxy.tmh"

MtDefine(CMediaEventProxy,  Mem, "CMediaEventProxy");
MtDefine(EventItem,         Mem, "EventItem");

//
// Public methods
//

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMediaEventProxy::RaiseEvent
//
//  Synopsis:
//      Raise a media specific event up to the managed layer
//
//------------------------------------------------------------------------------
HRESULT
CMediaEventProxy::
RaiseEvent(
    __in AVEvent eventType,
        //  The type of the event to raise
    __in HRESULT failureHr
        // The failure code (if any) associated with it.
    )
 {
    HRESULT     hr = S_OK;
    EventItem   *pEventItem = NULL;
    TRACEF(&hr);

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_EVENTS,
        "Adding event to queue: Event %d, with hr %x",
        eventType,
        failureHr);

    IFC(EventItem::Create(m_uiID, m_pCEventProxy, eventType, NULL, NULL, failureHr, &pEventItem));

    IFC(m_pEventThread->AddItem(pEventItem));

Cleanup:
    ReleaseInterface(pEventItem);

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CMediaEventProxy::RaiseEvent
//
//  Synopsis:
//      Raise a media specific event up to the managed layer
//
//------------------------------------------------------------------------------
HRESULT
CMediaEventProxy::
RaiseEvent(
    __in        AVEvent    eventType,
    __in_opt    PCWSTR     type,
    __in_opt    PCWSTR     param,
    __in        HRESULT    failureHr
    )
{
    HRESULT hr = S_OK;
    EventItem   *pEventItem = NULL;

    TRACEF(&hr);

    static const WCHAR    empty[] = L"";

    if (type == NULL)
    {
        type = empty;
    }

    if (param == NULL)
    {
        param = empty;
    }

    LogAVDataM(
        AVTRACE_LEVEL_INFO,
        AVCOMP_EVENTS,
        "Adding event to queue: Event %d, with hr %x and type %ws, param %ws",
        eventType,
        failureHr,
        type,
        param);

    IFC(EventItem::Create(m_uiID, m_pCEventProxy, eventType, type, param, failureHr, &pEventItem));

    IFC(m_pEventThread->AddItem(pEventItem));

Cleanup:
    ReleaseInterface(pEventItem);

    EXPECT_SUCCESS(hr);
    RRETURN(hr);
}

void
CMediaEventProxy::
Shutdown(
    void
    )
{
    TRACEF(NULL);
    m_pCEventProxy->Shutdown();
}

//
// Private methods
//
CMediaEventProxy::
CMediaEventProxy(
    __in        UINT                    uiID,
    __in_opt    CEventProxy             *pCEventProxy
    ) : m_uiID(uiID),
        m_pCEventProxy(NULL)
{
    TRACEF(NULL);

    SetInterface(m_pCEventProxy, pCEventProxy);
}

/*virtual*/
CMediaEventProxy::
~CMediaEventProxy(
    )
{
    TRACEF(NULL);

    ReleaseInterface(m_pCEventProxy);
    ReleaseInterface(m_pEventThread);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMediaEventProxy::Init (private)
//
//  Synopsis:
//      Initialize state that might fail. It is possible for
//      CCriticalSection::Init to fail.
//
//------------------------------------------------------------------------------
HRESULT
CMediaEventProxy::
Init(
    void
    )
{
    HRESULT     hr = S_OK;

    TRACEF(&hr);

    IFC(m_stateLock.Init());

    IFC(CStateThread::CreateEventThread(&m_pEventThread));

Cleanup:

    EXPECT_SUCCESS(hr);

    RRETURN(hr);
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CMediaEventProxy::EventItem::Create (static)
//
//  Synopsis:
//      Creates a CStateThreadItem to raise an event in the event thread
//
//------------------------------------------------------------------------------
/*static*/
HRESULT
CMediaEventProxy::EventItem::
Create(
    __in        UINT                    id,
    __in        CEventProxy             *pCEventProxy,
    __in        AVEvent                 eventType,
    __in_opt    PCWSTR                  type,
    __in_opt    PCWSTR                  param,
    __in        HRESULT                 failureHr,
    __deref_out EventItem               **ppEventItem
    )
{
    HRESULT             hr = S_OK;
    EventItem           *pEventItem = NULL;

    TRACEFID(id, &hr);

    pEventItem = new EventItem(id, pCEventProxy, eventType, failureHr);

    IFCOOM(pEventItem);

    IFC(pEventItem->Init(type, param));

    *ppEventItem = pEventItem;
    pEventItem = NULL;

Cleanup:
    ReleaseInterface(pEventItem);

    EXPECT_SUCCESSID(id, hr);
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMediaEventProxy::EventItem::HrFindInterface, CMILCOMBase
//
//  Synopsis:
//            Get a pointer to another interface implemented by
//      EventItem
//
//------------------------------------------------------------------------------
STDMETHODIMP
CMediaEventProxy::EventItem::
HrFindInterface(
    __in_ecount(1) REFIID riid,
    __deref_out void **ppv
    )
{
    HRESULT hr = S_OK;

    //
    // IUnknown is handled by CMILCOMBase - no other interface requests are
    // valid.
    //

    LogAVDataM(
        AVTRACE_LEVEL_ERROR,
        AVCOMP_EVENTS,
        "Unexpected interface request: %!IID!",
        &riid);

    IFCN(E_NOINTERFACE);

Cleanup:
    RRETURN(hr);
}

//+-----------------------------------------------------------------------------
//
//  Member:
//      CMediaEventProxy::EventItem::Run,   CStateThreadItem
//
//  Synopsis:
//      This is called whenever we are run on the event thread side this
//      corresponds to an AddItem call on the apartment manager.
//
//------------------------------------------------------------------------------
__override
void
CMediaEventProxy::EventItem::
Run(
    void
    )
{
    HRESULT     hr = S_OK;
    AVEventData *pEventData = NULL;

    TRACEF(&hr);

    if (m_type == NULL)
    {
        AVEventData eventData = { m_eventType, m_failureHr, 0, 0 };

        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_EVENTS,
            "Raising Event %d, with hr %x",
            m_eventType,
            m_failureHr);

        IFC(m_pCEventProxy->RaiseEvent(
            reinterpret_cast<BYTE *>(&eventData), 
            sizeof(eventData)));
    }
    else
    {
        LogAVDataM(
            AVTRACE_LEVEL_INFO,
            AVCOMP_EVENTS,
            "Raising Event %d, with hr %x and type %ws, param %ws",
            m_eventType,
            m_failureHr,
            m_type,
            m_param);

        size_t  typeLength = wcslen(m_type);
        size_t  paramLength = wcslen(m_param);

        //
        // Length is the size of the structure plus the size of the two strings (Not null terminated)
        // The strings at the end are NULL terminated and this is taken care of by the 1 size
        // of the typeAndParamStrings array.
        //
        size_t  numChars = 0;
        size_t  stringSize = 0;
        size_t  totalSize = 0;

        IFC(SizeTAdd(typeLength, paramLength, &numChars));

        IFC(SizeTMult(numChars, sizeof(WCHAR), &stringSize));

        IFC(SizeTAdd(stringSize, sizeof(AVEventData), &totalSize));

        //
        // Won't send up a set of commands that are bigger than 4K of memory.
        // 
        if (totalSize > maximumEventPacketSize)
        {
            IFC(E_INVALIDARG);
        }

        pEventData = reinterpret_cast<AVEventData *>(new byte[totalSize]);
        IFCOOM(pEventData);

        pEventData->avEvent = m_eventType;
        pEventData->errorHResult = m_failureHr;

        pEventData->typeLength = static_cast<ULONG>(typeLength);
        pEventData->paramLength = static_cast<ULONG>(paramLength);

        WCHAR   *pString = pEventData->typeAndParamStrings;

        //
        // This is the size of the buffer remaining for copying the strings into.        
        // 
        size_t  stringBufferSize = (totalSize - (reinterpret_cast<BYTE *>(pString) - reinterpret_cast<BYTE *>(pEventData))) / 2;

        IFC(StringCchCopyEx(pString, stringBufferSize, m_type, &pString, &stringBufferSize, 0));

        IFC(StringCchCopy(pString, stringBufferSize, m_param));

        IFC(m_pCEventProxy->RaiseEvent(
            reinterpret_cast<BYTE *>(pEventData), 
            static_cast<ULONG>(totalSize)));
    }

Cleanup:
    delete[] reinterpret_cast<byte *>(pEventData);

    EXPECT_SUCCESS(hr);
}

CMediaEventProxy::EventItem::
EventItem(
    __in        UINT                    uiID,
    __in        CEventProxy             *pCEventProxy,
    __in        AVEvent                 eventType,
    __in        HRESULT                 failureHr
    ) : m_uiID(uiID),
        m_pCEventProxy(NULL),
        m_eventType(eventType),
        m_type(NULL),
        m_param(NULL),
        m_failureHr(failureHr)
{
    AddRef();

    SetInterface(m_pCEventProxy, pCEventProxy);
}

CMediaEventProxy::EventItem::
~EventItem(
    )
{
    ReleaseInterface(m_pCEventProxy);

    delete[] m_type;
    m_type = NULL;

    delete[] m_param;
    m_param = NULL;
}

HRESULT
CMediaEventProxy::EventItem::
Init(
    __in_opt    PCWSTR                  type,
    __in_opt    PCWSTR                  param
    )
{
    HRESULT hr = S_OK;

    IFC(CopyHeapString(type, &m_type));
    IFC(CopyHeapString(param, &m_param));

Cleanup:
    RRETURN(hr);
}


