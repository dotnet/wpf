// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***
* ddbanned.h - list of devdiv banned API
*
*
*
* Purpose:
*       This include file contains a list of banned API which should not be used.
*       Include this file as first include file (e.g. using the /FI compiler option).
*
*       Please contact "alecont; mclcore" for any issue with this file.
*
****/

#ifndef _INC_DDBANNED
#define _INC_DDBANNED
#pragma once

#include <vadefs.h>
#include <sal.h>

#ifdef __cplusplus
extern "C" {
#endif

/* defines and typedefs */

#ifndef _WCHAR_T_DEFINED
typedef unsigned short wchar_t;
#define _WCHAR_T_DEFINED
#endif

#pragma push_macro("_W64")
#if !defined(_W64)
#if !defined(__midl) && (defined(_X86_) || defined(_M_IX86)) && _MSC_VER >= 1300
#define _W64 __w64
#else
#define _W64
#endif
#endif

#ifdef _WIN64
typedef __int64 (__stdcall *FARPROC)();
typedef unsigned __int64 UINT_PTR;
#else
typedef int (__stdcall *FARPROC)();
typedef _W64 unsigned int UINT_PTR;
#endif

#pragma push_macro("UNALIGNED")
#if defined(_M_MRX000) || defined(_M_ALPHA) || defined(_M_PPC) || defined(_M_IA64) || defined(_M_AMD64)
#define UNALIGNED __unaligned
#else
#define UNALIGNED
#endif

/* internal macros */

#ifndef __DDBANNED_DLLIMPORT
#define __DDBANNED_DLLIMPORT __declspec(dllimport)
#endif

#ifndef __DDBANNED_DEPRECATED
#ifdef DDBANNED_NO_DEPRECATE
#define __DDBANNED_DEPRECATED
#else
#define __DDBANNED_DEPRECATED __declspec(deprecated)
#endif
#endif

#define __DDBANNED_FUNC_1(_ReturnType, _CallingConv, _FuncName, _TArg1) \
    __DDBANNED_DEPRECATED \
    __DDBANNED_DLLIMPORT \
    _ReturnType _CallingConv _FuncName(_TArg1);

#define __DDBANNED_FUNC_2(_ReturnType, _CallingConv, _FuncName, _TArg1, _TArg2) \
    __DDBANNED_DEPRECATED \
    __DDBANNED_DLLIMPORT \
    _ReturnType _CallingConv _FuncName(_TArg1, _TArg2);

#define __DDBANNED_INLINE_FUNC_2(_ReturnType, _CallingConv, _FuncName, _TArg1, _TArg2) \
    __DDBANNED_DEPRECATED \
    _ReturnType _CallingConv _FuncName(_TArg1, _TArg2);

#define __DDBANNED_FUNC_3(_ReturnType, _CallingConv, _FuncName, _TArg1, _TArg2, _TArg3) \
    __DDBANNED_DEPRECATED \
    __DDBANNED_DLLIMPORT \
    _ReturnType _CallingConv _FuncName(_TArg1, _TArg2, _TArg3);

#define __DDBANNED_FUNC_4(_ReturnType, _CallingConv, _FuncName, _TArg1, _TArg2, _TArg3, _TArg4) \
    __DDBANNED_DEPRECATED \
    __DDBANNED_DLLIMPORT \
    _ReturnType _CallingConv _FuncName(_TArg1, _TArg2, _TArg3, _TArg4);

/* winbase.h */

__DDBANNED_FUNC_2(char *, __stdcall, lstrcpy, char *, const char *);
__DDBANNED_FUNC_2(char *, __stdcall, lstrcpyA, _Out_writes_(_String_length_(lpString2) + 1) char *lpString1, _In_ const char *lpString2);
__DDBANNED_FUNC_2(wchar_t *, __stdcall, lstrcpyW, _Out_writes_(_String_length_(lpString2) + 1) wchar_t *lpString1, _In_ const wchar_t *lpString2);

__DDBANNED_FUNC_3(char *, __stdcall, lstrcpyn, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, lstrcpynA, _Out_writes_(iMaxLength) char * lpString1, _In_ const char *lpString2, _In_ int iMaxLength);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, lstrcpynW, _Out_writes_(iMaxLength) wchar_t * lpString1, _In_ const wchar_t *lpString2, _In_ int iMaxLength);

__DDBANNED_FUNC_2(char *, __stdcall, lstrcat, char *, const char *);
__DDBANNED_FUNC_2(char *, __stdcall, lstrcatA, _Inout_updates_z_(_String_length_(lpString1) + _String_length_(lpString2) + 1) char *lpString1, _In_ const char *lpString2);
__DDBANNED_FUNC_2(wchar_t *, __stdcall, lstrcatW, _Inout_updates_z_(_String_length_(lpString1) + _String_length_(lpString2) + 1) wchar_t *lpString1, _In_ const wchar_t *lpString2);

__DDBANNED_FUNC_3(char *, __stdcall, lstrcatn, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, lstrcatnA, char *, const char *, int);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, lstrcatnW, wchar_t *, const wchar_t *, int);

__DDBANNED_FUNC_1(int, __stdcall, IsBadCodePtr, _In_opt_ FARPROC);
__DDBANNED_FUNC_2(int, __stdcall, IsBadHugeReadPtr, _In_opt_ const void *, _In_ UINT_PTR);
__DDBANNED_FUNC_2(int, __stdcall, IsBadHugeWritePtr, _In_opt_ void *, _In_ UINT_PTR);
__DDBANNED_FUNC_2(int, __stdcall, IsBadReadPtr, _In_opt_ const void *, _In_ UINT_PTR);
__DDBANNED_FUNC_2(int, __stdcall, IsBadStringPtr, _In_opt_ const char *, _In_ UINT_PTR);
__DDBANNED_FUNC_2(int, __stdcall, IsBadStringPtrA, _In_opt_ const char *, _In_ UINT_PTR);
__DDBANNED_FUNC_2(int, __stdcall, IsBadStringPtrW, _In_opt_ const wchar_t *, _In_ UINT_PTR);
__DDBANNED_FUNC_2(int, __stdcall, IsBadWritePtr, _In_opt_ void *, _In_ UINT_PTR);

/* winuser.h */

__DDBANNED_FUNC_3(int, __stdcall, wvsprintf, char *, const char *, va_list);
__DDBANNED_FUNC_3(int, __stdcall, wvsprintfA, _Out_ char *, _In_ _Printf_format_string_ const char *, _In_ va_list);
__DDBANNED_FUNC_3(int, __stdcall, wvsprintfW, _Out_ wchar_t *, _In_ _Printf_format_string_ const wchar_t *, _In_ va_list);

__DDBANNED_FUNC_3(int, __stdcall, wsprintf, char *, const char *, ...);
__DDBANNED_FUNC_3(int, __stdcall, wsprintfA, _Out_ char *, _In_ _Printf_format_string_ const char *, ...);
__DDBANNED_FUNC_3(int, __stdcall, wsprintfW, _Out_ wchar_t *, _In_ _Printf_format_string_ const wchar_t *, ...);

/* shlwapi.h */

__DDBANNED_FUNC_2(char *, __stdcall, StrCpy, char *, const char *);
__DDBANNED_FUNC_2(char *, __stdcall, StrCpyA, _Out_ char *, _In_ const char *);
__DDBANNED_FUNC_2(wchar_t *, __stdcall, StrCpyW, _Out_ wchar_t *, _In_ const wchar_t *);

__DDBANNED_FUNC_3(char *, __stdcall, StrCpyN, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, StrCpyNA, _Out_writes_(cchMax) char *, _In_ const char *, int cchMax);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, StrCpyNW, _Out_writes_(cchMax) wchar_t *, _In_ const wchar_t *, int cchMax);

__DDBANNED_FUNC_3(char *, __stdcall, StrNCpy, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, StrNCpyA, char *, const char *, int);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, StrNCpyW, wchar_t *, const wchar_t *, int);

__DDBANNED_FUNC_2(char *, __stdcall, StrCat, char *, const char *);
__DDBANNED_FUNC_2(char *, __stdcall, StrCatA, _Inout_ char *, _In_ const char *);
__DDBANNED_FUNC_2(wchar_t *, __stdcall, StrCatW, _Inout_ wchar_t *, _In_ const wchar_t *);

__DDBANNED_FUNC_3(char *, __stdcall, StrCatN, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, StrCatNA, char *, const char *, int);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, StrCatNW, wchar_t *, const wchar_t *, int);

__DDBANNED_FUNC_3(char *, __stdcall, StrNCat, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, StrNCatA, _Inout_updates_(cchMax) char *, const char *, int cchMax);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, StrNCatW, _Inout_updates_(cchMax) wchar_t *, const wchar_t *, int cchMax);

__DDBANNED_FUNC_3(char *, __stdcall, StrCatBuff, char *, const char *, int);
__DDBANNED_FUNC_3(char *, __stdcall, StrCatBuffA, _Inout_updates_(cchDestBuffSize) char *, _In_ const char *, int cchDestBuffSize);
__DDBANNED_FUNC_3(wchar_t *, __stdcall, StrCatBuffW, _Inout_updates_(cchDestBuffSize) wchar_t *, _In_ const wchar_t *, int cchDestBuffSize);

__DDBANNED_FUNC_4(int, __stdcall, wvnsprintf, char *, int, const char *, va_list);
__DDBANNED_FUNC_4(int, __stdcall, wvnsprintfA, _Out_writes_(cchDest) char *, _In_ int cchDest, _In_ _Printf_format_string_ const char *, _In_ va_list);
__DDBANNED_FUNC_4(int, __stdcall, wvnsprintfW, _Out_writes_(cchDest) wchar_t *, _In_ int cchDest, _In_ _Printf_format_string_ const wchar_t *, _In_ va_list);

__DDBANNED_FUNC_4(int, __stdcall, wnsprintf, char *, int, const char *, ...);
__DDBANNED_FUNC_4(int, __stdcall, wnsprintfA, _Out_writes_(cchDest) char *, _In_ int cchDest, _In_ _Printf_format_string_ const char *, ...);
__DDBANNED_FUNC_4(int, __stdcall, wnsprintfW, _Out_writes_(cchDest) wchar_t *, _In_ int cchDest, _In_ _Printf_format_string_ const wchar_t *, ...);

__DDBANNED_FUNC_4(unsigned long, __stdcall, StrCatChainW, _Out_writes_(cchDst) wchar_t *, unsigned long cchDst, unsigned long, _In_ const wchar_t *);

/* uastrfnc.h */

__DDBANNED_INLINE_FUNC_2(UNALIGNED wchar_t *, , ualstrcpyW, UNALIGNED wchar_t *, UNALIGNED const wchar_t *);

/* remove defines */

#pragma pop_macro("_W64")
#pragma pop_macro("UNALIGNED")

#ifdef __cplusplus
}
#endif

#endif  /* _INC_DDBANNED */
