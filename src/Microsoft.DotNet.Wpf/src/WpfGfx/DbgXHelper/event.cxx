// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++



Module Name:

    event.cxx

Abstract:

    This file contains the routines to track and handle
    debugger events.

Environment:

    User Mode

--*/

#include "precomp.hxx"

BOOL gbSymbolsNotLoaded = TRUE;

ULONG UniqueTargetState = INVALID_UNIQUE_STATE;

#if DBG && 0
ULONG
DbgEventPrint(
    IN PCHAR Format,
    ...
    )
{
    va_list arglist;

    va_start(arglist, Format);
    return vDbgPrintExWithPrefix("Event: ", -1, 0, Format, arglist);
}

#else
#define DbgEventPrint
#endif

typedef struct {
    PDEBUG_CLIENT   Client;
    BOOL            ParamsRead;
} MonitorThreadParams;


DWORD WINAPI EventMonitorThread(MonitorThreadParams *);


class EventMonitorCallbacks : public DebugBaseEventCallbacks
{
private:
    ULONG           RefCount;

public:

    EventMonitorCallbacks()
    {
        RefCount = 1;
    }

    // IUnknown
    STDMETHOD_(ULONG, AddRef)(
        THIS
        )
    {
        RefCount++;

        return RefCount;
    }

    STDMETHOD_(ULONG, Release)(
        THIS
        )
    {
        RefCount--;

        if (RefCount == 0)
        {
            delete this;
            return 0;
        }

        return RefCount;
    }


    // IDebugEventCallbacks.
    
    STDMETHOD(GetInterestMask)(
        THIS_
        OUT PULONG Mask
        )
    {
        DbgEventPrint("GetInterestMask\n");

        if (Mask != NULL)
        {
            *Mask = DEBUG_EVENT_SESSION_STATUS |
                    DEBUG_EVENT_CHANGE_DEBUGGEE_STATE |
                    DEBUG_EVENT_CHANGE_ENGINE_STATE |
                    DEBUG_EVENT_CHANGE_SYMBOL_STATE |
                    DEBUG_EVENT_UNLOAD_MODULE;
        }

        return S_OK;
    }

    STDMETHOD(Breakpoint)(
        THIS_
        IN PDEBUG_BREAKPOINT /*Bp*/
        )
    {
        DbgEventPrint("BP\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(Exception)(
        THIS_
        IN PEXCEPTION_RECORD64 /*Exception*/,
        IN ULONG /*FirstChance*/
        )
    {
        DbgEventPrint("Exception\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(CreateThread)(
        THIS_
        IN ULONG64 /*Handle*/,
        IN ULONG64 /*DataOffset*/,
        IN ULONG64 /*StartOffset*/
        )
    {
        DbgEventPrint("CreateThread\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(ExitThread)(
        THIS_
        IN ULONG /*ExitCode*/
        )
    {
        DbgEventPrint("ExitThread\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(CreateProcess)(
        THIS_
        IN ULONG64 /*ImageFileHandle*/,
        IN ULONG64 /*Handle*/,
        IN ULONG64 /*BaseOffset*/,
        IN ULONG /*ModuleSize*/,
        IN PCSTR /*ModuleName*/,
        IN PCSTR /*ImageName*/,
        IN ULONG /*CheckSum*/,
        IN ULONG /*TimeDateStamp*/,
        IN ULONG64 /*InitialThreadHandle*/,
        IN ULONG64 /*ThreadDataOffset*/,
        IN ULONG64 /*StartOffset*/
        )
    {
        DbgEventPrint("CreateProcess\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(ExitProcess)(
        THIS_
        IN ULONG /*ExitCode*/
        )
    {
        DbgEventPrint("ExitProcess\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(LoadModule)(
        THIS_
        IN ULONG64 /*ImageFileHandle*/,
        IN ULONG64 BaseOffset,
        IN ULONG /*ModuleSize*/,
        IN PCSTR ModuleName,
        IN PCSTR ImageName,
        IN ULONG /*CheckSum*/,
        IN ULONG /*TimeDateStamp*/
        )
    {
        DbgEventPrint("LoadModule:\n"
                      "  ModuleName: %s\n"
                      "  ImageName: %s\n"
                      "  BaseOffset: %I64x\n",
                      ModuleName, ImageName, BaseOffset);
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(UnloadModule)(
        THIS_
        IN PCSTR /*ImageBaseName*/,
        IN ULONG64 BaseOffset
        )
    {
        // Don't use Image base name for now - Debugger bug
        //DbgEventPrint("UnloadModule %s @ %I64x\n", ImageBaseName, BaseOffset);
        DbgEventPrint("UnloadModule ? @ %I64x\n", BaseOffset);
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(SystemError)(
        THIS_
        IN ULONG Error,
        IN ULONG Level
        )
    {
        DbgEventPrint("SystemError(%lu, %lu)\n", Error, Level);
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(SessionStatus)(
        THIS_
        IN ULONG Status
        )
    {
        DbgEventPrint("SessionStatus(%lu)\n", Status);
        if (Status == DEBUG_SESSION_ACTIVE) DbgEventPrint("DEBUG_SESSION_ACTIVE\n");
        if (Status == DEBUG_SESSION_END_SESSION_ACTIVE_TERMINATE) DbgEventPrint("DEBUG_SESSION_END_SESSION_ACTIVE_TERMINATE\n");
        if (Status == DEBUG_SESSION_END_SESSION_ACTIVE_DETACH) DbgEventPrint("DEBUG_SESSION_END_SESSION_ACTIVE_DETACH\n");
        if (Status == DEBUG_SESSION_END_SESSION_PASSIVE) DbgEventPrint("DEBUG_SESSION_END_SESSION_PASSIVE\n");
        if (Status == DEBUG_SESSION_END) DbgEventPrint("DEBUG_SESSION_END\n");
        if (Status == DEBUG_SESSION_REBOOT) DbgEventPrint("DEBUG_SESSION_REBOOT\n");
        if (Status == DEBUG_SESSION_HIBERNATE) DbgEventPrint("DEBUG_SESSION_HIBERNATE\n");
        if (Status == DEBUG_SESSION_FAILURE) DbgEventPrint("DEBUG_SESSION_FAILURE\n");
        return DEBUG_STATUS_NO_CHANGE;
    }

    STDMETHOD(ChangeDebuggeeState)(
        THIS_
        IN ULONG Flags,
        IN ULONG64 Argument
        )
    {
        DbgEventPrint("ChangeDebuggeeState(0x%lx, 0x%I64x)\n", Flags, Argument);
        if (Flags == DEBUG_CDS_ALL)
        {
            DbgEventPrint("DEBUG_CDS_ALL\n");
            UniqueTargetState++;
        }
        else
        {
            if (Flags & DEBUG_CDS_REGISTERS) DbgEventPrint("DEBUG_CDS_REGISTERS\n");
            if (Flags & DEBUG_CDS_DATA)
            {
                DbgEventPrint("DEBUG_CDS_DATA\n");
                UniqueTargetState++;
            }
        }
        if (UniqueTargetState==INVALID_UNIQUE_STATE) UniqueTargetState++;
        return S_OK;
    }

    STDMETHOD(ChangeEngineState)(
        THIS_
        IN ULONG Flags,
        IN ULONG64 Argument
        )
    {
        //DbgEventPrint("ChangeEngineState(0x%lx, 0x%I64x)\n", Flags, Argument);
        if (Flags == DEBUG_CES_ALL)
        {
            DbgEventPrint("DEBUG_CES_ALL\n");
            UniqueTargetState++;
            if (UniqueTargetState==INVALID_UNIQUE_STATE) UniqueTargetState++;
        }
        else
        {
            if (Flags & DEBUG_CES_CURRENT_THREAD) DbgEventPrint("DEBUG_CES_CURRENT_THREAD\n");
            if (Flags & DEBUG_CES_EFFECTIVE_PROCESSOR) DbgEventPrint("DEBUG_CES_EFFECTIVE_PROCESSOR\n");
            if (Flags & DEBUG_CES_BREAKPOINTS) DbgEventPrint("DEBUG_CES_BREAKPOINTS\n");
            if (Flags & DEBUG_CES_CODE_LEVEL) DbgEventPrint("DEBUG_CES_CODE_LEVEL\n");
            if (Flags & DEBUG_CES_EXECUTION_STATUS)
            {
                DbgEventPrint("DEBUG_CES_EXECUTION_STATUS\n");
                switch (Argument & DEBUG_STATUS_MASK)
                {
                    case DEBUG_STATUS_NO_CHANGE: DbgPrint("Exec Status: DEBUG_STATUS_NO_CHANGE\n"); break;
                    case DEBUG_STATUS_GO: DbgPrint("Exec Status: DEBUG_STATUS_GO\n"); break;
                    case DEBUG_STATUS_GO_HANDLED: DbgPrint("Exec Status: DEBUG_STATUS_GO_HANDLED\n"); break;
                    case DEBUG_STATUS_GO_NOT_HANDLED: DbgPrint("Exec Status: DEBUG_STATUS_GO_NOT_HANDLED\n"); break;
                    case DEBUG_STATUS_STEP_OVER: DbgPrint("Exec Status: DEBUG_STATUS_STEP_OVER\n"); break;
                    case DEBUG_STATUS_STEP_INTO: DbgPrint("Exec Status: DEBUG_STATUS_STEP_INTO\n"); break;
                    case DEBUG_STATUS_BREAK: DbgPrint("Exec Status: DEBUG_STATUS_BREAK\n"); break;
                    case DEBUG_STATUS_NO_DEBUGGEE: DbgPrint("Exec Status: DEBUG_STATUS_NO_DEBUGGEE\n"); break;
                    case DEBUG_STATUS_STEP_BRANCH: DbgPrint("Exec Status: DEBUG_STATUS_STEP_BRANCH\n"); break;
                    case DEBUG_STATUS_IGNORE_EVENT: DbgPrint("Exec Status: DEBUG_STATUS_IGNORE_EVENT\n"); break;
                    default: DbgPrint("Exec Status: Unknown\n"); break;
                }
                if (Argument & DEBUG_STATUS_INSIDE_WAIT) DbgPrint("Exec Status: DEBUG_STATUS_INSIDE_WAIT\n");
                if ((Argument & DEBUG_STATUS_MASK) != DEBUG_STATUS_NO_CHANGE)
                {
                    UniqueTargetState++;
                    if (UniqueTargetState==INVALID_UNIQUE_STATE) UniqueTargetState++;
                }
            }
            if (Flags & DEBUG_CES_ENGINE_OPTIONS) DbgEventPrint("DEBUG_CES_ENGINE_OPTIONS\n");
            if (Flags & DEBUG_CES_LOG_FILE) DbgEventPrint("DEBUG_CES_LOG_FILE\n");
            //if (Flags & DEBUG_CES_RADIX) DbgEventPrint("DEBUG_CES_RADIX\n");
            if (Flags & DEBUG_CES_EVENT_FILTERS) DbgEventPrint("DEBUG_CES_EVENT_FILTERS\n");
            if (Flags & DEBUG_CES_PROCESS_OPTIONS) DbgEventPrint("DEBUG_CES_PROCESS_OPTIONS\n");
            if (Flags & DEBUG_CES_EXTENSIONS) DbgEventPrint("DEBUG_CES_EXTENSIONS\n");
        }
        return S_OK;
    }

    STDMETHOD(ChangeSymbolState)(
        THIS_
        IN ULONG Flags,
        IN ULONG64 Argument
        )
    {
        DbgEventPrint("ChangeSymbolState(0x%lx, 0x%I64x)\n", Flags, Argument);
        gbSymbolsNotLoaded = gbSymbolsNotLoaded || (Flags & DEBUG_CSS_UNLOADS);
        UniqueTargetState++;
        if (UniqueTargetState==INVALID_UNIQUE_STATE) UniqueTargetState++;
        return S_OK;
    }
};

enum MonitorState {
    NO_DISPATCHING,
    NEED_DISPATCH,
    DISPATCHED
};

LONG            g_MonitorState = NO_DISPATCHING;
PDEBUG_CLIENT   g_pMonitorClient = NULL;
BOOL            g_MonitorThreadSet = FALSE;

DWORD
WINAPI
EventMonitorThread(
    MonitorThreadParams *Params
    )
{
    HRESULT                 hr = S_OK;
    PDEBUG_CLIENT           Client;
    MonitorThreadParams     ParamCopy;
    HMODULE                 hModule = NULL;
    TCHAR                   ModulePath[256];

    if (Params != NULL && Params->Client != NULL)
    {
        ASSERTMSG("EventMonitorThread not started with NEED_DISPATCH.\n", g_MonitorState == NEED_DISPATCH);

        if (GetModuleFileName(ghDllInst, ModulePath, sizeof(ModulePath)/sizeof(TCHAR)) == 0)
        {
            DbgPrint("EventMonitorThread failed to get Module path.\n");
            hr = S_FALSE;
        }
        else
        {
            // LoadLibrary so we have a reference while this thread lives
            hModule = LoadLibrary(ModulePath);

            if (hModule != ghDllInst)
            {
                DbgPrint("EventMonitorThread retrieving an hModule different from ghDllInst.\n");
                hr = S_FALSE;
            }
        }

        if (hr == S_OK)
        {
            Params->Client->AddRef();

            ParamCopy = *Params;
            MemoryBarrier();
            Params->ParamsRead = TRUE;
            Params = &ParamCopy;

            hr = Params->Client->CreateClient(&Client);
            DbgPrint("EventMonitorThread created client %p.\n", Client);

            Params->Client->Release();

            if (hr == S_OK)
            {
                EventMonitorCallbacks  *EventMonitor = new EventMonitorCallbacks;

                if (EventMonitor != NULL)
                {
                    hr = Client->SetEventCallbacks(EventMonitor);

                    if (hr == S_OK)
                    {
                        // Pass monitoring client back to caller.
                        Client->AddRef();
                        if (InterlockedCompareExchangePointer((PVOID*)&g_pMonitorClient, Client, NULL) == NULL &&
                            InterlockedCompareExchange(&g_MonitorState, DISPATCHED, NEED_DISPATCH) == NEED_DISPATCH)
                        {
                            DbgPrint("EventMonitorThread dispatching for client %p.\n", Client);
                            UniqueTargetState++;
                            hr = Client->DispatchCallbacks(INFINITE);
                        }
                        else
                        {
                            // Another EventMonitorThread has already started or
                            // ReleaseEventCallbacks has already been called; so,
                            // release this client and
                            // NULL global monitor client if we set it
                            DbgPrint("EventMonitorThread exiting instead of dispatching for client %p.\n", Client);
                            InterlockedCompareExchangePointer((PVOID*)&g_pMonitorClient, NULL, Client);
                            Client->Release();
                        }

                        // Remove Client's reference to EventMonitor
                        Client->SetEventCallbacks(NULL);
                    }
                    else
                    {
                        OutputControl   OutCtl(Client);
                        OutCtl.OutErr("EventMonitorThread callbacks setup failed, %s.\n", pszHRESULT(hr));
                    }

#if DBG
                    ULONG DbgRemaingReferences =
#endif
                    EventMonitor->Release();
#if DBG
                    ASSERTMSG("Unexpected outstanding references to EventMonitor.\n",
                              DbgRemaingReferences == 0);
#endif
                }
                else
                {
                    hr = E_OUTOFMEMORY;
                }

                Client->Release();
            }
        }
    }
    else
    {
        hr = E_INVALIDARG;
    }

    DbgPrint("EventMonitorThread calling ExitThread().\n");

    FreeLibraryAndExitThread(hModule, (DWORD)hr);
}


void
ReleaseEventCallbacks(
    PDEBUG_CLIENT Client
    )
{
    if (g_MonitorThreadSet)
    {
        if (InterlockedExchange(&g_MonitorState, NO_DISPATCHING) == DISPATCHED)
        {
            PDEBUG_CLIENT   pMonitorClient;
            
            #pragma warning(push)
            // InterlockedExchangePointer is not /Wp64 happy
            #pragma warning(disable : 4312)
            pMonitorClient = (PDEBUG_CLIENT)InterlockedExchangePointer((PVOID *)&g_pMonitorClient, NULL);
            #pragma warning(pop)

            ASSERTMSG("g_MonitorState shows g_pMonitorClient should be set.\n", pMonitorClient != NULL);

            if (Client == NULL)
            {
                if (GetDebugClient(&Client) != S_OK)
                {
                    Client = pMonitorClient;
                    Client->AddRef();
                }
            }
            else
            {
                Client->AddRef();
            }

            Client->ExitDispatch(pMonitorClient);
            pMonitorClient->Release();
            Client->Release();
        }

        g_MonitorThreadSet = FALSE;
    }
}


HRESULT
SetEventCallbacks(
    PDEBUG_CLIENT Client
    )
{
    HRESULT hr = S_FALSE;

    if (!g_MonitorThreadSet)
    {
        MonitorThreadParams  NewThreadParams = { Client, FALSE };
        HANDLE  hThread;
        DWORD   ThreadID;
        LONG    PrevMonitorState;

        PrevMonitorState = InterlockedExchange(&g_MonitorState, NEED_DISPATCH);
        ASSERTMSG("Previous EventMonitor thread was never shutdown properly.\n", PrevMonitorState != DISPATCHED);
        ASSERTMSG("Previous EventMonitor thread never completed setup.\n", PrevMonitorState != NEED_DISPATCH);

        g_pMonitorClient = NULL;

        hThread = CreateThread(NULL,
                               0,
                               (LPTHREAD_START_ROUTINE)EventMonitorThread,
                               &NewThreadParams,
                               0,
                               &ThreadID);

        if (hThread)
        {
            // Default ExitCode to STILL_ACTIVE since it doesn't matter
            // if the Params were read before we started checking.
            DWORD ExitCode = STILL_ACTIVE;

            while (!NewThreadParams.ParamsRead)
            {
                ExitCode = 0;
                if (!GetExitCodeThread(hThread, &ExitCode))
                    DbgPrint("GetExitCodeThread returned error %lx.\n", GetLastError());
                if (ExitCode != STILL_ACTIVE)
                {
                    break;
                }

                Sleep(10);
            }

            if (ExitCode == STILL_ACTIVE)
            {
                hr = S_OK;
                g_MonitorThreadSet = TRUE;
            }

            CloseHandle(hThread);
        }
    }

    return hr;
}


HRESULT
EventCallbacksReady(
    PDEBUG_CLIENT /*Client*/
    )
{
    return (g_MonitorThreadSet && g_MonitorState == DISPATCHED) ? S_OK : S_FALSE;
}


