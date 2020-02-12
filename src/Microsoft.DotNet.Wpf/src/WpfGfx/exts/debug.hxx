// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++



Module Name:

    debug.hxx

Abstract:

    This file header contains prototypes for debug routines to 
    debug extenstion problems.

--*/

#pragma once

#include <wdbgexts.h>

#if DBG

extern const char NoIndent[];

void
vPrintNativeFieldInfo(
    PFIELD_INFO pFI,
    const char *pszIndent = NoIndent);

void
vPrintNativeSymDumpParam(
    PSYM_DUMP_PARAM pSDP,
    BOOL bDumpFields = TRUE,
    const char *pszIndent = NoIndent);

#define RIP(msg)            \
    do {                    \
        DbgPrint(msg);      \
        DbgBreakPoint();    \
    } while (0)

#else

// Disable DbgPrint from NT RTL
#define DbgPrint

#define RIP(msg)

#define vPrintNativeFieldInfo
#define vPrintNativeSymDumpParam

#endif  DBG



#define MILX_ENABLE_TRACING 0

#if MILX_ENABLE_TRACING

 #define MILX_TRACE_RETURN \
    pOutCtl->OutVerb("[trace return from %s @ %s:%i]\n", __FUNCTION__, __FILE__, __LINE__)

 #define MILX_TRACE_ENTRY { pOutCtl->OutVerb("[trace entry in %s @ %s:%i]\n", __FUNCTION__,  __FILE__, __LINE__); }

 #define MILX_TRACE { pOutCtl->OutVerb("[trace in %s @ %s:%i]\n", __FUNCTION__, __FILE__, __LINE__); }

#else

 #define MILX_TRACE_RETURN
 #define MILX_TRACE_ENTRY
 #define MILX_TRACE

#endif


#define RRETURN(HR) \
    { \
        HRESULT __hr = (HR); \
        __if_exists (pOutCtl) \
        { \
            if (pOutCtl) \
            { \
                if (FAILED(__hr)) { \
                    pOutCtl->OutErr("[failure in %s @ %s:%i -- returning error code 0x%08x]\n", __FUNCTION__, __FILE__, __LINE__, __hr); \
                } else { \
                    MILX_TRACE_RETURN; \
                } \
            } \
        } \
        \
        return __hr; \
    }


HRESULT ResolveHMilResource(
    PDEBUG_CLIENT Client,
    ULONG64 ulhResource,
    ULONG64 ulpMilChannel,
    __out ULONG64* pulpHANDLE_ENTRY
    );

HRESULT LookupCMilWindowContext(
    PDEBUG_CLIENT Client,
    ULONG64 hwnd,
    __out ULONG64* pulpCMilWindowContext
    );


