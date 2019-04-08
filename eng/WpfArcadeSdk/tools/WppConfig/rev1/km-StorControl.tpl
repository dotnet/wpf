`**********************************************************************`
`* This is an include template file for tracewpp preprocessor.        *`
`*                                                                    *`
`*    Copyright 1999-2000 Microsoft Corporation. All Rights Reserved. *`
`**********************************************************************`

// template `TemplateFile`
//
//     Defines a set of macro that expand control model specified
//     with WPP_CONTROL_GUIDS (example shown below)
//     into an enum of trace levels and required structures that
//     contain the mask of levels, logger handle and some information
//     required for registration.
//

///////////////////////////////////////////////////////////////////////////////////
//
// #define WPP_CONTROL_GUIDS \
//     WPP_DEFINE_CONTROL_GUID(Regular,(81b20fea,73a8,4b62,95bc,354477c97a6f), \
//       WPP_DEFINE_BIT(Error)      \
//       WPP_DEFINE_BIT(Unusual)    \
//       WPP_DEFINE_BIT(Noise)      \
//    )        \
//    WPP_DEFINE_CONTROL_GUID(HiFreq,(91b20fea,73a8,4b62,95bc,354477c97a6f), \
//       WPP_DEFINE_BIT(Entry)      \
//       WPP_DEFINE_BIT(Exit)       \
//       WPP_DEFINE_BIT(ApiCalls)   \
//       WPP_DEFINE_BIT(RandomJunk) \
//       WPP_DEFINE_BIT(LovePoem)   \
//    )        


#if !defined(WPP_NO_CONTROL_GUIDS)

#if defined(WPP_DEFAULT_CONTROL_GUID)
#  if defined(WPP_CONTROL_GUIDS)
#     pragma message(__FILE__ " : error : WPP_DEFAULT_CONTROL_GUID cannot be used together with WPP_CONTROL_GUIDS")
#     stop
#  else
#     define WPP_CONTROL_GUIDS \
         WPP_DEFINE_CONTROL_GUID(Default,(WPP_DEFAULT_CONTROL_GUID), \
         WPP_DEFINE_BIT(Error)      \
         WPP_DEFINE_BIT(Unusual)    \
         WPP_DEFINE_BIT(Noise)      \
      )
#  endif      
#endif

#if !defined(WPP_CONTROL_GUIDS)
#  pragma message(__FILE__ " : error : Please define control model via WPP_CONTROL_GUIDS or WPP_DEFAULT_CONTROL_GUID macros")
#  pragma message(__FILE__ " : error : don't forget to call WPP_INIT_TRACING and WPP_CLEANUP in your main, DriverEntry or DllInit")
#  pragma message(__FILE__ " : error : see tracewpp.doc for further information")
stop.
#endif
// a set of macro to convert a guid in a form x(81b20fea,73a8,4b62,95bc,354477c97a6f)
// into either a a struct or text string

#define _WPPW(x) L ## x

#define WPP_GUID_TEXT(l,w1,w2,w3,ll) #l "-" #w1 "-" #w2 "-" #w3 "-" #ll
#define WPP_GUID_WTEXT(l,w1,w2,w3,ll) _WPPW(#l) L"-" _WPPW(#w1) L"-" _WPPW(#w2) L"-" _WPPW(#w3) L"-" _WPPW(#ll)
#define WPP_EXTRACT_BYTE(val,n) ( ((0x ## val) >> (8 * n)) & 0xFF )
#define WPP_GUID_STRUCT(l,w1,w2,w3,ll) {0x ## l, 0x ## w1, 0x ## w2,\
     {WPP_EXTRACT_BYTE(w3, 1), WPP_EXTRACT_BYTE(w3, 0), \
      WPP_EXTRACT_BYTE(ll, 5), WPP_EXTRACT_BYTE(ll, 4), \
      WPP_EXTRACT_BYTE(ll, 3), WPP_EXTRACT_BYTE(ll, 2), \
      WPP_EXTRACT_BYTE(ll, 1), WPP_EXTRACT_BYTE(ll, 0)} }

// define annotation record that will carry control intormation to pdb (in case somebody needs it)
__forceinline void WPP_CONTROL_ANNOTATION() {
#if !defined(WPP_NO_ANNOTATIONS)
#if !defined(WPP_ANSI_ANNOTATION) 
#  define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) __annotation(L"TMC:", WPP_GUID_WTEXT Guid, _WPPW(#Name) Bits);
#  define WPP_DEFINE_BIT(Name) , _WPPW(#Name)
#else
#  define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) __annotation("TMC:", WPP_GUID_TEXT Guid, #Name Bits);
#  define WPP_DEFINE_BIT(Name) , #Name
#endif
    WPP_CONTROL_GUIDS 
#  undef WPP_DEFINE_BIT
#  undef WPP_DEFINE_CONTROL_GUID
#endif
}

// define an enum of control block names
//////
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) WPP_CTL_ ## Name,
enum WPP_CTL_NAMES { WPP_CONTROL_GUIDS WPP_LAST_CTL};
#undef WPP_DEFINE_CONTROL_GUID

// define control guids
//////
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) \
extern __declspec(selectany) const GUID WPP_ ## ThisDir ## _CTLGUID_ ## Name = WPP_GUID_STRUCT Guid;
WPP_CONTROL_GUIDS
#undef WPP_DEFINE_CONTROL_GUID

// define enums of individual bits
/////
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) \
    WPP_BLOCK_START_ ## Name = WPP_CTL_ ## Name * 0x10000, Bits WPP_BLOCK_END_ ## Name, 
# define WPP_DEFINE_BIT(Name) WPP_BIT_ ## Name,
enum WPP_DEFINE_BIT_NAMES { WPP_CONTROL_GUIDS };
# undef WPP_DEFINE_BIT
#undef WPP_DEFINE_CONTROL_GUID

#define WPP_MASK(CTL)    (1 << ( ((CTL)-1) & 31 ))
#define WPP_FLAG_NO(CTL) ( (0xFFFF & ((CTL)-1) ) / 32)
#define WPP_CTRL_NO(CTL) ((CTL) >> 16)

// calculate how many DWORDs we need to get the required number of bits
// upper estimate. Sometimes will be off by one
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) | WPP_BLOCK_END_ ## Name
enum _WPP_FLAG_LEN_ENUM { WPP_FLAG_LEN = 1 | ((0 WPP_CONTROL_GUIDS) & 0xFFFF) / 32 };
#undef WPP_DEFINE_CONTROL_GUID

#ifndef WPP_CB
#  define WPP_CB      WPP_GLOBAL_Control
#endif
#ifndef WPP_CB_TYPE
#define WPP_CB_TYPE WPP_PROJECT_CONTROL_BLOCK
#endif

typedef union {
    WPP_REGISTRATION_BLOCK  Registration; // need this only to register
    WPP_TRACE_CONTROL_BLOCK Control;      
    UCHAR ReserveSpace[ sizeof(WPP_TRACE_CONTROL_BLOCK) + sizeof(ULONG) * (WPP_FLAG_LEN - 1) ];
} WPP_CB_TYPE ;

#define WPP_NEXT(Name) ((WPP_REGISTRATION_BLOCK*) \
    (WPP_CTL_ ## Name + 1 == WPP_LAST_CTL ? 0:WPP_CB + WPP_CTL_ ## Name + 1))

// WPP_CONTROL structure has two extra fields in the kernel
// mode. We will use WPPKM_NULL to initalize them

#if defined(WPP_KERNEL_MODE)
#  define WPPKM_NULL 0,
#else
#  define WPPKM_NULL
#endif

#if defined(WPP_DLL)

extern __declspec(selectany) WPP_CB_TYPE WPP_CB[WPP_LAST_CTL];

__inline void WPP_INIT_CONTROL_ARRAY(WPP_CB_TYPE* Arr) {
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) \
   Arr->Registration.Next = WPP_NEXT(Name); \
   Arr->Registration.ControlGuid = &WPP_ ## ThisDir ## _CTLGUID_ ## Name; \
   Arr->Registration.FriendlyName = L ## #Name; \
   Arr->Registration.BitNames = Bits; \
   Arr->Registration.FlagsLen = WPP_FLAG_LEN; \
   Arr->Registration.RegBlockLen = WPP_LAST_CTL; ++Arr;
#define WPP_DEFINE_BIT(BitName) L" " L ## #BitName
WPP_CONTROL_GUIDS
#undef WPP_DEFINE_BIT
#undef WPP_DEFINE_CONTROL_GUID
}
#define WPP_INIT_STATIC_DATA WPP_INIT_CONTROL_ARRAY(WPP_CB)

#else

extern __declspec(selectany) WPP_CB_TYPE WPP_CB[WPP_LAST_CTL] = {
#define WPP_DEFINE_CONTROL_GUID(Name,Guid,Bits) {{WPPKM_NULL WPP_NEXT(Name), \
    &WPP_ ## ThisDir ## _CTLGUID_ ## Name, L ## #Name, Bits, WPPKM_NULL WPP_FLAG_LEN, WPP_LAST_CTL}},
#define WPP_DEFINE_BIT(BitName) L" " L ## #BitName
WPP_CONTROL_GUIDS
#undef WPP_DEFINE_BIT
#undef WPP_DEFINE_CONTROL_GUID
};
#define WPP_INIT_STATIC_DATA 0

#endif

#define WPP_CONTROL(CTL) (WPP_CB[WPP_CTRL_NO(CTL)].Control)
#define WPP_REGISTRATION(CTL) (WPP_CB[WPP_CTRL_NO(CTL)].Registration)

#define WPP_SET_FORWARD_PTR(CTL, FLAGS, PTR) (\
    (WPP_REGISTRATION(WPP_BIT_ ## CTL ).Options = (FLAGS)),\
    (WPP_REGISTRATION(WPP_BIT_ ## CTL ).Ptr = (PTR)) )

#if !defined(WPP_LEVEL_LOGGER)
#  define WPP_LEVEL_LOGGER(CTL)  (WPP_CONTROL(WPP_BIT_ ## CTL).Logger), 
#endif

#if !defined(WPP_LEVEL_ENABLED)
#  define WPP_LEVEL_ENABLED(CTL) (WPP_CONTROL(WPP_BIT_ ## CTL).Flags[WPP_FLAG_NO(WPP_BIT_ ## CTL)] & WPP_MASK(WPP_BIT_ ## CTL)) 
#endif

#if !defined(WPP_LOGGER_ARG)
#  define WPP_LOGGER_ARG TRACEHANDLE Logger,
#endif

#if !defined(WPP_GET_LOGGER)
#  define WPP_GET_LOGGER Logger
#endif

#ifndef WPP_ENABLED
#  define WPP_ENABLED() 1
#endif
#ifndef WPP_LOGGER
#  define WPP_LOGGER() (WPP_CB[0].Control.Logger),
#endif

#endif // WPP_NO_CONTROL_GUIDS


#if defined(__cplusplus)
extern "C" {
#endif
#if !defined(WPP_TRACE_W2K_COMPATABILITY) 
    VOID WppInitKm(PVOID DrvContext, PVOID InitInfo, PWPP_REGISTRATION_BLOCK Registration);
    VOID WppCleanupKm(PVOID TraceContext, PWPP_REGISTRATION_BLOCK Registration);
#else  // #if !defined(WPP_TRACE_W2K_COMPATABILITY)
        // W2K Compatable versions
    VOID WppInitKm(PDEVICE_OBJECT pDevObj, PUNICODE_STRING RegistryPath, PWPP_REGISTRATION_BLOCK Registration);
    VOID WppCleanupKm(PDEVICE_OBJECT pDeviceObject);
#endif // #if !defined(WPP_TRACE_W2K_COMPATABILITY)

#if defined(__cplusplus)
};
#endif

#if !defined(WPP_TRACE_W2K_COMPATABILITY)
#  define WPP_INIT_TRACING(DrvObj, Regpath, InitInfo) (WPP_CONTROL_ANNOTATION(),WPP_INIT_STATIC_DATA,WppInitKm(DrvObj, InitInfo, &WPP_CB[0].Registration))                                       

#  define WPP_CLEANUP(DrvObj, TraceContext) WppCleanupKm(TraceContext, &WPP_CB[0].Registration)

#else  // #if !defined(WPP_TRACE_W2K_COMPATABILITY)
#  define WPP_INIT_TRACING(DevObj, RegPath) (WPP_CONTROL_ANNOTATION(),WPP_INIT_STATIC_DATA,WppInitKm(DevObj, RegPath, &WPP_CB[0].Registration))                                   

#  define WPP_CLEANUP(DevObj) WppCleanupKm(DevObj)

NTSTATUS
WppTraceCallback(
    IN UCHAR minorFunction,
    IN PVOID DataPath,
    IN ULONG BufferLength,
    IN PVOID Buffer,
    IN PVOID Context,
    OUT PULONG Size
    ) ;

#define WPP_TRACE_CONTROL(Function,Buffer,BufferSize,ReturnSize) WppTraceCallback(Function,NULL,BufferSize,Buffer,&WPP_CB[0],&ReturnSize);

#endif // #if !defined(WPP_TRACE_W2K_COMPATABILITY)


