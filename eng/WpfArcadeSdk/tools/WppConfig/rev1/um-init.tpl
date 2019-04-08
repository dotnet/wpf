`**********************************************************************`
`* This is an include template file for tracewpp preprocessor.        *`
`*                                                                    *`
`*    Copyright (c) Microsoft Corporation. All Rights Reserved.       *`
`**********************************************************************`

// template `TemplateFile`
//
//     Defines a set of functions that simplifies
//     User mode registration for tracing
//

#if defined(__cplusplus)
extern "C" {
#endif

// define annotation record that will carry control information to pdb (in case somebody needs it)
WPP_FORCEINLINE void WPP_CONTROL_ANNOTATION() {
#if !defined(WPP_NO_ANNOTATIONS)
#if !defined(WPP_ANSI_ANNOTATION) 
#  define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) __annotation(L"TMC:", WPP_GUID_WTEXT Guid, _WPPW(WPP_STRINGIZE(Name)) Bits);
#  define WPP_DEFINE_BIT(Name) , _WPPW(#Name)
#else
#  define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) __annotation("TMC:", WPP_GUID_TEXT Guid, WPP_STRINGIZE(Name) Bits);
#  define WPP_DEFINE_BIT(Name) , #Name
#endif
    WPP_CONTROL_GUIDS 
#  undef WPP_DEFINE_BIT
#  undef WPP_DEFINE_CONTROL_GUID
#endif
}

LPCGUID WPP_REGISTRATION_GUIDS[WPP_LAST_CTL];

WPP_CB_TYPE WPP_MAIN_CB[WPP_LAST_CTL];

#define WPP_NEXT(Name) ((WPP_TRACE_CONTROL_BLOCK*) \
    (WPP_XGLUE(WPP_CTL_, WPP_EVAL(Name)) + 1 == WPP_LAST_CTL ? 0:WPP_MAIN_CB + WPP_XGLUE(WPP_CTL_, WPP_EVAL(Name)) + 1))    

__inline void WPP_INIT_CONTROL_ARRAY(WPP_CB_TYPE* Arr) {
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits)                        \
   Arr->Control.Ptr = NULL;                                            \
   Arr->Control.Next = WPP_NEXT(WPP_EVAL(Name));                       \
   Arr->Control.FlagsLen = WPP_FLAG_LEN;                               \
   Arr->Control.Level = WPP_LAST_CTL;                                  \
   Arr->Control.Options = 0;                                           \
   Arr->Control.Flags[0] = 0;                                          \
   ++Arr;
#define WPP_DEFINE_BIT(BitName) L" " L ## #BitName
WPP_CONTROL_GUIDS
#undef WPP_DEFINE_BIT
#undef WPP_DEFINE_CONTROL_GUID
}

#undef WPP_INIT_STATIC_DATA
#define WPP_INIT_STATIC_DATA WPP_INIT_CONTROL_ARRAY(WPP_MAIN_CB)

__inline void WPP_INIT_GUID_ARRAY(LPCGUID* Arr) {
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits)                         \
   WPP_XGLUE4(*Arr = &WPP_, ThisDir, _CTLGUID_, WPP_EVAL(Name));        \
   ++Arr;
WPP_CONTROL_GUIDS
#undef WPP_DEFINE_CONTROL_GUID
}


 
VOID WppInitUm(__in_opt LPCWSTR AppName);
 
#define WPP_INIT_TRACING(AppName)                                           \
                WppLoadTracingSupport;                                      \
                (WPP_CONTROL_ANNOTATION(),WPP_INIT_STATIC_DATA,             \
                 WPP_INIT_GUID_ARRAY((LPCGUID*)&WPP_REGISTRATION_GUIDS),    \
                 WPP_CB= WPP_MAIN_CB,                                       \
                 WppInitUm(AppName))


void WPP_Set_Dll_CB(
                    PWPP_TRACE_CONTROL_BLOCK Control, 
                    VOID * DllControlBlock,
                    USHORT Flags)
{

    if (*(PVOID*)DllControlBlock != DllControlBlock){
        Control->Ptr = DllControlBlock;
    } else {
        if (Flags == WPP_VER_WHISTLER_CB_FORWARD_PTR ){
            memset(Control, 0, sizeof(WPP_TRACE_CONTROL_BLOCK));
            *(PWPP_TRACE_CONTROL_BLOCK*)DllControlBlock = Control;
            Control->Options = WPP_VER_LH_CB_FORWARD_PTR;
            
        } else if (Flags == WPP_VER_WIN2K_CB_FORWARD_PTR ) {
            Control->Ptr = DllControlBlock;
        }
    }

}


#define WPP_SET_FORWARD_PTR(CTL, FLAGS, PTR) (\
    (WPP_MAIN_CB[WPP_CTRL_NO(WPP_BIT_ ## CTL )].Control.Options = (FLAGS)));\
    WPP_Set_Dll_CB(&WPP_MAIN_CB[WPP_CTRL_NO(WPP_BIT_ ## CTL )].Control,(PTR),(USHORT)FLAGS)


#define DEFAULT_LOGGER_NAME             L"stdout"

#if !defined(WppDebug)
#  define WppDebug(a,b)
#endif

#if !defined(WPPINIT_STATIC)
#  define WPPINIT_STATIC
#endif

#if !defined(WPPINIT_EXPORT)
#  define WPPINIT_EXPORT
#endif

#define WPP_GUID_FORMAT     "%08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x"
#define WPP_GUID_ELEMENTS(p) \
    p->Data1,                 p->Data2,    p->Data3,\
    p->Data4[0], p->Data4[1], p->Data4[2], p->Data4[3],\
    p->Data4[4], p->Data4[5], p->Data4[6], p->Data4[7]

#define WPP_MAX_LEVEL 255
#define WPP_MAX_FLAGS 0xFFFFFFFF





__inline TRACEHANDLE WppQueryLogger(__in_opt PCWSTR LoggerName)
{
    ULONG Status;
    EVENT_TRACE_PROPERTIES LoggerInfo;

    ZeroMemory(&LoggerInfo, sizeof(LoggerInfo));
    LoggerInfo.Wnode.BufferSize = sizeof(LoggerInfo);
    LoggerInfo.Wnode.Flags = WNODE_FLAG_TRACED_GUID;

    Status = ControlTraceW(0, LoggerName ? LoggerName : L"stdout", &LoggerInfo, EVENT_TRACE_CONTROL_QUERY);
    if (Status == ERROR_SUCCESS || Status == ERROR_MORE_DATA) {
        return (TRACEHANDLE) LoggerInfo.Wnode.HistoricalContext;
    }
    return 0;
}


#if defined (WPP_GLOBALLOGGER)

#define WPP_REG_GLOBALLOGGER_FLAGS             L"Flags"
#define WPP_REG_GLOBALLOGGER_LEVEL             L"Level"
#define WPP_REG_GLOBALLOGGER_START             L"Start"

#define WPP_TEXTGUID_LEN  38
#define WPP_REG_GLOBALLOGGER_KEY            L"SYSTEM\\CurrentControlSet\\Control\\Wmi\\GlobalLogger"

WPPINIT_STATIC
void WppIntToHex(
    __out_ecount(digits) LPWSTR Buf,
    unsigned int value,
    int digits
    )
{
    static LPCWSTR hexDigit = L"0123456789abcdef";
    while (--digits >= 0) {
        Buf[digits] = hexDigit[ value & 15 ];
        value /= 16;
    }
}

WPPINIT_EXPORT
void WppInitGlobalLogger(
        IN LPCGUID ControlGuid,
        IN PTRACEHANDLE LoggerHandle,
        OUT PULONG Flags,
        __out_ecount(sizeof(UCHAR)) PUCHAR Level )
{
WCHAR    GuidBuf[WPP_TEXTGUID_LEN];
ULONG    CurrentFlags = 0;
ULONG    CurrentLevel = 0;
DWORD    Start = 0;
DWORD    DataSize ;
ULONG    Status ;
HKEY     GloblaLoggerHandleKey;
HKEY     ValueHandleKey ;



   WppDebug(0,("WPP checking Global Logger %S",WPP_REG_GLOBALLOGGER_KEY));

   if ((Status = RegOpenKeyExW(HKEY_LOCAL_MACHINE,
                        (LPWSTR)WPP_REG_GLOBALLOGGER_KEY,
                        0,
                        KEY_READ,
                        &GloblaLoggerHandleKey
                        )) != ERROR_SUCCESS) {
       WppDebug(0,("GlobalLogger key does not exist (0x%08X)",Status));
       return ;
   }

   DataSize = sizeof(DWORD);
   Status = RegQueryValueExW(GloblaLoggerHandleKey,
                             (LPWSTR)WPP_REG_GLOBALLOGGER_START,
                             0,
                             NULL,
                             (LPBYTE)&Start,
                             &DataSize);
    if (Status != ERROR_SUCCESS || Start == 0 ) {
        WppDebug(0,("Global Logger not started (0x%08X)",Status));
        goto Cleanup;
    }


   WppDebug(0,("Global Logger exists and is set to be started"));

   {
        static LPCWSTR hexDigit = L"0123456789abcdef";
        int i;

        WppIntToHex(GuidBuf, ControlGuid->Data1, 8);
        GuidBuf[8]  = '-';

        WppIntToHex(&GuidBuf[9], ControlGuid->Data2, 4);
        GuidBuf[13] = '-';

        WppIntToHex(&GuidBuf[14], ControlGuid->Data3, 4);
        GuidBuf[18] = '-';

        GuidBuf[19] =  hexDigit[(ControlGuid->Data4[0] & 0xF0) >> 4];
        GuidBuf[20] =  hexDigit[ControlGuid->Data4[0] & 0x0F ];
        GuidBuf[21] =  hexDigit[(ControlGuid->Data4[1] & 0xF0) >> 4];
        GuidBuf[22] =  hexDigit[ControlGuid->Data4[1] & 0x0F ];
        GuidBuf[23] = '-';

        for( i=2; i < 8 ; i++ ){
            GuidBuf[i*2+20] =  hexDigit[(ControlGuid->Data4[i] & 0xF0) >> 4];
            GuidBuf[i*2+21] =  hexDigit[ControlGuid->Data4[i] & 0x0F ];
        }
        GuidBuf[36] = 0;

    }

   //
   // Perform the query
   //

   if ((Status = RegOpenKeyExW(GloblaLoggerHandleKey,
                        (LPWSTR)GuidBuf,
                        0,
                        KEY_READ,
                        &ValueHandleKey
                        )) != ERROR_SUCCESS) {
       WppDebug(0,("Global Logger Key not set for this Control Guid %S (0x%08X)",GuidBuf,Status));
       goto Cleanup;
   }
   // Get the Flags Parameter
   DataSize = sizeof(DWORD);
   Status = RegQueryValueExW(ValueHandleKey,
                             (LPWSTR)WPP_REG_GLOBALLOGGER_FLAGS,
                             0,
                             NULL,
                             (LPBYTE)&CurrentFlags,
                             &DataSize);
    if (Status != ERROR_SUCCESS || CurrentFlags == 0 ) {
        WppDebug(0,("GlobalLogger for %S Flags not set (0x%08X)",GuidBuf,Status));
    }
   // Get the levels Parameter
   DataSize = sizeof(DWORD);
   Status = RegQueryValueExW(ValueHandleKey,
                             (LPWSTR)WPP_REG_GLOBALLOGGER_LEVEL,
                             0,
                             NULL,
                             (LPBYTE)&CurrentLevel,
                             &DataSize);
    if (Status != ERROR_SUCCESS || CurrentLevel == 0 ) {
        WppDebug(0,("GlobalLogger for %S Level not set (0x%08X)",GuidBuf,Status));
    }

    if (Start==1) {

       if ((*LoggerHandle= WppQueryLogger( L"GlobalLogger")) != (TRACEHANDLE)NULL) {
           *Flags = CurrentFlags & 0x7FFFFFFF ;
           *Level = (UCHAR)(CurrentLevel & 0xFF) ;
           WppDebug(0,("WPP Enabled via Global Logger Flags=0x%08X Level=0x%02X",CurrentFlags,CurrentLevel));
       } else {
           WppDebug(0,("GlobalLogger set for start but not running (Flags=0x%08X Level=0x%02X)",CurrentFlags,CurrentLevel));
       }

    }

   RegCloseKey(ValueHandleKey);
Cleanup:
   RegCloseKey(GloblaLoggerHandleKey);
}
#endif  //#ifdef WPP_GLOBALLOGGER

#ifdef WPP_MANAGED_CPP
#pragma managed(push, off)
#endif 

ULONG
WINAPI
WppControlCallback(
    IN WMIDPREQUESTCODE RequestCode,
    IN PVOID Context,
    __inout ULONG *InOutBufferSize,
    __inout PVOID Buffer
    )
{
    PWPP_TRACE_CONTROL_BLOCK Ctx = (PWPP_TRACE_CONTROL_BLOCK)Context;
    TRACEHANDLE Logger;
    UCHAR Level;
    DWORD Flags;

    *InOutBufferSize = 0;

    switch (RequestCode)
    {
    case WMI_ENABLE_EVENTS:
        {
            Logger = GetTraceLoggerHandle( Buffer );
            Level = GetTraceEnableLevel(Logger);
            Flags = GetTraceEnableFlags(Logger);

            WppDebug(1, ("[WppInit] WMI_ENABLE_EVENTS Ctx %p Flags %x"
                     " Lev %d Logger %I64x\n",
                     Ctx, Flags, Level, Logger) );
            break;
        }
    case WMI_DISABLE_EVENTS:
        {
            Logger = 0;
            Flags  = 0;
            Level  = 0;
            WppDebug(1, ("[WppInit] WMI_DISABLE_EVENTS Ctx 0x%08p\n", Ctx));
            break;
        }
    default:
        {
            return(ERROR_INVALID_PARAMETER);
        }
    }
    if (Ctx->Options & WPP_VER_WIN2K_CB_FORWARD_PTR && Ctx->Win2kCb) {
        Ctx->Win2kCb->Logger = Logger;
        Ctx->Win2kCb->Level  = Level;
        Ctx->Win2kCb->Flags  = Flags;
    } else {
        if (Ctx->Options & WPP_VER_WHISTLER_CB_FORWARD_PTR && Ctx->Cb) {
            Ctx = Ctx->Cb; // use forwarding address
        }
        Ctx->Logger   = Logger;
        Ctx->Level    = Level;
        Ctx->Flags[0] = Flags;
        
    }
    return(ERROR_SUCCESS);
}

#ifdef WPP_MANAGED_CPP
#pragma managed(pop)
#endif 


WPPINIT_EXPORT
VOID WppInitUm(__in_opt LPCWSTR AppName)
{
    PWPP_TRACE_CONTROL_BLOCK Control = &WPP_CB[0].Control;
    TRACE_GUID_REGISTRATION TraceRegistration;
    LPCGUID *               RegistrationGuids = (LPCGUID *)&WPP_REGISTRATION_GUIDS;
    LPCGUID                 ControlGuid;

    ULONG Status;

#ifdef WPP_MOF_RESOURCENAME
#ifdef WPP_DLL
    HMODULE hModule = NULL;
#endif
    WCHAR ImagePath[MAX_PATH] = {UNICODE_NULL} ;
    WCHAR WppMofResourceName[] = WPP_MOF_RESOURCENAME ;
#else
    UNREFERENCED_PARAMETER(AppName);
#endif //#ifdef WPP_MOF_RESOURCENAME

    WppDebug(1, ("Registering %ws\n", AppName) );

    for(; Control; Control = Control->Next) {

        ControlGuid = *RegistrationGuids++;
        TraceRegistration.Guid = ControlGuid;
        TraceRegistration.RegHandle = 0;

        WppDebug(1,(WPP_GUID_FORMAT " %ws : %d\n",
                    WPP_GUID_ELEMENTS(ControlGuid),
                    AppName,
                    Control->FlagsLen));
                    

#ifdef WPP_MOF_RESOURCENAME
        if (AppName != NULL) {
           DWORD Status ;
#ifdef WPP_DLL
           if ((hModule = GetModuleHandleW(AppName)) != NULL) {
               Status = GetModuleFileNameW(hModule, ImagePath, MAX_PATH) ;
               ImagePath[MAX_PATH-1] = '\0';
               if (Status == 0) {
                  WppDebug(1,("RegisterTraceGuids => GetModuleFileName(DLL) Failed 0x%08X\n",GetLastError()));
               }
           } else {
               WppDebug(1,("RegisterTraceGuids => GetModuleHandleW failed for %ws (0x%08X)\n",AppName,GetLastError()));
           }
#else   // #ifdef WPP_DLL
           Status = GetModuleFileNameW(NULL,ImagePath,MAX_PATH);
           if (Status == 0) {
               WppDebug(1,("GetModuleFileName(EXE) Failed 0x%08X\n",GetLastError()));
           }
#endif  //  #ifdef WPP_DLL
        }
        WppDebug(1,("registerTraceGuids => registering with WMI, App=%ws, Mof=%ws, ImagePath=%ws\n",AppName,WppMofResourceName,ImagePath));

        Status = RegisterTraceGuidsW(                   // Always use Unicode
#else   // ifndef WPP_MOF_RESOURCENAME

        Status = RegisterTraceGuids(
#endif  // ifndef WPP_MOF_RESOURCENAME

            WppControlCallback,
            Control,              // Context for the callback
            ControlGuid,
            1,
            &TraceRegistration,
#ifndef WPP_MOF_RESOURCENAME
            0, //ImagePath,
            0, //ResourceName,
#else   // #ifndef WPP_MOF_RESOURCENAME
            ImagePath,
            WppMofResourceName,
#endif // #ifndef WPP_MOF_RESOURCENAME
            &Control->UmRegistrationHandle
        );

        WppDebug(1, ("RegisterTraceGuid => %d\n", Status) );
#if defined (WPP_GLOBALLOGGER)
        // Check if Global logger is active if we have not been immediately activated

        if (Control->Logger == (TRACEHANDLE)NULL) {          
            WppInitGlobalLogger( ControlGuid, (PTRACEHANDLE)&Control->Logger, &Control->Flags[0], &Control->Level);
        }
#endif  //#if defined (WPP_GLOBALLOGGER) 

    }

    
}

WPPINIT_EXPORT
VOID WppCleanupUm(    VOID   )
{
    PWPP_TRACE_CONTROL_BLOCK Control; 

    if (WPP_CB == (WPP_CB_TYPE*)&WPP_CB){
        //
        // WPP_INIT_TRACING macro has not been called
        //
        return;
    }
    WppDebug(1, ("Cleanup\n") );
    Control = &WPP_CB[0].Control;
    for(; Control; Control = Control->Next) {
        WppDebug(1,("UnRegistering %I64x\n", Control->UmRegistrationHandle) );
        if (Control->UmRegistrationHandle) {
            UnregisterTraceGuids(Control->UmRegistrationHandle);
            Control->UmRegistrationHandle = (TRACEHANDLE)NULL ;
        }
    }
    
    WPP_CB = (WPP_CB_TYPE*)&WPP_CB;
}

#if defined(__cplusplus)
};
#endif
