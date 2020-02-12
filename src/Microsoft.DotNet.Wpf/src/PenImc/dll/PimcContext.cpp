// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcContext.cpp : Implementation of CPimcContext

#include "stdafx.h"

#include "ComApartmentVerifier.hpp"
#include "pblist.h"
#include "PimcContext.h"
#include "..\tablib\sidutils.h"
#include "..\tablib\scopes.h"

using namespace ComUtils;

/////////////////////////////////////////////////////////////////////////////
//
// NOTE (alexz) There are several key assumptions used here to simplify this code.
//              Should the assumptions change, the code may break in a subtle
//              way. This includes data corruption due to missing synchronization, etc.
//              Look for ASSUMPTION markers for more details.
//

/////////////////////////////////////////////////////////////////////////////
// CPimcContext

/////////////////////////////////////////////////////////////////////////////

CPimcContext::CPimcContext() :
    m_sink(new CEventSink()), m_hEventMoreData(NULL), m_hEventClientReady(NULL),
    m_hMutexSharedMemory(NULL), m_hFileMappingSharedMemory(NULL), 
    m_pSharedMemoryHeader(NULL), m_pbSharedMemoryRawData(NULL), 
    m_cHandles(0), m_pHandles(NULL), m_cbPackets(0), 
    m_pbPackets(NULL), m_fCommHandleOutstanding(FALSE),
    m_pMgr(NULL), m_pPacketDescription(NULL), m_hEventUpdate(NULL), m_fIsTopmostHook(FALSE)
{
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::Init(
    __inout CComPtr<CPimcManager>    pMgr,
    __in_opt CComPtr<ITabletContext> pCtxS,
    __in HWND                     hwnd,
    TABLET_CONTEXT_ID             tcid,
    PACKET_DESCRIPTION *          pPacketDescription)
{
    DHR;
    // Make sure we clean up properly on failures.
    bool fCleanupCritSection = false;
    bool fCleanupHook = false;
    bool fCleanupCtx = false;
    
    m_pMgr  = pMgr;

    if (pCtxS)
    {
        // DDVSO:289954
        // We need to store the ITabletContextP inside the COM Global Interface Table
        // (GIT) because the proxy we get here from the QueryInterface will not be 
        // valid when used within the ExecuteUpdates function.  Using the GIT ensures
        // that we get an appropriate proxy when the time comes.
        CHR(pCtxS->QueryInterface(IID_ITabletContextP, reinterpret_cast<void**>(&m_pCtxS)));
        m_wispContextLock = GitComLockableWrapper<ITabletContextP>(m_pCtxS, ComApartmentVerifier::Mta());
        CHR(m_wispContextLock.CheckCookie());
        
        fCleanupCtx = true;

        m_fIsTopmostHook = (m_pCtxS->IsTopMostHook() == S_OK);
    }

    m_tcid  = tcid;
    m_pPacketDescription = pPacketDescription;

    m_dwUpdatesPending = 0;
    InitializeCriticalSection(&m_csUpdates);
    fCleanupCritSection = true;
    m_hEventUpdate = CreateEvent(NULL, FALSE, FALSE, NULL);
    CHR(m_hEventUpdate  ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_CREATEEVENT_CALL));

    CHR(m_pMgr->InstallWindowHook(hwnd, this));
    fCleanupHook = true;

    if (pCtxS)
    {
        CComPtr<ITabletContextP> pCtxP;
        CHR(m_pCtxS->QueryInterface(IID_ITabletContextP, (void**)&pCtxP));

        hr = InitUnnamedCommunications(pCtxP);

        // NOTICE-2006/05/25-WAYNEZEN,
        // The named communications is supported by wisptis on Vista ONLY.
        if ( hr == E_ACCESSDENIED && m_pMgr->IsVistaOrGreater() )
        {
            hr = InitNamedCommunications(pCtxP);
        }

        CHR(hr);
    }

    m_fSingleFireTimeout = FALSE;
    m_dwSingleFireTimeout = INFINITE;

    RHR;

CLEANUP:
    // On failure, make sure we clean up things.
    if (fCleanupHook)
        m_pMgr->UninstallWindowHook(this);
    if (fCleanupCritSection)
        DeleteCriticalSection(&m_csUpdates);
    SafeCloseHandle(&m_hEventUpdate);

    if (fCleanupCtx)
        m_wispContextLock.RevokeIfValid();

    m_pMgr = NULL;

    m_pPacketDescription = NULL;
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

void CPimcContext::FinalRelease() 
{
    if (m_pMgr != NULL)
    {
        m_pMgr->UninstallWindowHook(this);

        ShutdownSharedMemoryCommunications();

        if (m_pPacketDescription)
        {
            DestroyPacketDescription(m_pPacketDescription);
            m_pPacketDescription = NULL;
        }

        DeleteCriticalSection(&m_csUpdates);
        SafeCloseHandle(&m_hEventUpdate);

        m_wispContextLock.RevokeIfValid();

       m_pMgr = NULL;
    }
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::InitUnnamedCommunications(__in CComPtr<ITabletContextP> pCtxP)
{
    DHR;

    CHR(pCtxP->UseSharedMemoryCommunications(
        GetCurrentProcessId(),
        (DWORD*)&m_hEventMoreData,
        (DWORD*)&m_hEventClientReady,
        (DWORD*)&m_hMutexSharedMemory,
        (DWORD*)&m_hFileMappingSharedMemory));
    CHR(InitCommunicationsCore());

CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::InitNamedCommunications(__in CComPtr<ITabletContextP> pCtxP)
{
    DHR;

    DWORD dwPid = GetCurrentProcessId();
    DWORD dwMoreDataEventId;
    DWORD dwClientReadyEventId;
    DWORD dwSharedMemoryMutexId;
    DWORD dwFileMappingId;

    ScopedLocalString pszSid;
    ScopedLocalString pszSidIntegrity;

    ASSERTSZ(m_pMgr->IsVistaOrGreater(), _T("Only Vista supports the named communications."));

    CHR(GetUserSid(&pszSid.get()));
    CHR(GetMandatoryLabel(&pszSidIntegrity.get()));
    
    CHR(pCtxP->UseNamedSharedMemoryCommunications(
            dwPid,
            pszSid,
            pszSidIntegrity,
            &dwMoreDataEventId,
            &dwClientReadyEventId,
            &dwSharedMemoryMutexId,
            &dwFileMappingId));

    TCHAR szMoreDataName[MAX_PATH + 1];
    TCHAR szMutexName[MAX_PATH + 1];
    TCHAR szSectionName[MAX_PATH + 1];
    TCHAR szClientReadyName[MAX_PATH + 1];

    StringCchPrintf(
        szMoreDataName,
        LENGTHOFARRAY(szMoreDataName),
        WISPTIS_SM_MORE_DATA_EVENT_NAME,
        dwPid,
        dwMoreDataEventId);

    StringCchPrintf(
        szMutexName,
        LENGTHOFARRAY(szMutexName),
        WISPTIS_SM_MUTEX_NAME,
        dwPid,
        dwSharedMemoryMutexId);

    StringCchPrintf(
        szSectionName,
        LENGTHOFARRAY(szSectionName),
        WISPTIS_SM_SECTION_NAME,
        dwPid,
        dwFileMappingId);

    StringCchPrintf(
        szClientReadyName,
        LENGTHOFARRAY(szClientReadyName),
        WISPTIS_SM_THREAD_EVENT_NAME,
        dwClientReadyEventId);

    m_hEventClientReady = OpenEvent(EVENT_ALL_ACCESS, FALSE, szClientReadyName);
    CHR_WIN32(m_hEventClientReady);

    m_hEventMoreData = OpenEvent(SYNCHRONIZE, FALSE, szMoreDataName);
    CHR_WIN32(m_hEventMoreData);

    m_hMutexSharedMemory = OpenMutex(MUTEX_ALL_ACCESS, FALSE, szMutexName);
    CHR_WIN32(m_hMutexSharedMemory);

    m_hFileMappingSharedMemory = OpenFileMapping(FILE_MAP_READ | FILE_MAP_WRITE, FALSE, szSectionName);
    CHR_WIN32(m_hFileMappingSharedMemory);

    CHR(InitCommunicationsCore());

 CLEANUP:
    if(FAILED(hr))
    {
        SafeCloseHandle(&m_hFileMappingSharedMemory);
        SafeCloseHandle(&m_hMutexSharedMemory);
        SafeCloseHandle(&m_hEventMoreData);
        SafeCloseHandle(&m_hEventClientReady);
    }

    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::InitCommunicationsCore()
{
    DHR;

    CHR(m_hEventMoreData && m_hEventClientReady && m_hMutexSharedMemory && m_hFileMappingSharedMemory ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_USESHAREDMEMORYCOM_CALL));

    m_pSharedMemoryHeader = (SHAREDMEMORY_HEADER*)MapViewOfFile(
        m_hFileMappingSharedMemory,     // handle
        FILE_MAP_READ | FILE_MAP_WRITE, // desired access
        0,                              // offset in file, High
        0,                              // offset in file, Low
        sizeof(SHAREDMEMORY_HEADER));   // number of bytes to map
    CHR(m_pSharedMemoryHeader ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_SHAREDMEMORYHEADER_NULL));

#pragma prefast( suppress: 11, "Dereferencing NULL pointer 'm_pSharedMemoryHeader'." )
    m_pbSharedMemoryRawData = (BYTE*)MapViewOfFile(
        m_hFileMappingSharedMemory,     // handle
        FILE_MAP_READ,                  // desired access
        0,                              // offset in file, High
        0,                              // offset in file, Low
        m_pSharedMemoryHeader->cbTotal);// number of bytes to map
    CHR(m_pbSharedMemoryRawData ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_SHAREDMEMORYRAWDATA_NULL));

    m_pbSharedMemoryPackets = m_pbSharedMemoryRawData + sizeof(SHAREDMEMORY_HEADER);

    m_cHandles = 0;
    m_pHandles = NULL;
    m_cbPackets = 0;
    m_pbPackets = NULL;
    m_fCommHandleOutstanding = FALSE;

CLEANUP:
    if (FAILED(hr))
        ShutdownSharedMemoryCommunications();

    RHR;
}

///////////////////////////////////////////////////////////////////////////////

void CPimcContext::ShutdownSharedMemoryCommunications()
{
    if (m_pSharedMemoryHeader)
    {
        UnmapViewOfFile(m_pSharedMemoryHeader);
        m_pSharedMemoryHeader = NULL;
    }
    if (m_pbSharedMemoryRawData)
    {
        UnmapViewOfFile(m_pbSharedMemoryRawData);
        m_pbSharedMemoryRawData = NULL;
    }

    SafeCloseHandle(&m_hEventMoreData);
    SafeCloseHandle(&m_hEventClientReady);
    SafeCloseHandle(&m_hMutexSharedMemory);
    SafeCloseHandle(&m_hFileMappingSharedMemory);
    if (m_pHandles)
    {
        delete [] m_pHandles;
        m_pHandles = NULL;
    }
    if (m_pbPackets)
    {
        delete [] m_pbPackets;
        m_pbPackets = NULL;
    }
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::GetCommHandle(__out INT64* pHandle)
{
    // ASSUMPTION (alexz) this call is always balanced by ShutdownComm
    // (responsibility of the caller)
    DHR;
    CHR(pHandle ? S_OK : E_INVALIDARG);

    if (m_wispContextLock.GetCookie() != 0)
    {
        ASSERT (!m_fCommHandleOutstanding);
        CHR(!m_fCommHandleOutstanding ? S_OK : E_UNEXPECTED);
        m_fCommHandleOutstanding = TRUE;
        *pHandle = (INT_PTR)this;

        // DDVSO:514949
        // Create the CPimcContext and CEventSink lock here since we know this object is fully instantiated (including IUnknown).
        m_contextLock = ComLockableWrapper(this, ComApartmentVerifier::CurrentSta());
        m_sinkLock = ComLockableWrapper(m_sink.p, ComApartmentVerifier::CurrentSta());

        // DDVSO:514949
        // Make sure that we increase the ref count here since we
        // need to ensure that the apartment where this object lives
        // stays alive.
        AddRef();

        // DDVSO:514949
        // Calling this ensures that the CStdIdentity for this object is
        // not released if we hit a COM rundown due to OSGVSO:10779198.
        CHR(m_contextLock.Lock());

        // DDVSO:514949
        // Lock the CEventSink so WISP can rely on its proxy to it.
        CHR(m_sinkLock.Lock());
    }

CLEANUP:
    RHR;
}

///////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcContext::ShutdownComm()
{
    DHR;

    if (m_wispContextLock.GetCookie() != 0)
    {
        ASSERT(m_fCommHandleOutstanding);
        CHR (m_fCommHandleOutstanding ? S_OK : E_UNEXPECTED);
        m_fCommHandleOutstanding = FALSE;

        // DDVSO:514949
        // Balance the call in Init.
        CHR(m_sinkLock.Unlock());

        // DDVSO:514949
        // Balance the call in GetCommHandle.
        CHR(m_contextLock.Unlock());

        // DDVSO:514949
        // Balance out any GetCommHandle call here.  This will be done
        // when the PenThread no longer is using this context.
        Release();
    }

CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::GetKey(__out INT * pKey)
{
    DHR;
    CHR(pKey ? S_OK : E_INVALIDARG);
    *pKey = m_tcid;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcContext::GetPacketDescriptionInfo(__out INT * pcProps, __out INT * pcButtons)
{
    DHR;
    CHR(pcProps ? S_OK : E_INVALIDARG);
    CHR(pcButtons ? S_OK : E_INVALIDARG);
    *pcProps   = m_pPacketDescription->cPacketProperties;
    *pcButtons = m_pPacketDescription->cButtons;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcContext::GetPacketPropertyInfo(INT iProp, __out GUID * pGuid, __out INT * piMin, __out INT * piMax, __out INT * piUnits, __out FLOAT *pflResolution)
{
    HRESULT hr = S_OK;

    switch (iProp)
    {
        case QUERY_WISP_CONTEXT_KEY:
        {
            if (nullptr == piMin)
            {
                hr = E_INVALIDARG;
            }
            else
            {
                *piMin = (INT)m_wispContextLock.GetCookie();
            }
        }
        break;
        default:
        {
            hr = GetPacketPropertyInfoImpl(iProp, pGuid, piMin, piMax, piUnits, pflResolution);
        }
    }

    return hr;
}

STDMETHODIMP CPimcContext::GetPacketPropertyInfoImpl(INT iProp, __out GUID * pGuid, __out INT * piMin, __out INT * piMax, __out INT * piUnits, __out FLOAT *pflResolution)
{
    PACKET_PROPERTY * pProp = nullptr;
    DHR;
    CHR(0 <= iProp && (DWORD)iProp < m_pPacketDescription->cPacketProperties ? S_OK : E_INVALIDARG);
    CHR(pGuid         ? S_OK : E_INVALIDARG);
    CHR(piMin         ? S_OK : E_INVALIDARG);
    CHR(piMax         ? S_OK : E_INVALIDARG);
    CHR(piUnits        ? S_OK : E_INVALIDARG);
    CHR(pflResolution ? S_OK : E_INVALIDARG);
    // iProp value is checked above, disable prefast signedness warnings
#pragma prefast(suppress: 37001 37002 37003)
    pProp = &(m_pPacketDescription->pPacketProperties[iProp]);
    *pGuid          = pProp->guid;
    *piMin          = pProp->PropertyMetrics.nLogicalMin;
    *piMax          = pProp->PropertyMetrics.nLogicalMax;
    *piUnits        = pProp->PropertyMetrics.Units;
    *pflResolution  = pProp->PropertyMetrics.fResolution;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcContext::GetPacketButtonInfo(INT iButton, __out GUID * pGuid)
{
    DHR;
    CHR(0 <= iButton && (DWORD)iButton < m_pPacketDescription->cButtons ? S_OK : E_INVALIDARG);
    CHR(pGuid ? S_OK : E_INVALIDARG);
    // iButton value is checked above, disable prefast signedness warnings
#pragma prefast(suppress: 37001 37002 37003)
    *pGuid = m_pPacketDescription->pguidButtons[iButton];
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

void CPimcContext::DestroyPacketDescription(__in_opt PACKET_DESCRIPTION * pPacketDescription)
{
    if (pPacketDescription)
    {
        if (pPacketDescription->pPacketProperties)
            CoTaskMemFree(pPacketDescription->pPacketProperties);

        if (pPacketDescription->pguidButtons)
            CoTaskMemFree(pPacketDescription->pguidButtons);

        CoTaskMemFree(pPacketDescription);
    }
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::EnsureHandlesArray(INT cHandles)
{
    DHR;
    if (m_cHandles < cHandles)
    {
        if (m_pHandles)
            delete [] m_pHandles;
        m_cHandles = cHandles * 2;
        CHR_MEMALLOC(m_pHandles = new HANDLE[m_cHandles]);
    }
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::EnsurePackets(DWORD cb)
{
    DHR;
    if (m_cbPackets < cb)
    {
        if (m_pbPackets)
            delete [] m_pbPackets;
        m_cbPackets = max(256, cb * 2);
        CHR_MEMALLOC(m_pbPackets = new BYTE[m_cbPackets]);
    }
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::PostUpdate(DWORD update)
{
    DHR;
    EnterCriticalSection(&m_csUpdates);
    m_dwUpdatesPending |= update;
    LeaveCriticalSection(&m_csUpdates);
    SetEvent(m_hEventUpdate);
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::ExecuteUpdates()
{
    DWORD dwUpdatesPending;

    EnterCriticalSection(&m_csUpdates);
    dwUpdatesPending = m_dwUpdatesPending;
    m_dwUpdatesPending = 0;
    LeaveCriticalSection(&m_csUpdates);

    CComPtr<ITabletContextP>    pCtxS = nullptr;

    if (dwUpdatesPending)
    {
        // DDVSO:289954
        // Access the underlying WISP tablet context in order to properly respond to updates
        pCtxS = m_wispContextLock.GetComObject();

        if (pCtxS != nullptr)
        {
            // (order of these is important)

            if (dwUpdatesPending & UPDATE_SizeMove) // size, move
            {
                RECT rc;
                pCtxS->TrackInputRect(&rc);
            }
            if (dwUpdatesPending & UPDATE_SendToBack) // send to back
            {
                // If we are in wisptis PREHOOK (IsTopMost==true) queue then we can't call the Overlap API.
                if (!m_fIsTopmostHook)
                {
                    TABLET_CONTEXT_ID tcidT;
                    pCtxS->Overlap(/*fTop*/FALSE, &tcidT);
                }
            }
            if (dwUpdatesPending & UPDATE_SendToTop) // send to top
            {
                // If we are in wisptis PREHOOK (IsTopMost==true) queue then we can't call the Overlap API.
                if (!m_fIsTopmostHook)
                {
                    TABLET_CONTEXT_ID tcidT;
                    pCtxS->Overlap(/*fTop*/TRUE, &tcidT);
                }
            }
        }
        else
        {
            return E_INVALIDARG;
        }
    }

    return S_OK;
}

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcContext::GetLastSystemEventData(
    __out INT * piEvent, __out INT * piModifier, __out INT * piKey, 
    __out INT * piX, __out INT * piY, __out INT * piCursorMode, __out INT * piButtonState)
{
    DHR;
    CHR(piEvent       ? S_OK : E_INVALIDARG);
    CHR(piModifier    ? S_OK : E_INVALIDARG);
    CHR(piKey         ? S_OK : E_INVALIDARG);
    CHR(piX           ? S_OK : E_INVALIDARG);
    CHR(piY           ? S_OK : E_INVALIDARG);
    CHR(piCursorMode  ? S_OK : E_INVALIDARG);
    CHR(piButtonState ? S_OK : E_INVALIDARG);
    *piEvent       = (INT)m_sysEvt;
    *piModifier    = (INT)m_sysEvtData.bModifier;
    *piKey         = (INT)m_sysEvtData.wKey;
    *piX           = (INT)m_sysEvtData.xPos;
    *piY           = (INT)m_sysEvtData.yPos;
    *piCursorMode  = (INT)m_sysEvtData.bCursorMode;
    *piButtonState = (INT)m_sysEvtData.dwButtonState;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::SetSingleFireTimeout(UINT uiTimeout)
{
    DHR;
    CHR(1 <= uiTimeout ?  S_OK : E_INVALIDARG);
    m_dwSingleFireTimeout = uiTimeout;
CLEANUP:
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

#ifdef DELIVERY_PROFILING
void CPimcContext::ProfilePackets(BOOL fDown, BOOL fUp, int x, int y)
{
    LARGE_INTEGER counter;
    QueryPerformanceCounter(&counter);
    unsigned t = counter.LowPart & LONG_MAX;

    const  int      cMax = 5000;
    static int      cCur = 0;
    static int      xs[cMax];
    static int      ys[cMax];
    static unsigned ts[cMax];

    switch (fDown * 0x10 + fUp)
    {
    case 0x10:      // down
        cCur = 0;
        // fall thru

    case 0x00:      // packets
        if (cCur < cMax)
        {
            xs [cCur] = x;
            ys [cCur] = y;
            ts [cCur] = t;
            cCur++;
        }
        break;

    case 0x01:      // up
        {
            FILE * pf = fopen("c:\\perf_penimc_strokeProfile_wait.xml", "a+");
            if (pf)
            {
                fwprintf(pf, L"<stroke points = '%d'> \n", cCur);

                for (int i = 0; i < cCur; i++)
                {
                    fwprintf(pf,
                        L"    <point idx = '%d' t = '%u' x = '%d' y = '%d' /> \n",
                        i, ts[i], xs[i], ys[i]);
                }

                fwprintf(pf, L"</stroke> \n");

                fclose(pf);
            }
        }
        break;
    }
}
#endif // DELIVERY_PROFILING

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::GetPenEventCore(
    DWORD dwWait,
    __out BOOL * pfWaitAgain,
    __out BOOL * pfShutdown,
    __out INT * pEvt, __out INT * pCursorId, 
    __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets)
{
    DHR;

    ASSERT (pfShutdown);
    *pfShutdown  = FALSE;
    *pfWaitAgain = FALSE;

    switch (dwWait)
    {
    case WAIT_TIMEOUT:
        m_fSingleFireTimeout = FALSE; // (only fire the timeout once before more data shows up)
        *pEvt      = 1; // timeout event
        *pCursorId = 0;
        *pcPackets = 0;
        *pcbPacket = 0;
        *pPackets  = NULL;
        break;

    case WAIT_OBJECT_0 + 0: // update
        *pfWaitAgain = TRUE;
        ExecuteUpdates();
        break;

    case WAIT_OBJECT_0 + 1: // more data
        {
            m_fSingleFireTimeout = TRUE; // (got more data, set up for the time out again)

            // obtain mutex on the data
            DWORD dwWaitAccess = WaitForSingleObject(m_hMutexSharedMemory, INFINITE);
            CHR(dwWaitAccess == WAIT_OBJECT_0 ? S_OK : E_FAIL);

            // get the data
            switch (m_pSharedMemoryHeader->dwEvent)
            {
                case WM_TABLET_PACKET:
                case WM_TABLET_CURSORDOWN:
                case WM_TABLET_CURSORUP:
                    *pEvt      = m_pSharedMemoryHeader->dwEvent;
                    *pCursorId = m_pSharedMemoryHeader->cid;
                    *pcPackets = m_pSharedMemoryHeader->cPackets;
                    *pcbPacket = m_pSharedMemoryHeader->cbPackets / m_pSharedMemoryHeader->cPackets;
                    CHR(EnsurePackets(m_pSharedMemoryHeader->cbPackets));
                    CopyMemory(m_pbPackets, m_pbSharedMemoryPackets, m_pSharedMemoryHeader->cbPackets);
                    *pPackets  = (INT_PTR)m_pbPackets;

#ifdef DELIVERY_PROFILING
                    for (INT iPacket = 0; iPacket < *pcPackets; iPacket++)
                    {
                        INT iOffset = iPacket * (*pcbPacket) / sizeof(LONG);
                        switch (m_pSharedMemoryHeader->dwEvent)
                        {
                            case WM_TABLET_PACKET:     ProfilePackets(/*fDown*/FALSE, /*fUp*/FALSE, ((LONG*)m_pbSharedMemoryPackets)[iOffset + 0], ((LONG*)m_pbSharedMemoryPackets)[iOffset + 1]); break;
                            case WM_TABLET_CURSORDOWN: ProfilePackets(/*fDown*/TRUE,  /*fUp*/FALSE, ((LONG*)m_pbSharedMemoryPackets)[iOffset + 0], ((LONG*)m_pbSharedMemoryPackets)[iOffset + 1]); break;
                            case WM_TABLET_CURSORUP:   ProfilePackets(/*fDown*/FALSE, /*fUp*/TRUE,  ((LONG*)m_pbSharedMemoryPackets)[iOffset + 0], ((LONG*)m_pbSharedMemoryPackets)[iOffset + 1]); break;
                        }
                    }
#endif
                    break;

                case WM_TABLET_CURSORINRANGE:
                case WM_TABLET_CURSOROUTOFRANGE:
                    *pEvt      = m_pSharedMemoryHeader->dwEvent;
                    *pCursorId = m_pSharedMemoryHeader->cid;
                    *pcPackets = 0;
                    *pcbPacket = 0;
                    *pPackets  = NULL;
                    break;

                case WM_TABLET_SYSTEMEVENT:
                    *pEvt      = m_pSharedMemoryHeader->dwEvent;
                    *pCursorId = m_pSharedMemoryHeader->cid;
                    *pcPackets = 0;
                    *pcbPacket = 0;
                    *pPackets  = NULL;
                    m_sysEvt     = m_pSharedMemoryHeader->sysEvt;
                    m_sysEvtData = m_pSharedMemoryHeader->sysEvtData;
                    break;

                default:
                    *pEvt      = 0;
                    *pCursorId = 0;
                    *pcPackets = 0;
                    *pcbPacket = 0;
                    *pPackets  = NULL;
                    break;
            }

            // release the mutex we holding and signal wisptis to put more data here
            m_pSharedMemoryHeader->dwEvent = WISPTIS_SHAREDMEMORY_AVAILABLE;
            ReleaseMutex(m_hMutexSharedMemory);
            SetEvent(m_hEventClientReady);
        }
        break;

    case WAIT_OBJECT_0 + 2: // reset
        *pfShutdown = TRUE;
        break;

    default:                // an error condition; just keep rolling
        break;
    }

CLEANUP:
    RHR;
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::GetPenEvent(
    __in_opt HANDLE hEventReset, __out BOOL * pfShutdown,
    __out INT * pEvt, __out INT * pCursorId, 
    __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets)
{
    DHR;
    DWORD cObjects = 2;
    HANDLE ahObjects[3];
    ahObjects[0] = m_hEventUpdate;
    ahObjects[1] = m_hEventMoreData;
    if (hEventReset)
    {
        ahObjects[cObjects] = hEventReset;
        cObjects++;
    }

    for (;;)
    {
        DWORD dwTimeout = m_fSingleFireTimeout ? m_dwSingleFireTimeout : INFINITE;
        DWORD dwWait = MsgWaitForMultipleObjectsEx(cObjects, ahObjects, dwTimeout, 0, MWMO_ALERTABLE);
        
        BOOL fWaitAgain = FALSE;
        CHR(GetPenEventCore(dwWait, &fWaitAgain, pfShutdown, pEvt, pCursorId, pcPackets, pcbPacket, pPackets));
        if (!fWaitAgain)
            break;
    }

CLEANUP:
    RHR;
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcContext::GetPenEventMultiple(
    INT cCtxs, __in_ecount(cCtxs) CPimcContext ** ppCtxs,
    __in_opt HANDLE hEventReset,
    __out BOOL * pfShutdown, 
    __out INT * piCtxEvt,
    __out INT * pEvt, __out INT * pCursorId, 
    __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets)
{
    DHR;

    ASSERT (pfShutdown);
    *pfShutdown = FALSE;

    HANDLE * pHandles = NULL;
    INT      cHandles = 0;
    BOOL     fSingleFireTimeout = FALSE;
    DWORD    dwSingleFireTimeout = INFINITE;
    INT cCtxEvents = 0;
    CPimcContext ** ppCtxCur = NULL;
    
    // See if we have a special case where we don't have any real pen contexts
    // and just created the pen thread to get the UIContext on the pen thread set
    // up.  In this case we only need to wait for the reset event.
    if (cCtxs == 0)
    {
        cHandles = 1;
        pHandles = &hEventReset;
        //fSingleFireTimeout = true;
        //dwSingleFireTimeout = 500; // check every 500ms to see if we should shut down.
    }
    else
    {
        ASSERT (cCtxs);
        ASSERT (piCtxEvt);

        // build up the wait array
        for (INT i = 0; i < cCtxs; i++)
        {
            CPimcContext * pCtxHandleArray = ppCtxs[i];

            // Create handles array on the context only if it participates in the wait.
            if (pCtxHandleArray != NULL &&
                pCtxHandleArray->m_hEventMoreData)
            {
                CHR(pCtxHandleArray->EnsureHandlesArray(2 * cCtxs + 1));  // ASSUMPTION (alexz) no context is invoked on 2 separate threads
                                                                          // via GetPenEvent/GetPenEventMultiple, at the same time
                pHandles = pCtxHandleArray->m_pHandles;
                break;
            }
        }

        if (NULL == pHandles)
        {
            cHandles = 1;
            pHandles = &hEventReset;
        }
        else
        {
            HANDLE * phCur = pHandles;
        
            ppCtxCur = ppCtxs;
            for (INT i = 0; i < cCtxs; i++)
            {
                if ((*ppCtxCur) && (*ppCtxCur)->m_hEventMoreData)
                {
                    *phCur = (*ppCtxCur)->m_hEventUpdate;
                    phCur++;
                    cHandles++;

                    *phCur = (*ppCtxCur)->m_hEventMoreData;
                    phCur++;
                    cHandles++;

                    fSingleFireTimeout |= (*ppCtxCur)->m_fSingleFireTimeout;
                    dwSingleFireTimeout = min (dwSingleFireTimeout, (*ppCtxCur)->m_dwSingleFireTimeout);
                }
                ppCtxCur++;
            }

            cCtxEvents = cHandles;
            if (hEventReset)
            {
                *phCur = hEventReset;
                phCur++;
                cHandles++;
            }
        }
    }
    
    // do the wait
    for (;;)
    {
        DWORD dwTimeout = fSingleFireTimeout ? dwSingleFireTimeout : INFINITE;
        DWORD dwWait = MsgWaitForMultipleObjectsEx(cHandles, pHandles, dwTimeout, 0, MWMO_ALERTABLE);
        BOOL fWaitAgain = FALSE;
        // dispatch the result of wait
        if (dwWait == WAIT_TIMEOUT)
        {
            // If we hit a timeout when we don't have any real contexts then just deal with it as a
            // shutdown so we'll check to see if we should shut this thread down.
            if (cCtxs == 0)
            {
                *pfShutdown = TRUE;
            }
            else
            {
                *piCtxEvt  = 0;
                *pEvt      = 1; // timeout event
                *pCursorId = 0;
                *pcPackets = 0;
                *pcbPacket = 0;
                *pPackets  = NULL;
                ppCtxCur = ppCtxs;
                for (INT i = 0; i < cCtxs; i++)
                {
                    if ( (*ppCtxCur) != NULL  )
                        (*ppCtxCur)->m_fSingleFireTimeout = FALSE; // (only fire the timeout once before more data shows up)
                    
                    ppCtxCur++;
                }
            }
        }
        else if (dwWait < WAIT_OBJECT_0 + cCtxEvents)
        {
            // Either more data or update event for a context was
            // signaled. Find it and call GetPenEventCore on it.
            HANDLE signaledHandle = pHandles[dwWait];
            *piCtxEvt = -1;
            for (INT i = 0; i < cCtxs; i++)
            {
                // Check if the signaled handle belongs to this context
                CPimcContext * pCtxHandle = ppCtxs[i];
                if (pCtxHandle != NULL &&
                    (pCtxHandle->m_hEventMoreData == signaledHandle ||
                     pCtxHandle->m_hEventUpdate == signaledHandle))
                {
                    *piCtxEvt = i;
                    break;
                }
            }
            ASSERT(*piCtxEvt != -1);
            CPimcContext * pCtxEvt = ppCtxs[*piCtxEvt];
            dwWait = dwWait % 2;
            CHR(pCtxEvt->GetPenEventCore(dwWait, &fWaitAgain, pfShutdown, pEvt, pCursorId, pcPackets, pcbPacket, pPackets));
        }
        else if (WAIT_OBJECT_0 + cCtxEvents == dwWait)
        {
            // wait was reset
            *pfShutdown = TRUE;
        }
        else
        {
            // an unexpected condition; ignore it
        }
        if (!fWaitAgain)
            break;
    }

CLEANUP:
    RHR;
}

///////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI GetPenEvent(
    __typefix(CPimcContext *) __in INT_PTR commHandle,
    __typefix(HANDLE) __in_opt INT_PTR commHandleReset,
    __out INT * pEvt, __out INT * pCursorId,
    __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets)
{
    CPimcContext * pCtx = nullptr;
    DHR;
    BOOL fShutdown = TRUE;
    CHR(commHandle && pEvt && pCursorId && pcPackets && pcbPacket && pPackets ? S_OK : E_INVALIDARG);
    pCtx = (CPimcContext *)commHandle;
    CHR(pCtx->GetPenEvent((HANDLE)commHandleReset, &fShutdown, pEvt, pCursorId, pcPackets, pcbPacket, pPackets));

CLEANUP:
    return SUCCEEDED(hr) && !fShutdown;
}

///////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI GetPenEventMultiple(
    INT cCommHandles, __typefix(CPimcContext **) __in_ecount(cCommHandles) INT_PTR * pCommHandles,
    __typefix(HANDLE) __in_opt INT_PTR commHandleReset,
    __out INT * piEvt,
    __out INT * pEvt, __out INT * pCursorId,
    __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets)
{
    DHR;
    BOOL fShutdown = TRUE;

    CHR (((cCommHandles == 0 && commHandleReset) ||
          (cCommHandles && pCommHandles && commHandleReset &&
            piEvt && pEvt && pCursorId && pcPackets && pcbPacket && pPackets)) ?
            S_OK : E_INVALIDARG);

    CHR(CPimcContext::GetPenEventMultiple(
        cCommHandles, (CPimcContext **)pCommHandles, 
        (HANDLE) commHandleReset,
        &fShutdown,
        piEvt,
        pEvt, pCursorId, 
        pcPackets, pcbPacket, pPackets));

CLEANUP:
    return SUCCEEDED(hr) && !fShutdown;
}

///////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI GetLastSystemEventData(
    __typefix(CPimcContext *) __in INT_PTR commHandle,
    __out INT * piEvent, __out INT * piModifier, __out INT * piKey, 
    __out INT * piX, __out INT * piY, __out INT * piCursorMode, __out INT * piButtonState)
{
    CPimcContext * pCtx = nullptr;
    DHR;
    CHR(piEvent && piModifier && piKey && piX && piY && piCursorMode && piButtonState ? S_OK : E_INVALIDARG);
    pCtx = (CPimcContext *)commHandle;
    CHR(pCtx->GetLastSystemEventData(piEvent, piModifier, piKey, piX, piY, piCursorMode, piButtonState));
CLEANUP:
    return SUCCEEDED(hr);
}

///////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI CreateResetEvent(__out INT_PTR * pCommHandleReset)
{
    HANDLE hEventReset = nullptr;
    DHR;
    CHR (pCommHandleReset ? S_OK : E_INVALIDARG);
    hEventReset = CreateEvent(NULL, FALSE, FALSE, NULL);
    CHR(hEventReset ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_CANNOTCREATERESETEVENT));
    *pCommHandleReset = (INT_PTR)hEventReset;
CLEANUP:
    return SUCCEEDED(hr);
}

///////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI DestroyResetEvent(__typefix(HANDLE) __in INT_PTR commHandleReset)
{
    HANDLE hEventReset = nullptr;
    DHR;
    CHR (commHandleReset ? S_OK : E_INVALIDARG);
    hEventReset = (HANDLE)commHandleReset;
    CloseHandle(hEventReset);
CLEANUP:
    return SUCCEEDED(hr);
}

///////////////////////////////////////////////////////////////////////////////

extern "C" BOOL WINAPI RaiseResetEvent(__typefix(HANDLE) __in INT_PTR commHandleReset)
{
    HANDLE hEventReset = nullptr;
    DHR;
    CHR (commHandleReset ? S_OK : E_INVALIDARG);
    hEventReset = (HANDLE)commHandleReset;
    SetEvent(hEventReset);
CLEANUP:
    return SUCCEEDED(hr);
}

