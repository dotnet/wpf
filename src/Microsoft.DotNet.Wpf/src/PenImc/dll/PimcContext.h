// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcContext.h : Declaration of the CPimcContext

#pragma once
#include "resource.h"       // main symbols

#include "PenImc.h"
#include "PimcManager.h"
#include "ComLockableWrapper.hpp"
#include "GitComLockableWrapper.hpp"

/////////////////////////////////////////////////////////////////////////////
// CPimcContext

class ATL_NO_VTABLE CPimcContext : 
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<CPimcContext, &CLSID_PimcContext3>,
    public IPimcContext3
{
public:

    /////////////////////////////////////////////////////////////////////////

    CPimcContext();
    HRESULT FinalConstruct() { return S_OK; } ;
    void    FinalRelease();
    HRESULT Init(__inout  CComPtr<CPimcManager>       pMgr,
                 __in_opt CComPtr<ITabletContext>     pCtxS,
                 __in HWND                     hwnd,
                 TABLET_CONTEXT_ID             tcid,
                 PACKET_DESCRIPTION *          pPacketDescription);

    HRESULT InitUnnamedCommunications(__in CComPtr<ITabletContextP> pCtxP);
    HRESULT InitNamedCommunications(__in CComPtr<ITabletContextP> pCtxP);
    HRESULT InitCommunicationsCore();

    void    ShutdownSharedMemoryCommunications();

    HRESULT GetPenEvent (__in_opt HANDLE hEventReset, __out BOOL * pfShutdown, __out INT * pEvt, __out INT * pCursorId, __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets);
    HRESULT GetPenEventCore (DWORD dwWait, __out BOOL * pfWaitAgain, __out BOOL * pfShutdown, __out INT * pEvt, __out INT * pCursorId, __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets);

    static HRESULT GetPenEventMultiple(
                INT cCtxs, __in_ecount(cCtxs) CPimcContext ** ppCtxs,
                __in_opt HANDLE hEventAbort,
                __out BOOL * pfShutdown, 
                __out INT * piCtxEvt,
                __out INT * pEvt, __out INT * pCursorId, 
                __out INT * pcPackets, __out INT * pcbPacket, __out INT_PTR * pPackets);

    STDMETHOD(ShutdownComm)();
    STDMETHOD(GetPacketDescriptionInfo)(__out INT * pcProps, __out INT * pcButtons);
    STDMETHOD(GetPacketPropertyInfo)(INT iProp, __out GUID * pGuid, __out INT * piMin, __out INT * piMax, __out INT * piUnits, __out FLOAT *pflResolution);
    STDMETHOD(GetPacketPropertyInfoImpl)(INT iProp, __out GUID * pGuid, __out INT * piMin, __out INT * piMax, __out INT * piUnits, __out FLOAT *pflResolution);
    STDMETHOD(GetPacketButtonInfo)(INT iButton, __out GUID * pGuid);
    STDMETHOD(GetLastSystemEventData)(__out INT * piEvent, __out INT * piModifier, __out INT * piKey, __out INT * piX, __out INT * piY, __out INT * piCursorMode, __out INT * piButtonState);

    HRESULT GetCommHandle(__out INT64* pHandle);
    HRESULT GetKey(__out INT * pKey);
    HRESULT SetSingleFireTimeout(UINT uiTimeout);
    HRESULT EnsureHandlesArray(INT cHandles);
    HRESULT EnsurePackets(DWORD cb);
    static void DestroyPacketDescription(__in_opt PACKET_DESCRIPTION * pPacketDescription);

#ifdef DELIVERY_PROFILING
    void ProfilePackets(BOOL fDown, BOOL fUp, int x, int y);
#endif

    const static DWORD UPDATE_SizeMove        = 0x01;
    const static DWORD UPDATE_SendToTop       = 0x02;
    const static DWORD UPDATE_SendToBack      = 0x04;
    const static DWORD UPDATE_Enable          = 0x08;
    const static DWORD UPDATE_Disable         = 0x10;

    HRESULT PostUpdate(DWORD update);
    HRESULT ExecuteUpdates();

    /////////////////////////////////////////////////////////////////////////

BEGIN_COM_MAP(CPimcContext)
    COM_INTERFACE_ENTRY(IPimcContext3)
END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT()

    /////////////////////////////////////////////////////////////////////////

    class CEventSink : public ITabletEventSink
    {
    public:

        // DDVSO:514949
        // The lifetime of this object needs to be correctly tracked via
        // its IUnknown implementation as this will be passed onto WISP
        // when a WISP context is created.  WISP stores this in a CComPtr
        // member variable and this object must be alive when the WISP
        // context accesses it, even if the enclosing CPimcContext is
        // already destroyed.
        CEventSink() : m_cRef(0)
        {
        }

        // IUnknown
        STDMETHOD(QueryInterface)(REFIID riid, __typefix(ITabletEventSink **) __deref_out_opt void** ppv)
        {
            DHR;
            if (IsEqualGUID(riid, IID_IUnknown) ||
                IsEqualGUID(riid, IID_ITabletEventSink))
            {
                *ppv = (ITabletEventSink*)this;
                AddRef();
                hr = S_OK;
            }
            else
            {
                *ppv = NULL;
                hr = E_NOINTERFACE;
            }
            RHR;
        }

        STDMETHOD_(ULONG, AddRef) ()
        {
            LONG newRefCount = InterlockedIncrement(&m_cRef);
            return static_cast<ULONG>(newRefCount);
        }

        STDMETHOD_(ULONG, Release)()
        {
            LONG newRefCount = InterlockedDecrement(&m_cRef);

            // We should fail immediately if the ref count is ever below 0.
            // We want to know, even in production, if we have any unbalanced releases.
            ATLASSERT(newRefCount >= 0);

            if (newRefCount == 0)
            {
                delete this;
            }

            return static_cast<ULONG>(newRefCount);
        }

        LONG m_cRef;

        // ITabletEventSink
        STDMETHOD(ContextCreate)    (TABLET_CONTEXT_ID tcid) { return S_OK; }
        STDMETHOD(ContextDestroy)   (TABLET_CONTEXT_ID tcid) { return S_OK; }
        STDMETHOD(CursorNew)        (TABLET_CONTEXT_ID tcid, CURSOR_ID cid) { return S_OK; }
        STDMETHOD(CursorInRange)    (TABLET_CONTEXT_ID tcid, CURSOR_ID cid) { return S_OK; }
        STDMETHOD(CursorOutOfRange) (TABLET_CONTEXT_ID tcid, CURSOR_ID cid) { return S_OK; }
        STDMETHOD(CursorMove)       (TABLET_CONTEXT_ID tcid, CURSOR_ID cid, HWND hWnd, LONG xPos, LONG yPos) { return S_OK; }
        STDMETHOD(CursorDown)       (TABLET_CONTEXT_ID tcid, CURSOR_ID cid, ULONG nSerialNumber, ULONG cbPkt, BYTE * pbPkt) { return S_OK; }
        STDMETHOD(CursorUp)         (TABLET_CONTEXT_ID tcid, CURSOR_ID cid, ULONG nSerialNumber, ULONG cbPkt, BYTE * pbPkt) { return S_OK; }
        STDMETHOD(Packets)          (TABLET_CONTEXT_ID tcid, ULONG cPkts, ULONG cbPkts, BYTE * pbPkts, ULONG * pnSerialNumbers, CURSOR_ID cid) { return S_OK; }
        STDMETHOD(SystemEvent)      (TABLET_CONTEXT_ID tcid, CURSOR_ID cid, SYSTEM_EVENT, SYSTEM_EVENT_DATA) { return S_OK; }
    };
        
    CComPtr<CEventSink> m_sink;


    /////////////////////////////////////////////////////////////////////////

    // data

    CComPtr<CPimcManager>       m_pMgr;
    CComPtr<ITabletContextP>    m_pCtxS;
    TABLET_CONTEXT_ID           m_tcid;
    PACKET_DESCRIPTION *        m_pPacketDescription;

    HANDLE                      m_hEventMoreData;
    HANDLE                      m_hEventClientReady;
    HANDLE                      m_hMutexSharedMemory;
    HANDLE                      m_hFileMappingSharedMemory;
    SHAREDMEMORY_HEADER *       m_pSharedMemoryHeader;
    BYTE *                      m_pbSharedMemoryRawData;
    BYTE *                      m_pbSharedMemoryPackets;
    BOOL                        m_fCommHandleOutstanding;
    INT                         m_cHandles;
    HANDLE *                    m_pHandles;
    DWORD                       m_cbPackets;
    BYTE*                       m_pbPackets;
    SYSTEM_EVENT                m_sysEvt;
    SYSTEM_EVENT_DATA           m_sysEvtData;

    HookThreadItemKey           m_keyHookThreadItem;
    CPimcManager::HookWindowItemKey           m_keyHookWindowItem;

    HANDLE                      m_hEventUpdate;
    DWORD                       m_dwUpdatesPending;
    CRITICAL_SECTION            m_csUpdates;

    BOOL                        m_fSingleFireTimeout : 1;
    BOOL                        m_fIsTopmostHook : 1;
    DWORD                       m_dwSingleFireTimeout;

    ComUtils::ComLockableWrapper m_contextLock;
    ComUtils::ComLockableWrapper m_sinkLock;
    ComUtils::GitComLockableWrapper<ITabletContextP> m_wispContextLock;
    
    // DDVSO:514949
    // Special param flag for COM operations in GetPacketPropertyInfo.
    static const int            QUERY_WISP_CONTEXT_KEY = -1;
};

