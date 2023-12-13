// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------
//

//
//------------------------------------------------------------------------------

#pragma once

#include <windows.h>

#if !defined(STATUS_PROCEDURE_NOT_FOUND)
#define STATUS_PROCEDURE_NOT_FOUND       ((NTSTATUS)0xC000007AL)
#endif

#if !defined (STATUS_SUCCESS)
//
// MessageId: STATUS_SUCCESS
//
// MessageText:
//
//  STATUS_SUCCESS
//
#define STATUS_SUCCESS                   ((NTSTATUS)0x00000000L)
#endif

//
// RtlVerifyVersionInfo() conditions
//

#if !defined(VER_EQUAL)
#define VER_EQUAL                       1
#endif

#if !defined(VER_GREATER)
#define VER_GREATER                     2
#endif

#if !defined(VER_GREATER_EQUAL)
#define VER_GREATER_EQUAL               3
#endif

#if !defined(VER_LESS)
#define VER_LESS                        4
#endif

#if !defined(VER_LESS_EQUAL)
#define VER_LESS_EQUAL                  5
#endif

#if !defined(VER_AND)
#define VER_AND                         6
#endif

#if !defined(VER_OR)
#define VER_OR                          7
#endif

#if !defined(VER_CONDITION_MASK)
#define VER_CONDITION_MASK              7
#endif

#if !defined(VER_NUM_BITS_PER_CONDITION_MASK)
#define VER_NUM_BITS_PER_CONDITION_MASK 3
#endif 
//
// RtlVerifyVersionInfo() type mask bits
//

#if !defined(VER_MINORVERSION)
#define VER_MINORVERSION                0x0000001
#endif 

#if !defined(VER_MAJORVERSION)
#define VER_MAJORVERSION                0x0000002
#endif 

#if !defined(VER_BUILDNUMBER)
#define VER_BUILDNUMBER                 0x0000004
#endif

#if !defined(VER_PLATFORMID)
#define VER_PLATFORMID                  0x0000008
#endif

#if !defined(VER_SERVICEPACKMINOR)
#define VER_SERVICEPACKMINOR            0x0000010
#endif

#if !defined(VER_SERVICEPACKMAJOR)
#define VER_SERVICEPACKMAJOR            0x0000020
#endif

#if !defined(VER_SUITENAME)
#define VER_SUITENAME                   0x0000040
#endif

#if !defined(VER_PRODUCT_TYPE)
#define VER_PRODUCT_TYPE                0x0000080
#endif

//
// RtlVerifyVersionInfo() os product type values
//

#if !defined(VER_NT_WORKSTATION)
#define VER_NT_WORKSTATION              0x0000001
#endif

#if !defined(VER_NT_DOMAIN_CONTROLLER)
#define VER_NT_DOMAIN_CONTROLLER        0x0000002
#endif

#if !defined(VER_NT_SERVER)
#define VER_NT_SERVER                   0x0000003
#endif

//
// dwPlatformId defines:
//

#if !defined(VER_PLATFORM_WIN32s)
#define VER_PLATFORM_WIN32s             0
#endif

#if !defined(VER_PLATFORM_WIN32_WINDOWS)
#define VER_PLATFORM_WIN32_WINDOWS      1
#endif

#if !defined(VER_PLATFORM_WIN32_NT)
#define VER_PLATFORM_WIN32_NT           2
#endif 

#pragma region Desktop Family or OneCore Family
#if WINAPI_FAMILY_PARTITION(WINAPI_PARTITION_DESKTOP | WINAPI_PARTITION_SYSTEM)

//
//
// VerifyVersionInfo() macro to set the condition mask
//
// For documentation sakes here's the old version of the macro that got
// changed to call an API
// #define VER_SET_CONDITION(_m_,_t_,_c_)  _m_=(_m_|(_c_<<(1<<_t_)))
//

#if !defined(VER_SET_CONDITION)

#define VER_SET_CONDITION(_m_,_t_,_c_)  \
        ((_m_)=VerSetConditionMask((_m_),(_t_),(_c_)))

#endif
#endif
#pragma endregion

#if (NTDDI_VERSION >= NTDDI_WIN2K)

inline
_IRQL_requires_max_(PASSIVE_LEVEL)
_Must_inspect_result_
NTSTATUS
NTAPI
RtlVerifyVersionInfo(
    _In_ PRTL_OSVERSIONINFOEXW VersionInfo,
    _In_ ULONG TypeMask,
    _In_ ULONGLONG  ConditionMask
)
{
    NTSTATUS result = STATUS_PROCEDURE_NOT_FOUND;

    auto hModule = LoadLibraryW(L"ntdll.dll");
    if (hModule != nullptr)
    {
        auto pFarProc = GetProcAddress(hModule, "RtlVerifyVersionInfo");
        if (pFarProc != nullptr)
        {
            auto pRtlVerifyVersionInfo = reinterpret_cast<NTSTATUS(NTAPI*)(PRTL_OSVERSIONINFOEXW, ULONG, ULONGLONG)>(pFarProc);
            result = pRtlVerifyVersionInfo(VersionInfo, TypeMask, ConditionMask);
        }
        FreeLibrary(hModule);
    }

    return result;
}

inline
_IRQL_requires_max_(PASSIVE_LEVEL)
NTSTATUS
NTAPI
RtlGetVersion(
    _Out_
    _At_(lpVersionInformation->dwOSVersionInfoSize, _Pre_ _Valid_)
    _When_(lpVersionInformation->dwOSVersionInfoSize == sizeof(RTL_OSVERSIONINFOEXW),
        _At_((PRTL_OSVERSIONINFOEXW)lpVersionInformation, _Out_))
    PRTL_OSVERSIONINFOW lpVersionInformation
)
{
    NTSTATUS result = STATUS_PROCEDURE_NOT_FOUND;

    auto hModule = LoadLibraryW(L"ntdll.dll");
    if (hModule != nullptr)
    {
        auto pFarProc = GetProcAddress(hModule, "RtlGetVersion");
        if (pFarProc != nullptr)
        {
            auto pRtlVerifyVersionInfo = reinterpret_cast<NTSTATUS(NTAPI*)(PRTL_OSVERSIONINFOW)>(pFarProc);
            result = pRtlVerifyVersionInfo(lpVersionInformation);
        }
        FreeLibrary(hModule);
    }

    return result;
}

#endif


