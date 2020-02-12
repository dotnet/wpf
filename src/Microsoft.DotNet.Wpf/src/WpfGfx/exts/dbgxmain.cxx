// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++



Module Name:

   DbgXMain.cxx

Abstract:

    This file contains the generic routines and initialization code
    for the kernel and user mode debugger extensions dll by directly
    providing entrypoints called by debugger engine.

    Consuming debugger dll is expected to provide certain global data
    and specialized methods for some inialization steps.

Environment:

    User Mode

--*/

#include "precomp.hxx"

//__nullterminated CHAR *NAMES = {"wpfgfx_v0400", "wpfgfx_v0300", "MilCore"};

ModuleParameters Milcore_Module = { 0, DEBUG_ANY_ID, "MILCore", "dll" };
ModuleParameters wpfgfx_v0300_Module = { 0, DEBUG_ANY_ID, "wpfgfx_v0300", "dll" };
ModuleParameters wpfgfx_v0400_Module = { 0, DEBUG_ANY_ID, "wpfgfx_v0400", "dll" };

// Default symbol load module - set MILCore module as default
ModuleParameters UM_Module;


//
// globals
//

HINSTANCE               ghDllInst;


//
// Valid for the lifetime of the debug session.
//

ULONG   TargetMachine;
ULONG   TargetClass;
ULONG   PlatformId = static_cast<UINT>(-1);
ULONG   MajorVer = 0;
ULONG   MinorVer = 0;
ULONG   SrvPack = 0;
ULONG   BuildNo = 0;

BOOL    Connected = FALSE;
BOOL    Remote = FALSE;
CHAR    RemoteID[MAX_PATH];

extern ModuleParameters UM_Module;
ModuleParameters Type_Module;



BOOLEAN
WINAPI
DllMain(
    HINSTANCE hInstDll,
    DWORD fdwReason,
    LPVOID /*lpvReserved*/
    )
{
    switch (fdwReason) {
        case DLL_PROCESS_DETACH:
            break;

        case DLL_PROCESS_ATTACH:
            DbgPrint("DllMain: DLL_PROCESS_ATTACH: hInstance = %lx => ghDllInit(%lx)\n", hInstDll, ghDllInst);
            ghDllInst = hInstDll;
            DisableThreadLibraryCalls(hInstDll);
            break;
    }

    return TRUE;
}


extern "C"
HRESULT
CALLBACK
DebugExtensionSetClient(
    LPCSTR RemoteArgs
    )
{
    if (RemoteArgs != NULL)
    {
        Remote = TRUE;
        StringCchCopyA(RemoteID, ARRAYSIZE(RemoteID), RemoteArgs);
    }
    else
    {
        Remote = FALSE;
    }

    return S_OK;
}


HRESULT
GetDebugClient(
    __deref_out PDEBUG_CLIENT *pClient
    )
{
    HRESULT         hr = S_FALSE;
    PDEBUG_CLIENT   Client;

    if (pClient == NULL)
    {
        return S_FALSE;
    }

    *pClient = NULL;

    if (Remote)
    {
        hr = DebugConnect(RemoteID, __uuidof(IDebugClient), (void **)&Client);
        if (hr == S_OK)
        {
            hr = Client->ConnectSession(DEBUG_CONNECT_SESSION_NO_VERSION |
                                        DEBUG_CONNECT_SESSION_NO_ANNOUNCE,
                                        0);
            if (hr != S_OK)
            {
                Client->Release();
            }
        }
    }
    else
    {
        hr = DebugCreate(__uuidof(IDebugClient), (void **)&Client);
    }

    if (hr == S_OK)
    {
        *pClient = Client;
    }

    return hr;
}


//PDEBUG_EXTENSION_INITIALIZE
extern "C"
HRESULT
CALLBACK
DebugExtensionInitialize(
    PULONG Version,
    PULONG Flags
    )
{
    IDebugClient *DebugClient;
    PDEBUG_CONTROL DebugControl;
    HRESULT hr;

    DbgPrint("DebugExtensionInitialize called.\n");

    *Version = DEBUG_EXTENSION_VERSION(1, 0);
    *Flags = 0;

    if ((hr = GetDebugClient(&DebugClient)) != S_OK)
    {
        return hr;
    }

    if ((hr = DebugClient->QueryInterface(__uuidof(IDebugControl),
                                          (void **)&DebugControl)) != S_OK)
    {
        DebugClient->Release();
        return hr;
    }

    hr = SetEventCallbacks(DebugClient);
    DbgPrint("EventCallbacks set for 0x%p returned %s.\n",
             DebugClient, pszHRESULT(hr));
    // hr is intentionally ignored.

    // Oppurtunity for consumer customization
    hr = OnExtensionInitialize(DebugClient);

    DebugControl->Release();
    DebugClient->Release();
    return hr;
}


//PDEBUG_EXTENSION_NOTIFY
extern "C"
void
CALLBACK
DebugExtensionNotify(
    ULONG Notify,
    ULONG64 /*Argument*/
    )
{
    switch (Notify) {
        case DEBUG_NOTIFY_SESSION_ACTIVE:
            DbgPrint("DebugExtensionNotify recieved DEBUG_NOTIFY_SESSION_ACTIVE\n");
            break;
        case DEBUG_NOTIFY_SESSION_INACTIVE:
            DbgPrint("DebugExtensionNotify recieved DEBUG_NOTIFY_SESSION_INACTIVE\n");
            break;
        case DEBUG_NOTIFY_SESSION_ACCESSIBLE:
            DbgPrint("DebugExtensionNotify recieved DEBUG_NOTIFY_SESSION_ACCESSIBLE\n");
            break;
        case DEBUG_NOTIFY_SESSION_INACCESSIBLE:
            DbgPrint("DebugExtensionNotify recieved DEBUG_NOTIFY_SESSION_INACCESSIBLE\n");
            break;
        default:
            DbgPrint("DebugExtensionNotify recieved unknown notification %u\n", Notify);
            break;
    }

    //
    // The first time we actually connect to a target, get the architecture
    //

    if ((Notify == DEBUG_NOTIFY_SESSION_ACCESSIBLE) && (!Connected))
    {
        IDebugClient *DebugClient;
        PDEBUG_CONTROL DebugControl;
        HRESULT hr;

       if ((hr = GetDebugClient(&DebugClient)) == S_OK)
        {
            //
            // Get the architecture type.
            //

            if ((hr = DebugClient->QueryInterface(__uuidof(IDebugControl),
                                       (void **)&DebugControl)) == S_OK)
            {
                if ((hr = DebugControl->GetActualProcessorType(
                                             &TargetMachine)) == S_OK)
                {
                    Connected = TRUE;
                }

                ULONG Qualifier;
                if ((hr = DebugControl->GetDebuggeeType(&TargetClass, &Qualifier)) != S_OK)
                {
                    TargetClass = DEBUG_CLASS_UNINITIALIZED;
                }

                if ((hr = DebugControl->GetSystemVersion(&PlatformId, &MajorVer,
                                                         &MinorVer, NULL,
                                                         0, NULL,
                                                         &SrvPack,
                                                         NULL, 0, NULL)) == S_OK)
                {
                    BuildNo = MinorVer;
                }
                else
                {
                    PlatformId = static_cast<UINT>(-1);
                    MajorVer = 0;
                    MinorVer = 0;
                    SrvPack = 0;
                    BuildNo = 0;
                }

                DebugControl->Release();
            }

            // Try to initialize symbols only if the event monitor
            // hasn't fully registered.  This indicates that the
            // extension is just being loaded as opposed to being
            // loaded at system boot and reconnect (when GDI modules
            // won't even be loaded yet).
            if (UniqueTargetState == INVALID_UNIQUE_STATE)
            {
                SymbolInit(DebugClient);
            }

            DebugClient->Release();
        }
    }


    if (Notify == DEBUG_NOTIFY_SESSION_INACTIVE)
    {
        Connected = FALSE;
        TargetMachine = 0;
        PlatformId = static_cast<UINT>(-1);
        MajorVer = 0;
        MinorVer = 0;
        SrvPack = 0;
    }

    return;
}

//PDEBUG_EXTENSION_UNINITIALIZE
extern "C"
void
CALLBACK
DebugExtensionUninitialize(void)
{
    DbgPrint("DebugExtensionUninitialize called.\n");

    // Oppurtunity for consumer customization
    OnExtensionUninitialize();

    ReleaseEventCallbacks(NULL);

    return;
}




HRESULT GetModuleParameters(
    __inout PDEBUG_CLIENT Client,
    __out ModuleParameters *Module,
    BOOL TryReload
    )
{
    HRESULT         hr;
    PDEBUG_SYMBOLS  Symbols;
    OutputControl   OutCtl(Client);

    if (Client == NULL) return E_POINTER;

    if ((hr = Client->QueryInterface(__uuidof(IDebugSymbols),
                                    (void **)&Symbols)) != S_OK)
    {
        return hr;
    }

    hr = Symbols->GetModuleByModuleName(Module->Name, 0, &Module->Index, &Module->Base);

    Client->FlushCallbacks();

    if (hr != S_OK && TryReload)
    {
        CHAR ReloadArgs[MAX_PATH];

        OutCtl.OutVerb("GetModuleByModuleName returned %s.\n", pszHRESULT(hr));

        StringCchPrintfA(ReloadArgs, ARRAYSIZE(ReloadArgs),
                (Module->Base != 0) ? "%s.%s=0x%I64x" : "%s.%s",
                Module->Name, Module->Ext, Module->Base);

        OutCtl.OutWarn("Trying %s reload.\n", ReloadArgs);

        hr = Symbols->Reload(ReloadArgs);

        Client->FlushCallbacks();

        if (hr == S_OK)
        {
            hr = Symbols->GetModuleByModuleName(Module->Name, 0, &Module->Index, &Module->Base);
            OutCtl.OutVerb("Module %s @ 0x%p; HRESULT %s\n", Module->Name, Module->Base, pszHRESULT(hr));

            Client->FlushCallbacks();
        }
        else
        {
            OutCtl.OutWarn("Reload(\"%s\") returned %s\n", ReloadArgs, pszHRESULT(hr));
        }
    }
    else
    {
        OutCtl.OutVerb("Module %s @ 0x%p.\n", Module->Name, Module->Base);
    }

    if (hr == S_OK)
    {
        hr = Symbols->GetModuleParameters(1,
                                          NULL,
                                          Module->Index,
                                          &Module->DbgModParams);

        OutCtl.OutVerb("SymbolType for %s: ", Module->Name);
        switch (Module->DbgModParams.SymbolType)
        {
            case DEBUG_SYMTYPE_NONE: OutCtl.OutVerb("NONE"); break;
            case DEBUG_SYMTYPE_COFF: OutCtl.OutVerb("COFF"); break;
            case DEBUG_SYMTYPE_CODEVIEW: OutCtl.OutVerb("CODEVIEW"); break;
            case DEBUG_SYMTYPE_PDB: OutCtl.OutVerb("PDB"); break;
            case DEBUG_SYMTYPE_EXPORT: OutCtl.OutVerb("EXPORT"); break;
            case DEBUG_SYMTYPE_DEFERRED: OutCtl.OutVerb("DEFERRED"); break;
            case DEBUG_SYMTYPE_SYM: OutCtl.OutVerb("SYM"); break;
            case DEBUG_SYMTYPE_DIA: OutCtl.OutVerb("DIA"); break;
            default:
                OutCtl.OutVerb("unknown %ld", Module->DbgModParams.SymbolType);
                break;
        }
        OutCtl.OutVerb(" (HRESULT %s)\n", pszHRESULT(hr));

        Client->FlushCallbacks();
    }

    Symbols->Release();

    return hr;
}


HRESULT
SymbolLoad(
    PDEBUG_CLIENT Client
    )
{
    HRESULT hr = S_OK;
    UNREFERENCED_PARAMETER(Client);
    
    OutputControl   OutCtl(Client);    

    OutCtl.Output("Attempting to load module: %s.%s... ", UM_Module.Name, UM_Module.Ext);

    hr = GetModuleParameters(Client, &UM_Module, FALSE);

    if (hr == S_OK)
    {
        gbSymbolsNotLoaded = FALSE;
        OutCtl.Output("success!\n");
    }
    else
    {
        OutCtl.Output("failed!\n");
    }

    if (Type_Module.Base == 0)
    {
        Type_Module = UM_Module;
    }

    DbgPrint("Using %s for type module.\n", Type_Module.Name);
    
    return hr;
}


HRESULT SymbolInit(PDEBUG_CLIENT Client)
{
    HRESULT hr = S_OK;
    UNUSED_PARAMETER(Client);

    UM_Module.Base = 0;
    Type_Module.Base = 0;



    UM_Module = wpfgfx_v0400_Module;
    hr = SymbolLoad(Client);    

    if (FAILED(hr))
    {
        UM_Module = wpfgfx_v0300_Module;
        hr = SymbolLoad(Client);
    }
    if (FAILED(hr))
    {
        UM_Module = Milcore_Module;
        hr = SymbolLoad(Client);
    }
    if (FAILED(hr))
    {
        OutputControl   OutCtl(Client);    
        OutCtl.Output("Could not find any known WPF graphics modules to debug. This extension can only be used after the WPF graphics library has been loaded\n");
    }
    

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:  GetTypeId
//
//  Synopsis:  
//

HRESULT
GetTypeId(
    __inout PDEBUG_CLIENT Client,
    __in PCSTR Type,
    __out PULONG TypeId,
    __out_opt PULONG64 Module
    )
{
    HRESULT         hr;
    PDEBUG_SYMBOLS  Symbols;

    if (Client == NULL || Type == NULL || TypeId == NULL)
    {
        return E_INVALIDARG;
    }

    if ((hr = Client->QueryInterface(__uuidof(IDebugSymbols),
                                     (void **)&Symbols)) != S_OK)
    {
        return hr;
    }

    if (strchr(Type, '!') == NULL &&
        Type_Module.Base != 0 &&
        (hr = Symbols->GetTypeId(Type_Module.Base, Type, TypeId)) == S_OK)
    {
        if (Module != NULL)
        {
            *Module = Type_Module.Base;
        }
    }
    else
    {
        hr = Symbols->GetSymbolTypeId(Type, TypeId, Module);
    }

    Symbols->Release();

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:  Evaluate
//
//  Synopsis:  
//

const CHAR szNULL[] = "(null)";
DEBUG_VALUE DbgValNULL = { 0, 0, DEBUG_VALUE_INT64 };

HRESULT
Evaluate(
    __inout PDEBUG_CLIENT Client,
    __in PCSTR Expression,
    ULONG DesiredType,
    ULONG Radix,
    __out PDEBUG_VALUE Value,
    __out_opt PULONG RemainderIndex,
    __out_opt PULONG StartIndex,
    FLONG Flags
    )
{
    HRESULT         hr = S_FALSE;
    PDEBUG_CONTROL  Control;
    BOOL            FoundNULL = FALSE;
    ULONG           OrgRadix = 0;
    // Use unsigned char type for compatibility with is* routines
    __nullterminated const UCHAR *pStr;
    __nullterminated UCHAR EvalBuffer[128];
    ULONG           EvalLen = 0;

    if (RemainderIndex != NULL) *RemainderIndex = 0;
    if (StartIndex != NULL) *StartIndex = 0;

    if (Expression == NULL ||
        Client == NULL ||
        (hr = Client->QueryInterface(__uuidof(IDebugControl),
                                     (void **)&Control)) != S_OK)
    {
        return hr;
    }

    pStr = reinterpret_cast<const UCHAR *>(Expression);

    while (*pStr != '\n' && (isspace(*pStr) || (*pStr != '-' && ispunct(*pStr))))
    {
        if (_strnicmp(reinterpret_cast<PCSTR>(pStr), szNULL, sizeof(szNULL)-1) == 0)
        {
            FoundNULL = TRUE;
            break;
        }

        pStr++;
    }

    if (FoundNULL)
    {
        hr = Control->CoerceValue(&DbgValNULL,
                                  (DesiredType == DEBUG_VALUE_INVALID) ?
                                  DEBUG_VALUE_INT64 : DesiredType,
                                  Value);
        EvalLen = sizeof(szNULL)-1;
    }
    else
    {
        // Find expression string and only text revalent
        // to evalutating that expression.
        //
        // Otherwise IDebugControl::Evaluate will spend
        // too much time looking up values that are not
        // really part of the expression.
        //
        // IDebugControl::Evaluate also doesn't handle
        // binary strings well.  We expect binary strings
        // to be followed by a non-binary value enclosed
        // in parenthesis.  Just use that value.

        const UCHAR *psz;
        UINT i = 0;

        while (pStr[i] != '\0' &&
               (pStr[i] == '0' || pStr[i] == '1'))
        {
            i++;
        }

        if (i &&
            pStr[i] == ' ' &&
            pStr[i+1] == '(' &&
            isdigit(pStr[i+2]))
        {
            pStr += i + 1;
        }

        psz = pStr;
        i = 0;

        if (Flags & EVALUATE_COMPACT_EXPR)
        {
            while ((i < sizeof(EvalBuffer)-1) &&
                   *psz != '\0' && !isspace(*psz))
            {
                EvalBuffer[i++] = *psz++;
            }
        }
        else
        {
            do
            {
                while ((i < sizeof(EvalBuffer)-1) &&
                       *psz != '\0' && !isspace(*psz))
                {
                    EvalBuffer[i++] = *psz++;
                }
                while ((i < sizeof(EvalBuffer)-1) &&
                    (*psz == ' ' || *psz == '\t'))
                {
                    EvalBuffer[i++] = *psz++;
                }
            } while ((i < sizeof(EvalBuffer)-1) &&
                     (ispunct(*psz) && *psz != '-' && *psz != '_' &&
                      !(psz[0] == '-' && psz[1] == '>')));

            // Remove any trailing whitespace
            while (i > 0 && isspace(EvalBuffer[i-1])) i--;
        }

        EvalBuffer[i] = '\0';

        if (Radix == 0 ||
                 ((hr = Control->GetRadix(&OrgRadix)) == S_OK &&
                  (hr = Control->SetRadix(Radix)) == S_OK)
            )
        {
//            DbgPrint("Calling Eval(%s) --\n", EvalBuffer);
            hr = Control->Evaluate(reinterpret_cast<PCSTR>(EvalBuffer), 
                                   DesiredType,
                                   Value,
                                   &EvalLen);
//            DbgPrint("-- Eval returned\n");

            if (Radix != 0)
            {
                Control->SetRadix(OrgRadix);
            }

            if (hr == S_OK &&
                Flags & EVALUATE_COMPACT_EXPR &&
                EvalLen != i)
            {
                hr = S_FALSE;
            }
        }
        else
        {
            DbgPrint("Can't setup new radix, %lu, for Evaluate.\n", Radix);
        }
    }

    Control->Release();

    if (hr == S_OK)
    {
        if (RemainderIndex != NULL)
        {
            *RemainderIndex = (ULONG)(reinterpret_cast<PCSTR>(pStr) - Expression) + EvalLen;
        }

        if (StartIndex != NULL)
        {
            *StartIndex = (ULONG)(reinterpret_cast<PCSTR>(pStr) - Expression);
        }
    }

    return hr;
}


HRESULT
InitAPI(__inout PDEBUG_CLIENT Client, __in PCSTR ExtName)
{
    static BOOL SecondaryCall = FALSE;

    HRESULT hr;

    hr = EventCallbacksReady(Client);

    if (hr != S_OK)
    {
        OutputControl   OutCtl(Client);

        OutCtl.OutWarn(" Warning: Event callbacks have not been registered.\n");

        if (SecondaryCall)
        {
            OutCtl.OutWarn("   All extension caching is disabled.\n");
        }
        else
        {
            OutCtl.OutWarn("   If %s is the first extension used, use .load or !load in the future.\n"
                           "   Caching is disabled for this use of !%s.\n",
                           ExtName, ExtName);
        }
    }

    SecondaryCall = TRUE;

    if (gbSymbolsNotLoaded)
    {
        SymbolInit(Client);
    }

    return hr;
}


DECLARE_API(reinit)
{
    UNREFERENCED_PARAMETER(args);

    HRESULT hr;
    hr = SymbolInit(Client);
    return hr;
}






