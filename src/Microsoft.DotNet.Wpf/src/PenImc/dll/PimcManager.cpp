// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// PimcManager.cpp : Implementation of CPimcManager

#include "stdafx.h"
#include "OSVersionHelper.h"
#include "PimcContext.h"
#include "PimcTablet.h"
#include "PimcManager.h"
#include "Penimc.h"
#include "shellapi.h"
#include <strsafe.h>
#include "osversionhelper.h"

using namespace ComUtils;

// from drivers/tablet/include/tabinc.h:
#define PENPROCESS_WISPTIS_REQUEST_EVENT    _T("{773F1B9A-35B9-4E95-83A0-A210F2DE3B37}-request")
#define PENPROCESS_WISPTIS_RUNNING_EVENT    _T("{773F1B9A-35B9-4E95-83A0-A210F2DE3B37}-running")
// Our local define for how long we'll wait for Tablet Input Service to load wisptis.
#define PENPROCESS_WISPTIS_LOADING_TIMEOUT 30000 // 30 seconds
#define WISPTIS_DIR              _T("%SystemRoot%\\SYSTEM32\\")
#define WISPTIS_NAME             _T("WISPTIS.EXE")
#define WISPTIS_MANUAL_LAUNCH   _T("/ManualLaunch;")

#define KERNEL32_NAME           _T("KERNEL32")
#define WOW64DISABLEWOW64FSREDIRECTION_NAME "Wow64DisableWow64FsRedirection"
#define WOW64REVERTWOW64FSREDIRECTION_NAME  "Wow64RevertWow64FsRedirection"
typedef BOOL (WINAPI *LPFNWOW64DISABLEWOW64FSREDIRECTION) (PVOID*);
typedef BOOL (WINAPI *LPFNWOW64REVERTWOW64FSREDIRECTION) (PVOID);


/////////////////////////////////////////////////////////////////////////////

class CAsyncData
{
public:
    CAsyncData(DWORD dwArg, BOOL fArg = FALSE, BOOL fEventAck = FALSE)
        : m_dwArg(dwArg), m_fArg(fArg)
    {
        m_hEventAck = fEventAck ? CreateEvent(NULL, FALSE, FALSE, NULL) : NULL;
    }
    ~CAsyncData()
    {
        if (m_hEventAck)
        {
            CloseHandle(m_hEventAck);
        }
    }
    void SignalAck()
    {
        if (m_hEventAck)
            SetEvent(m_hEventAck);
    }
    void WaitAck()
    {
        if (m_hEventAck)
        {
            WaitForSingleObject(m_hEventAck, INFINITE);
        }
    }

    HANDLE      m_hEventAck;
    DWORD_PTR   m_dwArg;
    BOOL        m_fArg;
    DWORD_PTR   m_dwRes;
};

/////////////////////////////////////////////////////////////////////////////
// CPimcManager

/////////////////////////////////////////////////////////////////////////////

// Store the thread map globally so we can look up the manager given 
// a window in the HookProc since we don't have access to an instance of the
// CPimcManager at that time.
CPbList<CHookThreadItem>    g_HookThreadMap;

HANDLE  g_hMutexHook = NULL;

#ifdef DBG_LATER
DWORD   g_dwMutexHookOwnerThreadId = 0;
BOOL    g_cHookLock = 0;
#endif

CPimcManager::CPimcManager() :
#if WANT_PROFILE
    m_fIsProfilingCached(FALSE),
#endif
    m_fLoadedWisptis(FALSE)
{
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcManager::FinalConstruct()
{
    DHR;

    // DDVSO:514949
    // Calling this ensures that the CStdIdentity for this IPimcManager3 is
    // not released if we hit a COM rundown due to OSGVSO:10779198.
    m_managerLock = ComLockableWrapper(this, ComApartmentVerifier::CurrentSta());
    CHR(m_managerLock.Lock());

    // Verify the mutex we created in DllLoad went OK.
    CHR(g_hMutexHook ? S_OK : E_FAIL);

CLEANUP:    
    RHR;
}

/////////////////////////////////////////////////////////////////////////////

void CPimcManager::LoadWisptis()
{
    DHR;

    if (!m_fLoadedWisptis)
    {
        // **********
        // NOTE:    PenIMC has duplicated the code for loading wisptis from InkObj.
        //          Whenever WIC team makes any changes, we should coordinate with them to work on fixes.
        // **********
        if (IsVistaOrGreater())
        {
            // DDVSO 144719. There are some scenarios were we must skip loading wisptis since 
            // they are not supported and can cause delays or crashes.
            if (ShouldLoadWisptis())
            {
                // we do this to signal TabSvc that it needs to spin up wisptis
                //  so that it is at the right IL.
                HANDLE hEventRequest = OpenEvent(EVENT_MODIFY_STATE, FALSE, PENPROCESS_WISPTIS_REQUEST_EVENT);
                HANDLE hEventRunning = OpenEvent(SYNCHRONIZE, FALSE, PENPROCESS_WISPTIS_RUNNING_EVENT);

                //if we don't have the event (TabSvc isn't running), or we timed out,
                // that means Wisptis isn't running, so we'll start it; we do this via
                // ShellExecute so that it gets started at high-IL (as indicated by
                // Wisptis's manifest) to avoid IL-mismatch issues
                //we allow wisptis to be started without TabSvc for backcompat
            
                if(hEventRunning == NULL)
                {
                   // create the event since TabSvc isn't running
                   hEventRunning = CreateEvent(NULL, TRUE, FALSE, PENPROCESS_WISPTIS_RUNNING_EVENT);
                }

                if(hEventRequest != NULL && hEventRunning != NULL)
                {
                    //when this wait returns, wisptis will have registered its classes with COM
                    //if this fails or times out, we'll risk starting wisptis at a mismatched IL
                    DWORD dwResult = SignalObjectAndWait(hEventRequest, hEventRunning, 30000 /* thirty seconds */, FALSE);

                    hr = dwResult == WAIT_OBJECT_0 ? S_OK : E_FAIL;
                }

                // DDVSO:398137
                // Since hEventRequest is no longer of use at this point, close the handle.
                SafeCloseHandle(&hEventRequest);

                if(/* wait timed out */ FAILED(hr) ||
                   /* couldn't open the event for some reason */ hEventRunning == NULL ||
                   /* wisptis isn't already running */ WaitForSingleObject(hEventRunning, 0) == WAIT_TIMEOUT)
                {
                    PVOID pvOldValue = NULL;
                    BOOL bIsWow64 = FALSE;
                    LPFNWOW64DISABLEWOW64FSREDIRECTION fnWow64DisableWow64FsRedirection = NULL;
                    LPFNWOW64REVERTWOW64FSREDIRECTION fnWow64RevertWow64FsRedirection = NULL;
                    HMODULE hKernel32 = NULL;

                    // Check whether this is running under Wow64 and, if so, disable file system redirection
                    // on the current thread - otherwise it will look for wisptis in the syswow64 directory
                    // instead of system32.
                    TPDBG_VERIFY(IsWow64Process(GetCurrentProcess(),&bIsWow64));
                    if (bIsWow64)
                    {
                        // NOTICE-2006/06/13-WAYNEZEN,
                        // Since penimc may also run on the top of XPSP2, We cannot call Wow64DisableWow64FsRedirection/Wow64RevertWow64FsRedirection
                        // directly. Otherwise it will cause Entry Point Not Found error even though we don't really on those functions on 32-bit XP.
                        // So we have to use GetProcAddress to resovle the function address dynamically.
                        hKernel32 = GetModuleHandle(KERNEL32_NAME);
                        fnWow64DisableWow64FsRedirection = (LPFNWOW64DISABLEWOW64FSREDIRECTION)GetProcAddress(
                                                                hKernel32, WOW64DISABLEWOW64FSREDIRECTION_NAME);
                        fnWow64RevertWow64FsRedirection = (LPFNWOW64REVERTWOW64FSREDIRECTION)GetProcAddress(
                                                                hKernel32, WOW64REVERTWOW64FSREDIRECTION_NAME);

                        TPDBG_VERIFY(fnWow64DisableWow64FsRedirection(&pvOldValue));
                    }

                    SHELLEXECUTEINFO sei = {0};

                    sei.cbSize = sizeof(sei);
                    sei.lpFile = WISPTIS_DIR WISPTIS_NAME;
                    sei.lpParameters = WISPTIS_MANUAL_LAUNCH;
                    sei.lpVerb = NULL;
                    sei.fMask = SEE_MASK_FLAG_DDEWAIT | SEE_MASK_DOENVSUBST | SEE_MASK_FLAG_NO_UI;
                    sei.lpDirectory = WISPTIS_DIR;
                    sei.hInstApp = (HINSTANCE)0;

                    BOOL bResult = ShellExecuteEx(&sei);

                    // Restore the file system redirection settings.
                    if (bIsWow64)
                    { 
                        TPDBG_VERIFY(fnWow64RevertWow64FsRedirection(pvOldValue));
                    }

                    hr = bResult ? S_OK : E_FAIL;
                    if(FAILED(hr))
                    {
                       OutputDebugString(L"PimcManager::LoadWisptis failed to ShellExecuteEx.\r\n");
                    }
                }
                   
                if(SUCCEEDED(hr) && hEventRunning != NULL)
                {
                    (void)WaitForSingleObject(hEventRunning, PENPROCESS_WISPTIS_LOADING_TIMEOUT /* 30 seconds */);
                    //regardless of the return from this, we'll still try to spin wisptis up via COM
                }
                
                SafeCloseHandle(&hEventRunning);

                if(SUCCEEDED(hr))
                {
                   CHR(m_pMgrS.CoCreateInstance(CLSID_TabletManagerS)); //, NULL, CLSCTX_INPROC_SERVER | CLSCTX_LOCAL_SERVER));

                   // Ensure the WISP tablet manager is added to the GIT.
                   m_wispManagerLock = GitComLockableWrapper<ITabletManager>(m_pMgrS, ComApartmentVerifier::Mta());
                   CHR(m_wispManagerLock.CheckCookie());

                   m_fLoadedWisptis = TRUE;
                }
            }
        }
        else
        {
            // To get around the issue with spinning up two wisptis.exe instances per user session we create an
            // object that is a local server (using DllHost.exe to host one of our objects out of proc) that is
            // marked as RunAs="Interactive User" to make sure it gets launched with the user's full priveledges.
            // We then CoCreateInstance the wisptis.exe object from there to ensure we don't spin up an extra instance
            // of wisptis.exe.  The PimcSurrogate object is implemented in penimc.dll.
            CComPtr<IPimcSurrogate3>  pSurrogate;
            CComPtr<IUnknown> pTabletManager;
            CHR(pSurrogate.CoCreateInstance(CLSID_PimcSurrogate3, NULL, CLSCTX_LOCAL_SERVER));
            CHR(pSurrogate != NULL ? S_OK : E_UNEXPECTED);
            CHR(pSurrogate->GetWisptisITabletManager(&pTabletManager));
            CHR(pTabletManager->QueryInterface(IID_ITabletManager, (void**)&m_pMgrS));
            m_fLoadedWisptis = TRUE;
        }
    }
    
CLEANUP:
    // No return code needed.
    return;
}

/////////////////////////////////////////////////////////////////////////////

BOOL CPimcManager::IsVistaOrGreater()
{
    static bool bIsVistaOrGreater = WPFUtils::OSVersionHelper::IsWindowsVistaOrGreater();
    return bIsVistaOrGreater;
}

/////////////////////////////////////////////////////////////////////////////

BOOL CPimcManager::ShouldLoadWisptis()
{
    // DDVSO 144719. Wisptis(Vista & 7) doesn't support inking while running under the system account
    // Wisp (Win8 and above) supports this scenario, so we check for OS version and then for system account
    static bool bShouldLoadWisptis = WPFUtils::OSVersionHelper::IsWindows8OrGreater() ||
                                     !UserIsLocalSystem();
    return bShouldLoadWisptis;
}

/////////////////////////////////////////////////////////////////////////////

BOOL CPimcManager::UserIsLocalSystem()
{
    BOOL fLocalSystem = FALSE;

    HANDLE hProcess = GetCurrentProcess();
    HANDLE hToken;
    if (OpenProcessToken(hProcess, TOKEN_QUERY, &hToken))
    {
        DWORD retLength = 0;
        GetTokenInformation(hToken, TokenUser, nullptr, 0, &retLength);
        if (retLength)
        {
            BYTE* tUser = new (std::nothrow) BYTE[retLength];
            if (tUser)
            {
                DWORD dwRealLength = retLength;
                if(GetTokenInformation(hToken, TokenUser, tUser, dwRealLength, &retLength))
                {
                    PSID SIDSystem;
                    SID_IDENTIFIER_AUTHORITY siaNT = SECURITY_NT_AUTHORITY;
                    if(AllocateAndInitializeSid(&siaNT, 1, SECURITY_LOCAL_SYSTEM_RID,
                                                 0, 0, 0, 0, 0, 0, 0, &SIDSystem))
                    {
                        fLocalSystem = EqualSid(((TOKEN_USER*)tUser)->User.Sid, SIDSystem);
                        FreeSid(SIDSystem);
                    }
                }
                delete [] tUser;
            }
        }
        CloseHandle(hToken);
    }

    return fLocalSystem;
}

/////////////////////////////////////////////////////////////////////////////

HRESULT CPimcManager::InitializeHookThread(__inout CHookThreadItem * pThread)
{
    DHR;

    BOOL fCleanupThread = FALSE;
    CAsyncData *    pAsyncData = NULL;

    DWORD dwHookThread = 0;
    DWORD dwWaitHookThread = WAIT_FAILED;

    // Only need to do this once.
    ASSERT(!pThread->m_hHook);
    
    // hook handling
    pThread->m_hEventHookThreadReady   = CreateEvent(NULL, FALSE, FALSE, NULL);
    pThread->m_hEventHookThreadExit    = CreateEvent(NULL, FALSE, FALSE, NULL);
    pThread->m_hEventHookThreadExitAck = CreateEvent(NULL, FALSE, FALSE, NULL);
    // timer to deal with hosting in other processes (don't get move event)
    pThread->m_hTimer = CreateWaitableTimer(NULL, TRUE, NULL); // last param make this Waitable Timer

    CHR((pThread->m_hEventHookThreadReady != NULL && pThread->m_hEventHookThreadExit != NULL && 
          pThread->m_hEventHookThreadExitAck != NULL && pThread->m_hTimer != NULL) ? S_OK : E_FAIL);

    pThread->m_hHookThread = CreateThread(NULL, 0, HookThreadProc, (LPVOID)pThread, 0, &dwHookThread);
    CHR(pThread->m_hHookThread ? S_OK : E_FAIL);

    dwWaitHookThread = WaitForSingleObject(pThread->m_hEventHookThreadReady, INFINITE);
    CHR(dwWaitHookThread == WAIT_OBJECT_0 ? S_OK : E_FAIL);
    fCleanupThread = true;

    // post the APC call
    CHR_MEMALLOC(pAsyncData = new CAsyncData(/*dwArg*/pThread->m_dwThreadId, /*fArg*/FALSE, /*fEventDone*/TRUE));
    CHR(QueueUserAPC(InstallWindowHookApcCore, pThread->m_hHookThread, (ULONG_PTR)pAsyncData) ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_QUEUEUSERAPC_CALL));
    pAsyncData->WaitAck();
    pThread->m_hHook = (HHOOK)pAsyncData->m_dwRes;
    delete pAsyncData;
    pAsyncData = NULL;

    RHR;
    
CLEANUP:
    if (fCleanupThread)
    {
        SignalObjectAndWait(pThread->m_hEventHookThreadExit, pThread->m_hEventHookThreadExitAck, INFINITE, FALSE);
    }
    if (pAsyncData)
    {
        delete pAsyncData;    
    }
    SafeCloseHandle(&pThread->m_hHookThread);
    SafeCloseHandle(&pThread->m_hEventHookThreadReady);
    SafeCloseHandle(&pThread->m_hEventHookThreadExit);
    SafeCloseHandle(&pThread->m_hEventHookThreadExitAck);
    SafeCloseHandle(&pThread->m_hTimer);

    RHR;
}    

void CPimcManager::TerminateHookThread(__inout CHookThreadItem * pThread)
{
    // Only do this once.
    if (pThread->m_hHook != NULL)
    {
        UnhookWindowsHookEx(pThread->m_hHook);
        SignalObjectAndWait(pThread->m_hEventHookThreadExit, pThread->m_hEventHookThreadExitAck, INFINITE, FALSE);
        pThread->m_hHook = NULL;
        SafeCloseHandle(&pThread->m_hHookThread);
        SafeCloseHandle(&pThread->m_hEventHookThreadReady);
        SafeCloseHandle(&pThread->m_hEventHookThreadExit);
        SafeCloseHandle(&pThread->m_hEventHookThreadExitAck);
        SafeCloseHandle(&pThread->m_hTimer);
    }
}


/////////////////////////////////////////////////////////////////////////////

void CPimcManager::FinalRelease() 
{
    m_wispManagerLock.RevokeIfValid();
}

/////////////////////////////////////////////////////////////////////////////
//                                          
// CPimcManager::HookThreadProc
// 
// This thread is used to install hooks for contexts. The thread is alertable for APCs and
// the actual installation of the hook happens in InstallWindowHookApcCore.
//
// IMPORTANT NOTE (alexz): there was a significant amount of investigation done about 
// what is the correct logic to maintain hook on a window when done in COM in-proc servers.
// See Tablet V1 Raid bugs # 17589, 23860 for details.
// In particular, note that we can not install the hooks from the thread that invokes
// CPimcContext. This is because the thread used is from the thread pool (either CLR or COM RPC),
// and can be switched at any moment. When the switch happens, Windows disconnects the hook.
//

DWORD CPimcManager::HookThreadProc(__typefix(CHookThreadItem *) __in LPVOID pvParam)
{
    DHR;
    CHookThreadItem * pThread = (CHookThreadItem*)pvParam;
    ASSERT (pThread);

    CHR(SetEvent(pThread->m_hEventHookThreadReady) ? S_OK : E_FAIL);

    // MAIN LOOP
    {
        BOOL fLoop = TRUE;
        HANDLE waitHandles[2] = { pThread->m_hEventHookThreadExit, pThread->m_hTimer };
        while (fLoop)
        {
            DWORD dwWait = MsgWaitForMultipleObjectsEx(
                2, &(waitHandles[0]), INFINITE,
                QS_ALLEVENTS, MWMO_ALERTABLE);

            switch (dwWait)
            {
            case WAIT_OBJECT_0 + 0: // m_hEventHookThreadExit
                fLoop = FALSE;
                break;

            case WAIT_OBJECT_0 + 1: // waitable timer triggered
                // See if any of our contexts have changed location
                HandleTimer(pThread->m_dwThreadId);
                fLoop = TRUE;
                break;

            case WAIT_OBJECT_0 + 2: // a message in the queue of this thread
            {
                MSG msg;
                PeekMessage(&msg, 0, 0, 0, PM_NOREMOVE); // this will cause hook proc to get invoked
            }
            fLoop = TRUE;
            break;

            case WAIT_IO_COMPLETION: // (an APC call will trigger this)
                fLoop = TRUE;
                break;

            default:
                ASSERT(FALSE && "CPimcManager::HookThreadProc: an unexpected error in the wait");
                fLoop = FALSE;
                break;
            }
        }
    }

CLEANUP:
    SetEvent(pThread->m_hEventHookThreadExitAck);

    return 0;
}

///////////////////////////////////////////////////////////////////////////////

// A new PimcContext is created, make sure we have a hook set up.
HRESULT CPimcManager::InstallWindowHook(__in HWND hwnd, __inout CPimcContext * pCtx)
{
    DHR;
    DWORD           dwProcessId;
    CAsyncData *    pAsyncData = NULL;
    BOOL fCleanupThreadItem = false;
    BOOL fCleanupHook = false;
    BOOL fCleanupWindowItem = false;
    BOOL fAddedMgr = FALSE;

	HookThreadItemKey   keyHookThreadItem = NULL;
    CHookThreadItem *   pHookThreadItem = NULL;
    HookWindowItemKey   keyHookWindowItem = NULL;
    CHookWindowItem *   pHookWindowItem = NULL;
    
    ASSERT (hwnd && IsWindow(hwnd));

    // DDVSO:220285
    // Scope the CHookLock so we don't attempt to call TerminateWindowHook
    // under the lock (see UninstallWindowHook).
    {
        CHookLock lock;

        // we don't allow handling of hwnd-s not owned by this process
        DWORD dwThreadId = GetWindowThreadProcessId(hwnd, &dwProcessId);
        DWORD dwProcessIdCur = GetCurrentProcessId();
        CHR(dwProcessIdCur == dwProcessId ? S_OK : MAKE_HRESULT(SEVERITY_ERROR, FACILITY_NULL, E_GETCURRENTPROCESSID_CALL)); //..WIP (alexz) use TPC_E_INVALID_WINDOW_HANDLE

        // register in m_HookThreadMap

        CHR(EnsureHookThreadItem(dwThreadId, this, &keyHookThreadItem, &fAddedMgr));
        pHookThreadItem = &(g_HookThreadMap[keyHookThreadItem]);
        pHookThreadItem->m_cUsages++;
        fCleanupThreadItem = true;

        // Set up the window hook if it has not been done yet for this thread.
        if (!pHookThreadItem->m_hHook)
        {
            CHR(InitializeHookThread(pHookThreadItem));
            fCleanupHook = true;
        }

        pCtx->m_keyHookThreadItem = keyHookThreadItem;

        // register in m_HookWindowMap

        CHR(EnsureHookWindowItem(hwnd, &keyHookWindowItem));
        fCleanupWindowItem = true;
        pHookWindowItem = &(m_HookWindowMap[keyHookWindowItem]);
        CHR(pHookWindowItem->m_ctxs.Add(pCtx));

        pCtx->m_keyHookWindowItem = keyHookWindowItem;

        // Now see if we need to start the waittimer
        if (pHookWindowItem->m_bNeedsTimer && !pHookThreadItem->m_bTimerStarted)
        {
            StartWaitTimer(pHookThreadItem);
        }

        RHR;

    CLEANUP:
        
        if (pAsyncData)
            delete pAsyncData;

        if (fCleanupThreadItem)
        {
            if (fAddedMgr)
            {
                for (INT i = 0; pHookThreadItem->m_mgrs.GetSize(); i++)
                {
                    if (pHookThreadItem->m_mgrs[i] == this)
                    {
                        pHookThreadItem->m_mgrs.Remove(i);
                        break;
                    }
                }
            }

            pHookThreadItem->m_cUsages--;
            if (!pHookThreadItem->m_cUsages)
            {
                // DDVSO:424827
                // Keep pHookThreadItem alive until we terminate the hook thread
                g_HookThreadMap.Remove(keyHookThreadItem, false /*deleteEntry*/);
            }
        }

        if (fCleanupWindowItem)
        {
            // Add of context failed so see if we need to unregister hwnd key in HookWindowMap
            if (!pHookWindowItem->m_ctxs.GetSize())
            {
                m_HookWindowMap.Remove(keyHookWindowItem, true /*deleteEntry*/);
            }
        }
    } // End of CHookLock block

    if (fCleanupHook)
    {
        TerminateHookThread(pHookThreadItem);
        delete pHookThreadItem;
    }

    RHR;
}


///////////////////////////////////////////////////////////////////////////////

HookThreadItemKey CPimcManager::FindHookThreadItem(DWORD dwThreadId)
{
    HookThreadItemKey   keyFound = NULL;
    HookThreadItemKey   keyCur = g_HookThreadMap.GetHead();
    while (!g_HookThreadMap.IsAtEnd(keyCur))
    {
        if (g_HookThreadMap[keyCur].m_dwThreadId == dwThreadId)
        {
            keyFound = keyCur;
            break;
        }
        keyCur = g_HookThreadMap.GetNext(keyCur);
    }
    return keyFound;
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcManager::EnsureHookThreadItem(DWORD dwThreadId, __in CPimcManager * pMgr, 
                                                   __out HookThreadItemKey * pKey, __out BOOL *pfAddedManager)
{
    DHR;
    *pfAddedManager = FALSE;
    *pKey = FindHookThreadItem(dwThreadId);
    if (!(*pKey))
    {
        CHR(g_HookThreadMap.AddToTail(pKey)); 
        g_HookThreadMap[*pKey].m_dwThreadId                 = dwThreadId;
        g_HookThreadMap[*pKey].m_cUsages                    = 0;
        g_HookThreadMap[*pKey].m_hHook                      = NULL;
        g_HookThreadMap[*pKey].m_hHookThread                = NULL;
        g_HookThreadMap[*pKey].m_hEventHookThreadReady      = NULL;
        g_HookThreadMap[*pKey].m_hEventHookThreadExit       = NULL;
        g_HookThreadMap[*pKey].m_hEventHookThreadExitAck    = NULL;
        g_HookThreadMap[*pKey].m_hTimer                     = NULL;
        g_HookThreadMap[*pKey].m_bTimerStarted              = false;
        g_HookThreadMap[*pKey].m_mgrs.Add(pMgr);
    }
    else
    {
        // Make sure this manager has been added to the HookThreadItem mgr list
        CHookThreadItem * pItem = &g_HookThreadMap[*pKey];
        
        BOOL fFound = FALSE;
        INT cMgrs = pItem->m_mgrs.GetSize();
        for (INT iMgr = 0; iMgr < cMgrs; iMgr++)
        {
            CPimcManager* pcurMgr = pItem->m_mgrs[iMgr];
            if (pcurMgr == pMgr)
            {
                fFound = TRUE;
                break;
            }
        }
        if (!fFound)
        {
            pItem->m_mgrs.Add(pMgr);
            *pfAddedManager = TRUE;
        }
    }

CLEANUP:
    RHR;
}

///////////////////////////////////////////////////////////////////////////////

CPimcManager::HookWindowItemKey CPimcManager::FindHookWindowItem(__in HWND hwnd)
{
    HookWindowItemKey   keyFound = NULL;
    HookWindowItemKey   keyCur = m_HookWindowMap.GetHead();
    while (!m_HookWindowMap.IsAtEnd(keyCur))
    {
        if (m_HookWindowMap[keyCur].m_hwnd == hwnd)
        {
            keyFound = keyCur;
            break;
        }
        keyCur = m_HookWindowMap.GetNext(keyCur);
    }
    return keyFound;
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcManager::EnsureHookWindowItem(__in HWND hwnd, __out HookWindowItemKey * pKey)
{
    DHR;
    *pKey = FindHookWindowItem(hwnd);
    if (!(*pKey))
    {
        CHR(m_HookWindowMap.AddToTail(pKey)); 
        m_HookWindowMap[*pKey].m_hwnd = hwnd;
        m_HookWindowMap[*pKey].m_bNeedsTimer = false;

        // See if this hwnd needs tracking by the waitable timer
        DWORD dwProcessId = 0;
        DWORD dwThreadId = GetWindowThreadProcessId(hwnd, &dwProcessId);
        HWND hwndParent = GetParent(hwnd);
        while (hwndParent != NULL)
        {
            DWORD dwProcessIdParent = 0;
            DWORD dwThreadIdParent = GetWindowThreadProcessId(hwndParent, &dwProcessIdParent);
            if (dwProcessIdParent != dwProcessId || dwThreadIdParent != dwThreadId)
            {
                RECT rc = {0}; // Init to empty rect to make sure it triggers first time.
                m_HookWindowMap[*pKey].m_rc = rc;
                m_HookWindowMap[*pKey].m_bNeedsTimer = true;
                break;
            }
            hwndParent = GetParent(hwndParent);
        }
    }

CLEANUP:
    RHR;
}

///////////////////////////////////////////////////////////////////////////////

void CPimcManager::InstallWindowHookApcCore(__typefix(CAsyncData *) __inout ULONG_PTR pvAsyncData)
{
    CAsyncData *    pAsyncData = (CAsyncData*)pvAsyncData;
    DWORD           dwThreadId = (DWORD)pAsyncData->m_dwArg;

    HHOOK           hHook = SetWindowsHookEx(WH_CALLWNDPROC, HookProc, NULL, dwThreadId);

    pAsyncData->m_dwRes = (DWORD_PTR)hHook;

    pAsyncData->SignalAck();
}

///////////////////////////////////////////////////////////////////////////////

HRESULT CPimcManager::UninstallWindowHook(__in CPimcContext * pCtx)
{
    DHR;

    CHookThreadItem * pThreadItem = NULL;
    HookThreadItemKey keyHookThreadItem = NULL;
    bool shouldTerminateHookThread = false;

    // DDVSO:220285
    // Keeping the CHookLock while the hook thread is being terminated in TerminateHookThread 
    // can lead to a deadlock situation.  If any message comes through the hook thread 
    // or if the timer ticks while we have this lock, the hook thread itself may attempt 
    // to acquire the lock in several of its handlers.  If this occurs, TerminateHookThread 
    // will eventually wait forever on m_hEventHookThreadExitAck which can never be signaled 
    // since the hook thread is waiting on CHookLock.
    //
    // To stop this occurring, scope the hook lock to only what needs a lock, the
    // processing of contexts using the hook thread.  Once contexts are manipulated
    // we can signal the hook thread to exit with confidence that m_hEventHookThreadExitAck 
    // will be signaled as the hook thread is free to process.
    {
        CHookLock   lock;

        // unregister in HookThreadMap

        keyHookThreadItem = pCtx->m_keyHookThreadItem;
        pThreadItem = &g_HookThreadMap[keyHookThreadItem];

        // unregister in HookWindowMap

        HookWindowItemKey keyHookWindowItem = pCtx->m_keyHookWindowItem;
        CHookWindowItem * pWindowItem = &m_HookWindowMap[keyHookWindowItem];
        for (INT idx = 0; pWindowItem->m_ctxs.GetSize(); idx++)
        {
            if (pWindowItem->m_ctxs[idx] == pCtx)
            {
                pWindowItem->m_ctxs.Remove(idx);
                break;
            }
        }

        BOOL bNeedsTimer = pWindowItem->m_bNeedsTimer;
        if (pWindowItem->m_ctxs.GetSize() == 0)
        {
            m_HookWindowMap.Remove(keyHookWindowItem, true /*deleteEntry*/);

            // If no more windows on this manager, then remove this pMgr from the list
            if (m_HookWindowMap.IsEmpty())
            {
                for (INT i = 0; pThreadItem->m_mgrs.GetSize(); i++)
                {
                    if (pThreadItem->m_mgrs[i] == this)
                    {
                        pThreadItem->m_mgrs.Remove(i);
                        break;
                    }
                }
            }
        }

        // see if we can turn off waitabletimer
        // Now see if we need to start the waittimer
        if (bNeedsTimer && pThreadItem->m_bTimerStarted)
        {
            StopWaitTimerIfNotNeeded(pThreadItem);
        }

        pThreadItem->m_cUsages--;

        if (pThreadItem->m_cUsages == 0)
        {
            // DDVSO:424827
            // Keep pThreadItem alive until we terminate the hook thread
            g_HookThreadMap.Remove(keyHookThreadItem, false /*deleteEntry*/);
            shouldTerminateHookThread = true;
        }
    } // End of CHookLock block

    if (shouldTerminateHookThread)
    {
        TerminateHookThread(pThreadItem);
        delete pThreadItem;
    }

    RHR;
}

/////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK CPimcManager::HookProc(int nCode, WPARAM wParam, LPARAM lParam)
{
    PCWPSTRUCT  pcwp = (PCWPSTRUCT)lParam;
    HWND        hwnd = pcwp->hwnd;
    DWORD       dwTid;
    DWORD       dwPid;

    if (!IsWindow(hwnd))
        goto CLEANUP;

    dwTid = GetWindowThreadProcessId(hwnd, &dwPid);
    if (dwTid == 0 || GetCurrentProcessId() != dwPid)
        goto CLEANUP;

    try
    {
        //static BOOL fInSizeMove = FALSE;

        if (0 <= nCode)
        {
            switch (pcwp->message)
            {
            case WM_MDIACTIVATE:
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SendToTop,  (HWND)pcwp->lParam);
                break;

            case WM_ACTIVATE:
                {
                    if (WA_INACTIVE != pcwp->wParam)
                    {
                        MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SendToTop, hwnd);
                    }
                }
                break;

            case WM_CHILDACTIVATE:
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SendToTop,  hwnd);
                break;

            //case WM_INITMENUPOPUP:
            //    MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SendToBack,  hwnd);
            //    break;

            case WM_UNINITMENUPOPUP:
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SendToTop,  hwnd);
                break;

            case WM_COMMAND:
                if ((HIWORD(pcwp->wParam) == CBN_CLOSEUP))
                {
                    MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SendToTop, hwnd);
                }
                break;
            
            case WM_SIZE:
            case WM_MOVE:
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_SizeMove, hwnd);
                break;

#if 0 //..WIP (alexz)
            case WM_ENTERSIZEMOVE:
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_Disable, hwnd);
                fInSizeMove = TRUE;
                break;

            case WM_SETCURSOR:
                if (!fInSizeMove)
                    break;
                // else
                //    fall thru

            case WM_EXITSIZEMOVE:
                //..WIP (alexz) this is not correct if the context was disabled
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_Enable, hwnd);
                fInSizeMove = FALSE; 
                break;

            case WM_DESTROY:
                MgrHandleCtxUpdate(dwTid, CPimcContext::UPDATE_Unhook, hwnd);
                break;
#endif
            }
        }
    }
    catch (...)
    {
        ASSERT(FALSE && "not reached");
    }

CLEANUP:

    return CallNextHookEx(NULL, nCode, wParam, lParam);
}


/////////////////////////////////////////////////////////////////////////////

void CPimcManager::HandleTimer(DWORD dwThreadId)
{
    CHookLock   lock;

    // Look up CHookThreadItem instance for this thread.
    HookThreadItemKey hookThreadKey = FindHookThreadItem(dwThreadId);
    CHookThreadItem * pThreadItem = &g_HookThreadMap[hookThreadKey];

    // DDVSO:220285
    // If the CHookThreadItem is either awaiting cleanup due to a failed install
    // or we are uninstalling the hook thread, do not initiate processing.  Both
    // cleanup and uninstall will remove the last entry for a CHookThreadItem, so
    // if the result of the lookup is NULL we know we are in a cleanup/shutdown
    // scenario.
    if (NULL != pThreadItem)
    {
        // Loop through the CPimcManager list looking for contexts that need the timer.
        INT cMgrs = pThreadItem->m_mgrs.GetSize();
        for (INT i = 0; i < cMgrs; i++)
        {
            CPimcManager* pMgr = pThreadItem->m_mgrs[i];

            HookWindowItemKey   keyCur = pMgr->m_HookWindowMap.GetHead();
            while (!pMgr->m_HookWindowMap.IsAtEnd(keyCur))
            {
                CHookWindowItem * pItem = &pMgr->m_HookWindowMap[keyCur];
                if (pItem->m_bNeedsTimer)
                {
                    HWND hwnd = pItem->m_hwnd;
                    // Only do this work if the window is still valid.
                    if (::IsWindow(hwnd))
                    {
                        RECT rc = { 0 };
                        ::GetWindowRect(hwnd, &rc);
                        if (!EqualRect(&rc, &(pItem->m_rc)))
                        {
                            pItem->m_rc = rc;
                            // We only need to update contexts for this window (any children will also use timer).
                            INT c = pItem->m_ctxs.GetSize();
                            for (INT i = 0; i < c; i++)
                            {
                                CPimcContext * pCtx = pItem->m_ctxs[i];
                                pCtx->PostUpdate(CPimcContext::UPDATE_SizeMove);
                            }
                        }
                    }
                }
                keyCur = pMgr->m_HookWindowMap.GetNext(keyCur);
            }
        }

        StartWaitTimer(pThreadItem);
    }
}

/////////////////////////////////////////////////////////////////////////////

void CPimcManager::StartWaitTimer(__inout CHookThreadItem * pThread)
{
    LARGE_INTEGER liDueTime;
    liDueTime.QuadPart=-WAITTIMER_DELAY;

    pThread->m_bTimerStarted = SetWaitableTimer(pThread->m_hTimer, &liDueTime, 0, NULL, NULL, 0);
}

/////////////////////////////////////////////////////////////////////////////

void CPimcManager::StopWaitTimerIfNotNeeded(__inout CHookThreadItem * pThread)
{
    // If no other contexts require timer then stop it.
    if (!DoContextsNeedWaitableTimer(pThread))
    {
        CancelWaitableTimer(pThread->m_hTimer);
        pThread->m_bTimerStarted = FALSE;
    }
}

/////////////////////////////////////////////////////////////////////////////

BOOL CPimcManager::DoContextsNeedWaitableTimer(__in CHookThreadItem * pThread)
{
    // Loop through the CPimcManager list looking for contexts that need the timer.
    INT cMgrs = pThread->m_mgrs.GetSize();
    for (INT i = 0; i < cMgrs; i++)
    {
        CPimcManager* pMgr = pThread->m_mgrs[i];

        HookWindowItemKey   keyCur = pMgr->m_HookWindowMap.GetHead();
        while (!pMgr->m_HookWindowMap.IsAtEnd(keyCur))
        {
            if (pMgr->m_HookWindowMap[keyCur].m_bNeedsTimer)
            {
                return TRUE;
            }
            keyCur = pMgr->m_HookWindowMap.GetNext(keyCur);
        }
    }
    return FALSE;
}


/////////////////////////////////////////////////////////////////////////////


void CPimcManager::MgrHandleCtxUpdate(DWORD dwThreadId, DWORD dwUpdate, __in HWND hwnd)
{
    CHookLock   lock;

    // Look up CPimcManager instance for this thread and process update on that instance.
    HookThreadItemKey hookThreadKey = FindHookThreadItem(dwThreadId);
    CHookThreadItem * pThreadItem = &g_HookThreadMap[hookThreadKey];

    if (pThreadItem != NULL)
    {
        PostCtxUpdateForSubtree(dwUpdate, hwnd, pThreadItem);
    }
}

/////////////////////////////////////////////////////////////////////////////

void CPimcManager::PostCtxUpdateForWnd(DWORD dwUpdate, __in HWND hwnd, __in CHookThreadItem * pThreadItem)
{
    // Since we can have multiple CPimcManager objects per thread we need to enum
    // them and notify all of them of this context update for this hwnd.
    INT cMgrs = pThreadItem->m_mgrs.GetSize();
    for (INT iMgr = 0; iMgr < cMgrs; iMgr++)
    {
        CPimcManager* pMgr = pThreadItem->m_mgrs[iMgr];
        
        HookWindowItemKey key = pMgr->FindHookWindowItem(hwnd);
        if (key)
        {
            CHookWindowItem * pItem = &(pMgr->m_HookWindowMap[key]);

            // Update our rect if the hookproc window messages triggers an update to our size.
            if (pItem->m_bNeedsTimer && ((dwUpdate & CPimcContext::UPDATE_SizeMove) != 0))
            {
                RECT rc = {0};
                ::GetWindowRect(pItem->m_hwnd, &rc);
                pItem->m_rc = rc;
            }
            
            INT c = pItem->m_ctxs.GetSize();
            for (INT i = 0; i < c; i++)
            {
                CPimcContext * pCtx = pItem->m_ctxs[i];
                pCtx->PostUpdate(dwUpdate);
            }
        }
    }
}

/////////////////////////////////////////////////////////////////////////////

void CPimcManager::PostCtxUpdateForSubtree(DWORD dwUpdate, __in HWND hwndRoot, __in CHookThreadItem * pThreadItem)
{
    try
    {
        DHR;
        CPbList<HWND>   queue;
        CHR(queue.AddToTail(hwndRoot));
        for (;;)
        {
            PBLKEY keyHead = queue.GetHead();
            if (queue.IsAtEnd(keyHead))
                break;

            HWND hwndCur = queue[keyHead];
            queue.Remove(keyHead, true /*deleteEntry*/);

            // handle the event for this hwnd
            PostCtxUpdateForWnd(dwUpdate, hwndCur, pThreadItem);

            // enumerate children
            hwndCur = ::GetWindow(hwndCur, GW_CHILD);
            if (hwndCur)
            {
                hwndCur = ::GetWindow(hwndCur, GW_HWNDLAST);
                while (hwndCur)
                {
                    CHR(queue.AddToTail(hwndCur));
                    hwndCur= ::GetWindow(hwndCur, GW_HWNDPREV);
                }
            }
        }
CLEANUP:
        return;
    }
    catch (...)
    {
    }
}

/////////////////////////////////////////////////////////////////////////////

#if WANT_PROFILE
BOOL CPimcManager::IsProfiling()
{
    if (!m_fIsProfilingCached)
    {
        m_fIsProfilingCached = TRUE;
        m_fIsProfiling = FALSE;

        HKEY hKey;
        if (ERROR_SUCCESS == RegOpenKeyExW(HKEY_CURRENT_USER, SZ_REGKEY_PROFILE, 0, KEY_QUERY_VALUE, &hKey))
        {
            DWORD cbSize = sizeof(DWORD);
            DWORD dwProfiling = 0;
            RegQueryValueExW(hKey, L"V2Profiling", NULL, NULL, (BYTE*) &dwProfiling, &cbSize);
            RegCloseKey(hKey);

            m_fIsProfiling = dwProfiling != 0;
        }
    }
    return m_fIsProfiling;
}
#endif

/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcManager::GetTabletCount(__out ULONG* pcTablets)
{
    DHR;

    ULONG cTablets = 0;

    LoadWisptis(); // Try to load wisptis via the surrogate object.
    
    // we will return 0 in the case that there is no stylus since mouse is not considered a stylus anymore
    if (m_fLoadedWisptis)
    {
        CHR(m_pMgrS->GetTabletCount(&cTablets));
    }
    
    *pcTablets = cTablets;
    
CLEANUP:
    RHR;
}


/////////////////////////////////////////////////////////////////////////////

STDMETHODIMP CPimcManager::GetTablet(ULONG iTablet, __deref_out IPimcTablet3** ppTablet)
{
    DHR;

    switch (iTablet)
    {
        case RELEASE_MANAGER_EXT:
        {
            CHR(m_managerLock.Unlock());
        }
        break;
        default:
        {
            CHR(GetTabletImpl(iTablet, ppTablet));
        }
    }

CLEANUP:
    RHR;
}

STDMETHODIMP CPimcManager::GetTabletImpl(ULONG iTablet, __deref_out IPimcTablet3** ppTablet)
{
    DHR;
    LoadWisptis(); // Make sure wisptis has been loaded! (Can happen when handling OnTabletAdded message)
    
    CComPtr<ITablet>            pTabS;
    CComObject<CPimcTablet> *   pTabC;

    // Can only call if we have real tablet hardware which means wisptis must be loaded!
    CHR(m_fLoadedWisptis ? S_OK : E_UNEXPECTED);
    CHR(CComObject<CPimcTablet>::CreateInstance(&pTabC));
    CHR(pTabC->QueryInterface(IID_IPimcTablet3, (void**)ppTablet));
    CHR(m_pMgrS->GetTablet(iTablet, &pTabS));
    CHR(pTabC->Init(m_fLoadedWisptis?pTabS:NULL, this));

CLEANUP:
    RHR;
}

