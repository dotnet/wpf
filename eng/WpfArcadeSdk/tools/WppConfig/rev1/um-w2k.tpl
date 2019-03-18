`**********************************************************************`
`* This is a template file for tracewpp preprocessor                  *`
`* If you need to use a custom version of this file in your project   *`
`* Please clone it from this one and point WPP to it by specifying    *`
`* -gen:{yourfile} option on RUN_WPP line in your sources file        *`
`*                                                                    *`
`*    Copyright (c) Microsoft Corporation. All Rights Reserved.       *`
`**********************************************************************`
//`Compiler.Checksum` Generated File. Do not edit.
// File created by `Compiler.Name` compiler version `Compiler.Version`-`Compiler.Timestamp`
// on `System.Date` at `System.Time` UTC from a template `TemplateFile`

`INCLUDE um-header.tpl` 

#ifndef WPP_ALREADY_INCLUDED

#if defined(__cplusplus)
extern "C" {
#endif

typedef
ULONG
(*PFN_WPPTRACEMESSAGE)(
    IN TRACEHANDLE  LoggerHandle,
    IN ULONG   MessageFlags,
    IN LPGUID  MessageGuid,
    IN USHORT  MessageNumber,
    IN ...
    );


typedef
ULONG
(*PFN_WPPTRACEMESSAGEVA)(
    IN TRACEHANDLE  LoggerHandle,
    IN ULONG   MessageFlags,
    IN LPGUID  MessageGuid,
    IN USHORT  MessageNumber,
    IN va_list      MessageArgList    
    );

typedef enum _WPP_TRACE_API_SUITE {
    WppTraceWin2K,
    WppTraceWinXP,
    WppTraceTraceLH,
    WppTraceMaxSuite
} WPP_TRACE_API_SUITE;

ULONG
WppInitTraceFunction(IN TRACEHANDLE  LoggerHandle,
                    IN DWORD TraceOptions,
                    IN LPGUID MessageGuid,
                    IN USHORT MessageNumber,
                    ...
                    );
ULONG 
TraceMessageW2k(IN TRACEHANDLE  LoggerHandle, 
                IN DWORD TraceOptions, 
                IN LPGUID MessageGuid, 
                IN USHORT MessageNumber, 
                ...
                ) ;

ULONG
TraceMessageW2kVa(IN TRACEHANDLE  LoggerHandle,
                IN DWORD TraceOptions,
                IN LPGUID MessageGuid,
                IN USHORT MessageNumber,
                IN va_list MessageArgList
                );
                
__declspec(selectany) PFN_WPPTRACEMESSAGE  pfnWppTraceMessage = WppInitTraceFunction;
__declspec(selectany) PFN_WPPTRACEMESSAGEVA  pfnWppTraceMessageVa = TraceMessageW2kVa;
__declspec(selectany) WPP_TRACE_API_SUITE WPPTraceSuite = WppTraceWin2K;


#undef WPP_TRACE
#define WPP_TRACE pfnWppTraceMessage

#ifndef WPP_MAX_MOF_FIELDS
#define WPP_MAX_MOF_FIELDS 7
#endif


#ifndef TRACE_MESSAGE_MAXIMUM_SIZE
#define TRACE_MESSAGE_MAXIMUM_SIZE  8*1024
#endif


__inline ULONG
TraceMessageW2kVa(IN TRACEHANDLE  LoggerHandle,
                IN DWORD TraceOptions,
                IN LPGUID MessageGuid,
                IN USHORT MessageNumber,
                IN va_list MessageArgList
                )
/*++

Routine Description:

    This is a function that simulates tracemessage in W2K

Arguments:

    LoggerHandle - handle providers logger handle

    TraceOptions - unreferenced

    MessageGuid - pointer to message GUID

    MessageNumber - Type of message been logged

    MessageArgList - list or arguments

Return Value:

     code indicating success or failure
--*/
{

typedef struct _WPP_TRACE_BUFFER {
    EVENT_TRACE_HEADER Header;
    MOF_FIELD MofField[WPP_MAX_MOF_FIELDS+1];
} WPP_TRACE_BUFFER;


    size_t               ByteCount;
    size_t               ArgumentCount;
    va_list              VarArgs = MessageArgList;
    PVOID                DataPtr ;
    size_t               DataSize = 0;
    size_t               ArgOffset;
    ULONG                Status;
    WPP_TRACE_BUFFER     TraceBuf;
    PVOID                Data=NULL;

   UNREFERENCED_PARAMETER(TraceOptions);

   //
   // Fill in header fields
   // Type is 0xFF to indicate that the first data is the MessageNumber
   // The first Mof data is the Message Number
   //

   TraceBuf.Header.GuidPtr = (ULONGLONG)MessageGuid ;
   TraceBuf.Header.Flags = WNODE_FLAG_TRACED_GUID |WNODE_FLAG_USE_GUID_PTR|WNODE_FLAG_USE_MOF_PTR;
   TraceBuf.Header.Class.Type = 0xFF ;
   TraceBuf.MofField[0].DataPtr = (ULONGLONG)&MessageNumber;
   TraceBuf.MofField[0].Length = sizeof(USHORT);

   // Determine the number bytes to follow header
   ByteCount = 0 ;
   ArgumentCount = 0 ;

   while ((DataPtr = va_arg (VarArgs, PVOID)) != NULL) {
        DataSize = va_arg (VarArgs, size_t);

        // Check for integer overflow.
        if (ByteCount + DataSize > ByteCount)
        {
            ByteCount += DataSize ;
            ArgumentCount++ ;

            if (ArgumentCount <= WPP_MAX_MOF_FIELDS) {
                TraceBuf.MofField[ArgumentCount].DataPtr = (ULONGLONG)DataPtr;
                TraceBuf.MofField[ArgumentCount].Length = (ULONG)DataSize;
            }
        }
   }

   va_end(VarArgs);
   if (ByteCount > TRACE_MESSAGE_MAXIMUM_SIZE) {
            return 0;
   }

   if (ArgumentCount > WPP_MAX_MOF_FIELDS) {
        //
        // This occurs infrequently
        // Allocate the blob to hold the data
        //
        Data = LocalAlloc(0,ByteCount);
        if (Data == NULL) {
            return 0;
        }

        TraceBuf.MofField[1].DataPtr = (ULONGLONG)Data;
        TraceBuf.MofField[1].Length = (ULONG)ByteCount;

        ArgOffset = 0 ;
        DataSize = 0 ;
        VarArgs = MessageArgList;
        while ((DataPtr = va_arg (VarArgs, PVOID)) != NULL) {
            DataSize = va_arg (VarArgs, size_t) ;
            memcpy((char*)Data + ArgOffset, DataPtr, DataSize) ;
            ArgOffset += DataSize ;
        }
        va_end(VarArgs) ;

        //Fill in the total size (header + 2 mof fields)
        TraceBuf.Header.Size = (USHORT)(sizeof(EVENT_TRACE_HEADER) + 2*sizeof(MOF_FIELD));

   } else {
        //Fill in the total size (header + mof fields)
        TraceBuf.Header.Size = (USHORT)(sizeof(EVENT_TRACE_HEADER) + (ArgumentCount+1)*sizeof(MOF_FIELD));
   }

   Status = TraceEvent(LoggerHandle, &TraceBuf.Header) ;

   if (Data) {
        LocalFree(Data);
   }

   if(ERROR_SUCCESS != Status) {
        // Silently ignored error
   }

    return 0;
}


__inline ULONG
TraceMessageW2k(IN TRACEHANDLE  LoggerHandle,
                IN DWORD TraceOptions,
                IN LPGUID MessageGuid,
                IN USHORT MessageNumber,
                ...
                )
/*++

Routine Description:

    This is a function that simulates tracemessage in W2K

Arguments:

    LoggerHandle - handle providers logger handle

    TraceOptions - unreferenced

    MessageGuid - pointer to message GUID

    MessageNumber - Type of message been logged

    ...         - list or arguments

Return Value:

     code indicating success or failure
--*/
{
    va_list              VarArgs ;

    va_start(VarArgs, MessageNumber);

   return( TraceMessageW2kVa(   LoggerHandle, 
                                TraceOptions,
                                MessageGuid,
                                MessageNumber,
                                VarArgs
                             )
                          );
                        
}

//
// Advanced tracing APIs (XP and later) will be indirectly called.
//

__inline VOID WppDynamicTracingSupport(
    VOID
    )
/*++

Routine Description:

    This function assigns at runtime the ETW API set to be use for tracing.

Arguments:

Remarks:

    At runtime determine assing the funtions pointers for the trace APIs to be use.
    XP and above will use TraceMessage, and Win2K will use our private W2kTraceMessage
    which uses TraceEvent

--*/
{
    HINSTANCE hinstLib;

    hinstLib = LoadLibraryW(L"advapi32");
    if (hinstLib != NULL)
    {
        pfnWppTraceMessage = (PFN_WPPTRACEMESSAGE) (INT_PTR)
        GetProcAddress(hinstLib, "TraceMessage");

        if (NULL == pfnWppTraceMessage) {
            pfnWppTraceMessage = TraceMessageW2k;
            pfnWppTraceMessageVa = TraceMessageW2kVa;
            WPPTraceSuite = WppTraceWin2K;
        } else {

            WPPTraceSuite = WppTraceWinXP;
            pfnWppTraceMessageVa = (PFN_WPPTRACEMESSAGEVA) (INT_PTR)
            GetProcAddress(hinstLib, "TraceMessageVa");
            if (NULL == pfnWppTraceMessageVa) {
                pfnWppTraceMessageVa = TraceMessageW2kVa;
            }
        }

        FreeLibrary(hinstLib);
    } else {
        pfnWppTraceMessage = TraceMessageW2k;
        pfnWppTraceMessageVa = TraceMessageW2kVa;
        WPPTraceSuite = WppTraceWin2K;
    }
}


__inline ULONG
WppInitTraceFunction(IN TRACEHANDLE  LoggerHandle,
                    IN DWORD TraceOptions,
                    IN LPGUID MessageGuid,
                    IN USHORT MessageNumber,
                    ...
                    )
/*++

Routine Description:

    This is a function initializes the tracing function if not  
    calling WPP_INIT_TRACING. It uses TraceMessageVa to log events 


--*/
{

    va_list              VarArgs ;

    WppDynamicTracingSupport();

    va_start(VarArgs, MessageNumber);
        
    pfnWppTraceMessageVa(   LoggerHandle, 
                            TraceOptions, 
                            MessageGuid, 
                            MessageNumber,
                            VarArgs
                        );

    return 0;    
}

#undef WppLoadTracingSupport
#define WppLoadTracingSupport WppDynamicTracingSupport()

#if defined(__cplusplus)
};
#endif

#endif // WPP_ALREADY_INCLUDED

`INCLUDE control.tpl`
`INCLUDE tracemacro.tpl`

`IF FOUND WPP_INIT_TRACING`
#define WPPINIT_EXPORT 
  `INCLUDE um-init.tpl`
`ENDIF`

//
// Tracing Macro name redefinition 
//

// NoMsgArgs

`FORALL f IN Funcs WHERE !DoubleP && !MsgArgs`
#undef `f.Name`
#define `f.Name` WPP_(CALL)
`ENDFOR`

`FORALL f IN Funcs WHERE DoubleP && !MsgArgs`
#undef `f.Name`
#define `f.Name`(ARGS) WPP_(CALL) ARGS
`ENDFOR`


// MsgArgs

`FORALL f IN Funcs WHERE MsgArgs`
#undef `f.Name`
#define `f.Name`(`f.FixedArgs` MSGARGS) WPP_(CALL)(`f.FixedArgs` MSGARGS)
`ENDFOR`

`FORALL r IN Reorder`
#undef  WPP_R`r.Name`
#define WPP_R`r.Name`(`r.Arguments`) `r.Permutation`
`ENDFOR`

