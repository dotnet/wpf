`**********************************************************************`
`* This is an include template file for tracewpp preprocessor.        *`
`*                                                                    *`
`*    Copyright 1999-2000 Microsoft Corporation. All Rights Reserved. *`
`**********************************************************************`

// template `TemplateFile`

`* Dump the definitions specified via -D on the command line to WPP *`

`FORALL def IN MacroDefinitions`
#define `def.Name` `def.Alias`
`ENDFOR`

#define WPP_THIS_FILE `SourceFile.CanonicalName`

#include <stddef.h>
#include <stdarg.h>
#include <wmistr.h>

#if defined(WPP_TRACE_W2K_COMPATABILITY)
DEFINE_GUID(WPP_TRACE_CONTROL_NULL_GUID, 0x00000000L, 0x0000, 0x0000, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
#endif

#if defined(__cplusplus)
extern "C" {
#endif

ULONG
__inline
NullTraceFunc (
    IN ULONG64  LoggerHandle,
    IN ULONG   MessageFlags,
    IN LPGUID  MessageGuid,
    IN USHORT  MessageNumber,
    IN ...
    )
{
   return 0;
}
       
__declspec(selectany) PFN_WPPTRACEMESSAGE    pfnWppTraceMessage = NullTraceFunc;

#if defined(__cplusplus)
};
#endif

#if !defined(_NTRTL_) 
      // fake RTL_TIME_ZONE_INFORMATION //
    typedef int RTL_TIME_ZONE_INFORMATION;
#   define _WMIKM_
#endif
#ifndef WPP_TRACE
#define WPP_TRACE pfnWppTraceMessage
#endif

#ifndef WPP_OLDCC
#define WPP_OLDCC
#endif

///////////////////////////////////////////////////////////////////////////////
//
// B O R R O W E D  D E F I N I T I O N S
//
///////////////////////////////////////////////////////////////////////////////

# define WPP_LOGGER_ARG ULONG64 Logger,

#if ((_MSC_VER >= 800) || defined(_STDCALL_SUPPORTED)) && !defined(_M_AMD64)
#define NTAPI __stdcall
#else
#define _cdecl
#define NTAPI
#endif

//
// Define API decoration for direct importing system DLL references.
//

#if !defined(_NTSYSTEM_)
#define NTSYSAPI     DECLSPEC_IMPORT
#define NTSYSCALLAPI DECLSPEC_IMPORT
#else
#define NTSYSAPI
#if defined(_NTDLLBUILD_)
#define NTSYSCALLAPI
#else
#define NTSYSCALLAPI DECLSPEC_ADDRSAFE
#endif

#endif

int __cdecl swprintf (
        wchar_t *string,
        const wchar_t *format,
        ...
        );

typedef struct _UNICODE_STRING {
    USHORT Length;
    USHORT MaximumLength;
#ifdef MIDL_PASS
    [size_is(MaximumLength / 2), length_is((Length) / 2) ] USHORT * Buffer;
#else // MIDL_PASS
    PWSTR  Buffer;
#endif // MIDL_PASS
} UNICODE_STRING;
typedef UNICODE_STRING *PUNICODE_STRING;
typedef const UNICODE_STRING *PCUNICODE_STRING;

#define TRACE_MESSAGE_SEQUENCE                1      // Message should include a sequence number
#define TRACE_MESSAGE_GUID                    2      // Message includes a GUID
#define TRACE_MESSAGE_COMPONENTID             4      // Message has no GUID, Component ID instead
#define    TRACE_MESSAGE_TIMESTAMP            8      // Message includes a timestamp
#define TRACE_MESSAGE_PERFORMANCE_TIMESTAMP   16     // *Obsolete* Clock type is controlled by
                                                     // the logger
#define    TRACE_MESSAGE_SYSTEMINFO	      32     // Message includes system information TID,PID
#define TRACE_MESSAGE_FLAG_MASK               0xFFFF // Only the lower 16 bits of flags are
                                                     // placed in the message those above 16
                                                     // bits are reserved for local processing
#define TRACE_MESSAGE_MAXIMUM_SIZE  8*1024           // the maximum size allowed for a single trace
                                                     // message

#define RtlFillMemory(Destination,Length,Fill) StorMemSet((Destination),(Fill),(Length))
#define RtlZeroMemory(Destination,Length) StorMemSet((Destination),0,(Length))

typedef LONG NTSTATUS;

//
// Generic test for success on any status value (non-negative numbers
// indicate success).
//

#define NT_SUCCESS(Status) ((NTSTATUS)(Status) >= 0)

#define STATUS_SUCCESS                   ((NTSTATUS)0x00000000L) // ntsubauth
#define STATUS_WMI_GUID_NOT_FOUND        ((NTSTATUS)0xC0000295L)
#define STATUS_BUFFER_TOO_SMALL          ((NTSTATUS)0xC0000023L)
#define STATUS_INVALID_PARAMETER         ((NTSTATUS)0xC000000DL)
#define STATUS_INVALID_DEVICE_REQUEST    ((NTSTATUS)0xC0000010L)

typedef ULONG64 TRACEHANDLE, *PTRACEHANDLE;

typedef enum _TRACE_INFORMATION_CLASS {
    TraceIdClass,
    TraceHandleClass,
    TraceEnableFlagsClass,
    TraceEnableLevelClass,
    GlobalLoggerHandleClass,
    EventLoggerHandleClass,
    AllLoggerHandlesClass,
    TraceHandleByNameClass
} TRACE_INFORMATION_CLASS;

typedef PVOID PDEVICE_OBJECT;

//
// Action code for IoWMIRegistrationControl api
//

#define WMIREG_ACTION_REGISTER      1
#define WMIREG_ACTION_DEREGISTER    2
#define WMIREG_ACTION_REREGISTER    3
#define WMIREG_ACTION_UPDATE_GUIDS  4
#define WMIREG_ACTION_BLOCK_IRPS    5

#define REG_DWORD                   ( 4 )   // 32-bit number

//
// Subroutines for dealing with the Registry
//

typedef NTSTATUS (NTAPI * PRTL_QUERY_REGISTRY_ROUTINE)(
    IN PWSTR ValueName,
    IN ULONG ValueType,
    IN PVOID ValueData,
    IN ULONG ValueLength,
    IN PVOID Context,
    IN PVOID EntryContext
    );

typedef struct _RTL_QUERY_REGISTRY_TABLE {
    PRTL_QUERY_REGISTRY_ROUTINE QueryRoutine;
    ULONG Flags;
    PWSTR Name;
    PVOID EntryContext;
    ULONG DefaultType;
    PVOID DefaultData;
    ULONG DefaultLength;

} RTL_QUERY_REGISTRY_TABLE, *PRTL_QUERY_REGISTRY_TABLE;

//
// The following flags specify how the Name field of a RTL_QUERY_REGISTRY_TABLE
// entry is interpreted.  A NULL name indicates the end of the table.
//

#define RTL_QUERY_REGISTRY_SUBKEY   0x00000001  // Name is a subkey and remainder of
                                                // table or until next subkey are value
                                                // names for that subkey to look at.

#define RTL_QUERY_REGISTRY_TOPKEY   0x00000002  // Reset current key to original key for
                                                // this and all following table entries.

#define RTL_QUERY_REGISTRY_REQUIRED 0x00000004  // Fail if no match found for this table
                                                // entry.

#define RTL_QUERY_REGISTRY_NOVALUE  0x00000008  // Used to mark a table entry that has no
                                                // value name, just wants a call out, not
                                                // an enumeration of all values.

#define RTL_QUERY_REGISTRY_NOEXPAND 0x00000010  // Used to suppress the expansion of
                                                // REG_MULTI_SZ into multiple callouts or
                                                // to prevent the expansion of environment
                                                // variable values in REG_EXPAND_SZ

#define RTL_QUERY_REGISTRY_DIRECT   0x00000020  // QueryRoutine field ignored.  EntryContext
                                                // field points to location to store value.
                                                // For null terminated strings, EntryContext
                                                // points to UNICODE_STRING structure that
                                                // that describes maximum size of buffer.
                                                // If .Buffer field is NULL then a buffer is
                                                // allocated.
                                                //

#define RTL_QUERY_REGISTRY_DELETE   0x00000040  // Used to delete value keys after they
                                                // are queried.

#if (NTDDI_VERSION >= NTDDI_WIN2K)
NTSYSAPI
NTSTATUS
NTAPI
RtlQueryRegistryValues(
    IN ULONG RelativeTo,
    IN PCWSTR Path,
    IN PRTL_QUERY_REGISTRY_TABLE QueryTable,
    IN PVOID Context,
    IN OPTIONAL PVOID Environment
    );
#endif

//
// The following values for the RelativeTo parameter determine what the
// Path parameter to RtlQueryRegistryValues is relative to.
//

#define RTL_REGISTRY_ABSOLUTE     0   // Path is a full path
#define RTL_REGISTRY_SERVICES     1   // \Registry\Machine\System\CurrentControlSet\Services
#define RTL_REGISTRY_CONTROL      2   // \Registry\Machine\System\CurrentControlSet\Control
#define RTL_REGISTRY_WINDOWS_NT   3   // \Registry\Machine\Software\Microsoft\Windows NT\CurrentVersion
#define RTL_REGISTRY_DEVICEMAP    4   // \Registry\Machine\Hardware\DeviceMap
#define RTL_REGISTRY_USER         5   // \Registry\User\CurrentUser
#define RTL_REGISTRY_MAXIMUM      6
#define RTL_REGISTRY_HANDLE       0x40000000    // Low order bits are registry handle
#define RTL_REGISTRY_OPTIONAL     0x80000000    // Indicates the key node is optional

///////////////////////////////////////////////////////////////////////////////

__inline TRACEHANDLE WppQueryLogger(__in_opt PWSTR LoggerName)

{
#ifndef WPP_TRACE_W2K_COMPATABILITY
    ULONG ReturnLength;
    NTSTATUS Status;
    TRACEHANDLE TraceHandle;
    UNICODE_STRING  Buffer;

    StorRtlInitUnicodeString(&Buffer, LoggerName ? LoggerName : L"stdout");


    if ((Status = StorWmiQueryTraceInformation(TraceHandleByNameClass,
                                               (PVOID)&TraceHandle,
                                               sizeof(TraceHandle),
                                               &ReturnLength,
                                               (PVOID)&Buffer)) != STATUS_SUCCESS) {
       return 0;
    }

    return TraceHandle;
#else
    return (TRACEHANDLE) 0;
#endif  // #ifdef WPP_TRACE_W2K_COMPATABILITY

}

typedef NTSTATUS (*WMIENTRY_NEW)(
    IN UCHAR ActionCode,
    IN PVOID DataPath,
    IN ULONG BufferSize,
    IN OUT PVOID Buffer,
    IN PVOID Context,
    OUT PULONG Size
    );

typedef struct _WPP_TRACE_CONTROL_BLOCK
{
    WMIENTRY_NEW                     Callback;
    struct _WPP_TRACE_CONTROL_BLOCK *Next;

    __int64 Logger;
    UCHAR FlagsLen; UCHAR Level; USHORT Reserved;
    ULONG  Flags[1];
} WPP_TRACE_CONTROL_BLOCK, *PWPP_TRACE_CONTROL_BLOCK;

typedef struct _WPP_REGISTRATION_BLOCK
{
    WMIENTRY_NEW                    Callback;
    struct _WPP_REGISTRATION_BLOCK *Next;

    LPCGUID ControlGuid;
    LPCWSTR  FriendlyName;
    LPCWSTR  BitNames;
    PUNICODE_STRING RegistryPath;

    UCHAR   FlagsLen, RegBlockLen;
} WPP_REGISTRATION_BLOCK, *PWPP_REGISTRATION_BLOCK;



