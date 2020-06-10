// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcManager.h : Declaration of the CPimcManager

#pragma once
#include "resource.h"       // main symbols

#include "PenImc.h"
#include "PbList.h"
#include "PbPreallocArray.h"
#include "ComLockableWrapper.hpp"
#include "GitComLockableWrapper.hpp"

class CPimcContext;
class CPimcManager; 


// thread map
struct CHookThreadItem
{
    DWORD           m_dwThreadId;
    HHOOK           m_hHook;
    DWORD           m_cUsages;
    CPbPreallocArray<CPimcManager*,2>  m_mgrs;

    // Used to manage the hook thread
    HANDLE          m_hHookThread;
    HANDLE          m_hEventHookThreadReady; 
    HANDLE          m_hEventHookThreadExit;
    HANDLE          m_hEventHookThreadExitAck;
    HANDLE          m_hTimer;
    BOOL            m_bTimerStarted : 1;
};
typedef PBLKEY  HookThreadItemKey;

extern HANDLE  g_hMutexHook;

#ifdef DBG_LATER
extern DWORD   g_dwMutexHookOwnerThreadId;
extern BOOL    g_cHookLock;
#endif

#define WAITTIMER_DELAY         2500000 // 250 milliseconds (1/4 sec)

/////////////////////////////////////////////////////////////////////////////
// CPimcManager

class ATL_NO_VTABLE CPimcManager : 
    public CComObjectRootEx<CComSingleThreadModel>,
    public CComCoClass<CPimcManager, &CLSID_PimcManager3>,
    public IPimcManager3
{
public:

    /////////////////////////////////////////////////////////////////////////

    CPimcManager();

    HRESULT FinalConstruct();
    void    FinalRelease();
    void    LoadWisptis();
    BOOL    IsVistaOrGreater();

protected:

    BOOL    ShouldLoadWisptis();
    BOOL    UserIsLocalSystem();
    
public:
    
#if WANT_PROFILE
    BOOL    IsProfiling();
#endif

    HRESULT RegisterCtxS(__in ITabletContextP * pCtxS, __out DWORD * pdwCookie);
    HRESULT RevokeCtxS  (DWORD dwCookie);
    HRESULT GetCtxS     (DWORD dwCookie, __deref_out ITabletContextP ** ppCtxS);

    //
    // hook handling
    //

    // methods

    HRESULT InstallWindowHook(__in HWND hwnd, __inout CPimcContext * pCtx);
    HRESULT UninstallWindowHook(__in CPimcContext * pCtx);
    static DWORD WINAPI HookThreadProc(__typefix(CHookThreadItem *) __in LPVOID pvParam);
    static void CALLBACK InstallWindowHookApcCore(__typefix(CAsyncData *) __inout ULONG_PTR pvAsyncData);
    static HRESULT InitializeHookThread(__inout CHookThreadItem * pThread);
    static void TerminateHookThread(__inout CHookThreadItem * pThread);

    typedef PBLKEY  HookWindowItemKey;

    struct CHookWindowItem
    {
        HWND        m_hwnd;
        BOOL        m_bNeedsTimer;
        RECT        m_rc;
        CPbPreallocArray<CPimcContext*,2>  m_ctxs;
    };

    // tracks windows for this manager thread
    CPbList<CHookWindowItem>        m_HookWindowMap;

    static HookThreadItemKey FindHookThreadItem(DWORD dwThreadId);
    HRESULT           EnsureHookThreadItem(DWORD dwThreadId, __in CPimcManager * pMgr, __out HookThreadItemKey * pKey, __out BOOL *pfAddedManager);
    HookWindowItemKey FindHookWindowItem(__in HWND hwnd);
    HRESULT           EnsureHookWindowItem(__in HWND hwnd, __out HookWindowItemKey * pKey);

    static LRESULT CALLBACK HookProc(int nCode, WPARAM wParam, LPARAM lParam);

    static void HandleTimer(DWORD dwThreadId);
    static void StartWaitTimer(__inout CHookThreadItem * pThread);
    static void StopWaitTimerIfNotNeeded(__inout CHookThreadItem * pThread);
    static BOOL DoContextsNeedWaitableTimer(__in CHookThreadItem * pThread);
    static void MgrHandleCtxUpdate(DWORD dwThreadId, DWORD dwUpdate, __in HWND hwnd);
    static void PostCtxUpdateForSubtree(DWORD dwUpdate, __in HWND hwndRoot, __in CHookThreadItem * pThreadItem);
    static void PostCtxUpdateForWnd(DWORD dwUpdate, __in HWND hwnd, __in CHookThreadItem * pThreadItem);

    // locking

    class CHookLock
    {
    public:
        CHookLock()
        {
            m_dwWait = 0;
            ASSERT(g_hMutexHook);
            if (g_hMutexHook)
            {
                m_dwWait = WaitForSingleObject(g_hMutexHook, INFINITE);
                ASSERT (m_dwWait == WAIT_OBJECT_0);
            }
#ifdef DBG_LATER
            g_cHookLock++;
            g_dwMutexHookOwnerThreadId = GetCurrentThreadId();
#endif
        }
        ~CHookLock()
        {
#ifdef DBG_LATER
            g_dwMutexHookOwnerThreadId = 0;
            g_cHookLock--;
#endif
            if (m_dwWait == WAIT_OBJECT_0)
                ReleaseMutex(g_hMutexHook);
        }

    protected:
        DWORD           m_dwWait;
    };

    //
    // IPimcManager3
    //

    STDMETHOD(GetTabletCount)(__out ULONG* pcTablets);
    STDMETHOD(GetTablet)(ULONG iTablet, __deref_out IPimcTablet3** ppTablet);
    STDMETHOD(GetTabletImpl)(ULONG iTablet, __deref_out IPimcTablet3** ppTablet);

    // wiring

DECLARE_REGISTRY_RESOURCEID(IDR_PIMCMANAGER)

BEGIN_COM_MAP(CPimcManager)
    COM_INTERFACE_ENTRY(IPimcManager3)
END_COM_MAP()

    DECLARE_PROTECT_FINAL_CONSTRUCT()

    /////////////////////////////////////////////////////////////////////////

    // data
    CComPtr<ITabletManager>         m_pMgrS;
    ComUtils::GitComLockableWrapper<ITabletManager> m_wispManagerLock;

    BOOL                            m_fLoadedWisptis : 1;

    ComUtils::ComLockableWrapper m_managerLock;

    // DDVSO:514949
    // Special param flag for COM operations in GetTablet
    const static ULONG              RELEASE_MANAGER_EXT = 0xFFFFDEAD;

#if WANT_PROFILE
    BOOL                            m_fIsProfilingCached : 1;
    BOOL                            m_fIsProfiling       : 1;
#endif // WANT_PROFILE	
};

/////////////////////////////////////////////////////////////////////////////

OBJECT_ENTRY_AUTO(__uuidof(PimcManager3), CPimcManager)

