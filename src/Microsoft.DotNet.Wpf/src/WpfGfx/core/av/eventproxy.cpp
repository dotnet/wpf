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

#include "precomp.hpp"
#include "eventproxy.tmh"

MtDefine(CEventProxy, Mem, "CEventProxy");

LONG CEventProxy::ms_mediaCount = 0;

// +---------------------------------------------------------------------------
//
// CEventProxyDescriptor::CEventProxyDescriptor
//
// +---------------------------------------------------------------------------
CEventProxyDescriptor::CEventProxyDescriptor() :
    pfnDispose(NULL),
    pfnRaiseEvent(NULL),
    m_handle(0)
{
}

// +---------------------------------------------------------------------------
//
// CEventProxy::CEventProxy
//
// +---------------------------------------------------------------------------
CEventProxy::CEventProxy() :
    m_ulRef(1),
    m_isShutdown(false)
{

    if (InterlockedIncrement(&ms_mediaCount) <= 1)
    {
        WPP_INIT_TRACING(L"Microsoft\\Avamedia");

        LogAVDataX(
            AVTRACE_LEVEL_ERROR,
            AVCOMP_MILAV,
            "----------------------Starting new event log-------------------------");
    }

    LogAVDataX(
        AVTRACE_LEVEL_INFO,
        AVCOMP_MILAV,
        "CEventProxy()" #
        " [,%p]",
        this);
}

// +---------------------------------------------------------------------------
//
// CEventProxy::~CEventProxy
//
// +---------------------------------------------------------------------------
CEventProxy::~CEventProxy()
{
    // Identifies structured exception with code == EXCEPTION_EXX
    // and args[0] == E_PROCESS_SHUTDOWN_REENTRY
    //
    // EXCEPTION_EXX and E_PROCESS_SHUTDOWN_REENTRY are not documented per se, 
    // but they are publically known through coreclr's open-sourced
    // github repo, and should be safe to rely upon as fixed values 
    // that are unlikely to change and thus regress this check.
    auto ExceptionFilter = [](LPEXCEPTION_POINTERS exInfo) -> DWORD
    {
        // defined in clr/src/inc/corexcep.h
        ULONG EXCEPTION_EXX = 0xe0455858;

        // defined in clr/src/vm/runtimeexceptionkind.h
        HRESULT E_PROCESS_SHUTDOWN_REENTRY = HRESULT_FROM_WIN32(ERROR_PROCESS_ABORTED);

        if ((exInfo != nullptr) &&
            (exInfo->ExceptionRecord != nullptr) &&
            (exInfo->ExceptionRecord->ExceptionCode == EXCEPTION_EXX) &&
            (exInfo->ExceptionRecord->NumberParameters > 0) &&
            (exInfo->ExceptionRecord->ExceptionInformation[0] == static_cast<ULONG>(E_PROCESS_SHUTDOWN_REENTRY)))
        {
            return EXCEPTION_EXECUTE_HANDLER;
        }
        else
        {
            return EXCEPTION_CONTINUE_SEARCH;
        }
    };

    // Ensures that the process wouldn't crash if m_epd.pfnDispose(&m_epd)
    // is called during process shutdown, resulting in an unservicable 
    // reverse-P/Invoke call.
    // 
    // The SEH exception cannot be directly handled in CEventProxy::~CEventProxy 
    // due to the constraint imposed by compiler error C2712 (cannot use __try in 
    // functions that require object unwinding), so we introduce a separate
    // lambda to handle this. 
    auto Dispose = [&ExceptionFilter](CEventProxyDescriptor& epd) -> void
    {
        __try
        {
            epd.pfnDispose(&epd);
        }
        __except (ExceptionFilter(GetExceptionInformation()))
        {
            // Do nothing
        }
    };

    Dispose(m_epd);
}

// +---------------------------------------------------------------------------
//
// CEventProxy::Create
//
// +---------------------------------------------------------------------------
HRESULT CEventProxy::Create(
    __in_ecount(1) const CEventProxyDescriptor &epd,
    __deref_out_ecount(1) CEventProxy **ppEventProxy
    )
{
    HRESULT hr = S_OK;
    CEventProxy *pEventProxy = NULL;

    pEventProxy = new CEventProxy(); // refcount = 1
    IFCOOM(pEventProxy);

    IFC(pEventProxy->Init(epd));

    *ppEventProxy = pEventProxy;
    pEventProxy = NULL;

Cleanup:
    delete pEventProxy;
    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CEventProxy::RaiseEvent
//
// +---------------------------------------------------------------------------
HRESULT CEventProxy::RaiseEvent(
    BYTE *pb,
    ULONG cb
    )
{
    HRESULT hr = S_OK;

    CGuard<CCriticalSection> guard(m_lock);

    if (!m_isShutdown)
    {
        IFC(m_epd.pfnRaiseEvent(&m_epd, pb, cb));
    }

Cleanup:
    RRETURN(hr);
}

// +---------------------------------------------------------------------------
//
// CEventProxy::Shutdown
//
// +---------------------------------------------------------------------------
void
CEventProxy::
Shutdown(
    void
    )
{
    if (m_lock.IsValid())
    {
        m_lock.Enter();
    }

    m_isShutdown = true;


    if (m_lock.IsValid())
    {
        m_lock.Leave();
    }

}

/*=========================================================================*\
    Support methods
\*=========================================================================*/

ULONG CEventProxy::AddRef()
{
    return InterlockedIncrement(&m_ulRef);
}

ULONG CEventProxy::Release()
{
    long cRef;

    cRef = InterlockedDecrement(&m_ulRef);
    if (0 == cRef)
    {
        delete this;
    }

    return cRef;
}

HRESULT CEventProxy::QueryInterface(REFIID riid, void **ppvObject)
{
    HRESULT hr = S_OK;

    CHECKPTRARG(ppvObject);

    *ppvObject = NULL;
    if (riid == IID_IMILEventProxy)
    {
        *ppvObject = static_cast<IMILEventProxy *>(this);
    }
    else if (riid == IID_IUnknown)
    {
        *ppvObject = static_cast<IUnknown *>(this);
    }
    else
    {
        hr = E_NOINTERFACE;
    }

    if (SUCCEEDED(hr))
    {
        AddRef();
    }

Cleanup:
    RRETURN(hr);
}

HRESULT
CEventProxy::
Init(
    __in_ecount(1) const CEventProxyDescriptor &epd
    )
{
    HRESULT hr = S_OK;

    m_epd = epd;
    IFC(m_lock.Init());

Cleanup:
    RRETURN(hr);
}

