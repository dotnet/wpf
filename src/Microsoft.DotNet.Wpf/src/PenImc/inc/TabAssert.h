// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/////////////////////////////////////////////////////////////////////////////
//
//
// Module:       
//      TabAssert.h
//
// Description:
//      To facilitate the debugging of Tablet Platform built binaries, we
//      needed a common set of functionallity for ASSERTs and debug logging.
//      This header file represents that collection of functionallity.
//
//      Additionally, the testing team has requested the ability to turn
//      off ASSERTs programmatically. 
//
//
// Comments:
//
//      Under HKEY_CLASSES_ROOT\TpgDebug, there are currently 8 values:
//
//          AssertMode
//          AssertFile
//          DumpInfoMode
//          DumpInfoFile
//          FuncTraceMode
//          FuncTraceFile
//          HRFailMode
//          HRFailFile
//
//      These Modes are a bitwise OR of 3 possible values which correspond 
//      to the CRT Debug libraries definitions of
//
//          _CRTDBG_MODE_FILE      0x1    (file)
//          _CRTDBG_MODE_DEBUG     0x2    (trace window)
//          _CRTDBG_MODE_WNDW      0x4    (dialog window)
//
//      The File values (AssertFile, HRFailFile) are used if bit 0x1 is set
//      for a corresponding Mode.
//
//      In the default settings (if you've never modified your registry by
//      hand or programatically), the values would be:
//
//          AssertMode = 4
//          AssertFile = "C:\TPGDEBUG.LOG"
//          DumpInfoMode = 2
//          DumpInfoFile = "C:\TPGDEBUG.LOG"
//          FuncTraceMode = 0
//          FuncTraceFile = "C:\TPGDEBUG.LOG"
//          HRFailMode = 2
//          HRFailFile = "C:\TPGDEBUG.LOG"
//
//      This would result in Assert failures being displayed in a dialog window
//      (because 0x4 is set in AssertMode and 0x4 corresponds to WNDW above). 
//
//      If you wanted to turn off the dialog window based asserts, but you still
//      wanted them logged to a file and sent to the debugger output window, you
//      could set AssertMode = 3 (which is 0x1 | 0x2, or written symbolically as
//      FILE | DEBUG from above).
//
//      Additionally, there is a fifth value under TpgDebug, called 
//      AssertSettingsReReadEachTime. This is a boolean value and is used in
//      speical cases when the test team wants to programmatically change
//      the AssertMode at runtime while the binaries are running. The default
//      for this value is 0 and as such, all the TpgDebug registry settings
//      are read at startup. If AssertSettingsReReadEachTime is set to 1, 
//      most of the TpgDebug values are read at startup, with the exception of
//      AssertMode/AssertFile. This allows the testers to log the assert values
//      to specific locations. Care should be used when enabling this setting,
//      though, because this is a huge performance hit.
//
/////////////////////////////////////////////////////////////////////////////

#pragma once

#ifndef _TABASSERT_HEADER

#include <TCHAR.h>

#include <windows.h>
#include <imagehlp.h>
#include <crtdbg.h>

#if 0 // #ifdef DBG

#ifdef __cplusplus
extern "C" {
#endif

int WINAPI MyCrtSetReportMode(
        int nRptType,
        int fMode
        );
        
_HFILE WINAPI MyCrtSetReportFile(
        int nRptType,
        _HFILE hFile
        );
        
int WINAPI MyCrtDbgReportA(
        int nRptType,
        const char * szFile,
        int nLine,
        const char * szModule,
        const char * szFormat,
        ...
        );

int WINAPI MyCrtDbgReportW(
        int nRptType,
        const WCHAR * wzFile,
        int nLine,
        const WCHAR * wzModule,
        const WCHAR * wzFormat,
        ...
        );

void WINAPI MyCrtDbgBreak(
        void
        );
        
BOOL WINAPI TpgDebugAssertEnter();
BOOL WINAPI TpgDebugAssertLeave();
BOOL WINAPI TpgDebugDumpInfoEnter();
BOOL WINAPI TpgDebugDumpInfoLeave();
BOOL WINAPI TpgDebugFuncTraceEnter();
BOOL WINAPI TpgDebugFuncTraceLeave();
BOOL WINAPI TpgDebugHRFailEnter();
BOOL WINAPI TpgDebugHRFailLeave();

int WINAPI DoTheAssert(
        const char * pszFile,
        int nLine,
        const char * szExpr,
        BOOL fHaveHR,
        HRESULT hr
        );
        
void WINAPI DoTheHRFail(
    const char *pszFile,
    int nLine,
    HRESULT hr);

#ifdef __cplusplus
}
#endif
        

#endif // DBG

//=== User macros ==============================================================

#if 0 // #ifdef DBG

// Push current pragma settings
#pragma push

// Turn off "conditional expression is constant" because of while(0). 
#pragma warning ( disable : 4127 ) 

#if defined __cplusplus && !defined _PREFAST_

// Turn off "local variable 'hr' used without having been initialized" because of HRESULT hr; statements
#pragma warning ( disable : 4700 ) 

// C++ compatible assert that includes HR value.
#define TPDBG_ASSERTSZ(expr,szDescription)             \
    do                                                 \
    {                                                  \
        if (!(expr))                                   \
        {                                              \
            if (TpgDebugAssertEnter())                 \
            {                                          \
                static DWORD dwDisableAssert;          \
                if (dwDisableAssert == 0)              \
                {                                      \
                    __if_exists(hr)                    \
                    {                                  \
                        int iRet = DoTheAssert(        \
                                        __FILE__,      \
                                        __LINE__,      \
                                        szDescription, \
                                        TRUE,          \
                                        hr );          \
                    }                                  \
                    __if_not_exists(hr)                \
                    {                                  \
                        int iRet = DoTheAssert(        \
                                        __FILE__,      \
                                        __LINE__,      \
                                        szDescription, \
                                        FALSE,         \
                                        0);            \
                    }                                  \
                    if (iRet == 1)                     \
                    {                                  \
                        MyCrtDbgBreak();               \
                    }                                  \
                    else if (iRet == 2)                \
                    {                                  \
                        dwDisableAssert = 1;           \
                    }                                  \
                }                                      \
                TpgDebugAssertLeave();                 \
            }                                          \
        }                                              \
    }                                                  \
    while (0)

#else

// C compatible assert that does not includes HR value.
// Prefast compatible version (prefast also complains about uninitialized hr variables).
#define TPDBG_ASSERTSZ(expr,szDescription)             \
    do                                                 \
    {                                                  \
        if (!(expr))                                   \
        {                                              \
            if (TpgDebugAssertEnter())                 \
            {                                          \
                static DWORD dwDisableAssert;          \
                if (dwDisableAssert == 0)              \
                {                                      \
                    int iRet = DoTheAssert(            \
                                    __FILE__,          \
                                    __LINE__,          \
                                    szDescription,     \
                                    FALSE,             \
                                    0);                \
                    if (iRet == 1)                     \
                    {                                  \
                        MyCrtDbgBreak();               \
                    }                                  \
                    else if (iRet == 2)                \
                    {                                  \
                        dwDisableAssert = 1;           \
                    }                                  \
                }                                      \
                TpgDebugAssertLeave();                 \
            }                                          \
        }                                              \
    }                                                  \
    while (0)

#endif

// Restore pragma settings.
#pragma pop

#define TPDBG_ASSERT(expr)  \
    TPDBG_ASSERTSZ(expr, #expr)

#define TPDBG_VERIFY(expr)  \
    TPDBG_ASSERT(expr)

#ifdef ASSERT
#undef ASSERT
#endif // ASSERT

#define ASSERT(expr)        \
    TPDBG_ASSERT(expr)

#ifdef ASSERTSZ
#undef ASSERTSZ
#endif // ASSERTSZ

#define ASSERTSZ(expr, szDescription)        \
    TPDBG_ASSERTSZ(expr, szDescription)

#define TPDBG_RPT(rptno, msg) \
        do { if ((1 == MyCrtDbgReportW(rptno, NULL, 0, NULL, L"%s", msg))) \
                MyCrtDbgBreak(); } while (0)

#define TPDBG_RPT0(rptno, msg) \
        do { if ((1 == MyCrtDbgReportA(rptno, NULL, 0, NULL, "%s", msg))) \
                MyCrtDbgBreak(); } while (0)

#define TPDBG_RPT1(rptno, msg, arg1) \
        do { if ((1 == MyCrtDbgReportA(rptno, NULL, 0, NULL, msg, arg1))) \
                MyCrtDbgBreak(); } while (0)

#define TPDBG_RPT2(rptno, msg, arg1, arg2) \
        do { if ((1 == MyCrtDbgReportA(rptno, NULL, 0, NULL, msg, arg1, arg2))) \
                MyCrtDbgBreak(); } while (0)

#define TPDBG_RPT3(rptno, msg, arg1, arg2, arg3) \
        do { if ((1 == MyCrtDbgReportA(rptno, NULL, 0, NULL, msg, arg1, arg2, arg3))) \
                MyCrtDbgBreak(); } while (0)

#define TPDBG_RPT4(rptno, msg, arg1, arg2, arg3, arg4) \
        do { if ((1 == MyCrtDbgReportA(rptno, NULL, 0, NULL, msg, arg1, arg2, arg3, arg4))) \
                MyCrtDbgBreak(); } while (0)

void DMSG(WCHAR *wzformat, ...);

#define TPDBG_DMSG0(format)                                        \
    do                                                             \
    {                                                              \
        if (TpgDebugDumpInfoEnter())                               \
        {                                                          \
            TPDBG_RPT0(_CRT_WARN, format);                         \
            TpgDebugDumpInfoLeave();                               \
        }                                                          \
    } while (0)
#define TPDBG_DMSG1(format, arg1)                                  \
    do                                                             \
    {                                                              \
        if (TpgDebugDumpInfoEnter())                               \
        {                                                          \
            TPDBG_RPT1(_CRT_WARN, format, arg1);                   \
            TpgDebugDumpInfoLeave();                               \
        }                                                          \
    } while (0)
#define TPDBG_DMSG2(format, arg1, arg2)                            \
    do                                                             \
    {                                                              \
        if (TpgDebugDumpInfoEnter())                               \
        {                                                          \
            TPDBG_RPT2(_CRT_WARN, format, arg1, arg2);             \
            TpgDebugDumpInfoLeave();                               \
        }                                                          \
    } while (0)
#define TPDBG_DMSG3(format, arg1, arg2, arg3)                      \
    do                                                             \
    {                                                              \
        if (TpgDebugDumpInfoEnter())                               \
        {                                                          \
            TPDBG_RPT3(_CRT_WARN, format, arg1, arg2, arg3);       \
            TpgDebugDumpInfoLeave();                               \
        }                                                          \
    } while (0)
#define TPDBG_DMSG4(format, arg1, arg2, arg3, arg4)                \
    do                                                             \
    {                                                              \
        if (TpgDebugDumpInfoEnter())                               \
        {                                                          \
            TPDBG_RPT4(_CRT_WARN, format, arg1, arg2, arg3, arg4); \
            TpgDebugDumpInfoLeave();                               \
        }                                                          \
    } while (0)
    
#define TPDBG_FUNC_ENTER(name)                                          \
    do                                                                  \
    {                                                                   \
        if (TpgDebugFuncTraceEnter())                                   \
        {                                                               \
        TPDBG_RPT2(_CRT_WARN, "0x%x: Entering Function: %s\r\n", GetCurrentThreadId(), name);   \
            TpgDebugFuncTraceLeave();                                   \
        }                                                               \
    } while (0)

#define TPDBG_FUNC_LEAVE(name)                                          \
    do                                                                  \
    {                                                                   \
        if (TpgDebugFuncTraceEnter())                                   \
        {                                                               \
        TPDBG_RPT2(_CRT_WARN, "0x%x: Leaving Function: %s\r\n", GetCurrentThreadId(), name);      \
            TpgDebugFuncTraceLeave();                                   \
        }                                                               \
    } while (0)

#ifdef __cplusplus

class CTpgFuncTrace
{
public:

    CTpgFuncTrace(char * pszFuncName) : m_pszFuncName(pszFuncName) { TPDBG_FUNC_ENTER(m_pszFuncName); }
    ~CTpgFuncTrace() { TPDBG_FUNC_LEAVE(m_pszFuncName); }
    
private:
    char * m_pszFuncName;
};

#define TPDBG_FUNC(name) \
    CTpgFuncTrace functrace(name)

#define DBGFUNC \
    CTpgFuncTrace functrace(__FUNCTION__)

#endif // __cplusplus


#define TPDBG_REPORT_ON_FAIL(hr)                    \
    do                                              \
    {                                               \
        HRESULT _hr = (hr);                         \
        if (FAILED(_hr) && TpgDebugHRFailEnter())   \
        {                                           \
            DoTheHRFail(                            \
                __FILE__,                           \
                __LINE__,                           \
                _hr);                               \
            TpgDebugHRFailLeave();                  \
        }                                           \
    } while (0)

#define TPDBG_RETURN(hr)                \
    {                                   \
        HRESULT __hr = (hr);            \
        if (FAILED(__hr))               \
        {                               \
            TPDBG_REPORT_ON_FAIL(__hr); \
        }                               \
        return __hr;                    \
    }

#else // DBG

#define TPDBG_ASSERT(expr)
#define TPDBG_VERIFY(expr) (expr)

#ifdef ASSERT
#undef ASSERT
#endif // ASSERT

#define ASSERT(expr)

#ifdef ASSERTSZ
#undef ASSERTSZ
#endif //ASSERTSZ

#define ASSERTSZ(expr, szDescription)

#define TPDBG_RPT0(rptno, msg)
#define TPDBG_RPT1(rptno, msg, arg1)
#define TPDBG_RPT2(rptno, msg, arg1, arg2)
#define TPDBG_RPT3(rptno, msg, arg1, arg2, arg3)
#define TPDBG_RPT4(rptno, msg, arg1, arg2, arg3, arg4)

#define TPDBG_DMSG0(format)                                   
#define TPDBG_DMSG1(format, arg1)                             
#define TPDBG_DMSG2(format, arg1, arg2)                       
#define TPDBG_DMSG3(format, arg1, arg2, arg3)                 
#define TPDBG_DMSG4(format, arg1, arg2, arg3, arg4)           

#define TPDBG_FUNC_ENTER(name)
#define TPDBG_FUNC_LEAVE(name)

#ifdef __cplusplus
#define TPDBG_FUNC(name)
#define DBGFUNC
#endif // __cplusplus

#define TPDBG_REPORT_ON_FAIL(hr)
#define TPDBG_RETURN(hr) return (hr)

#endif // DBG

#endif //_TABASSERT_HEADER

