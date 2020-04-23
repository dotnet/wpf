// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#ifndef __TABINC_H_
#define __TABINC_H_

#include <TabAssert.h>

//////////////////////////////////////////////////////////////////////////////////////////////
//
// SECURITY
//

#define TPGSECURE(X, DEVELOPERS, REVIEW_DATE, EXPLANATION_WHY_SECURE) X

//
// Example:
//
//		pszFoo = new TCHAR[_tcslen(pszBar) + 1]
//		_tcscpy(pszFoo, pszBar);
//	change to:
//		pszFoo = new TCHAR[_tcslen(pszBar) + 1]
//		TPGSECURE(_tcscpy(pszFoo, pszBar), "JohnDoe", "2/12/2002", "pszFoo is allocated using length of pszBar");
//

//////////////////////////////////////////////////////////////////////////////////////////////
//
// HR and return value handling
//

#define IGNORERESULT(result) (void)result

// (using inlines for IGNOREHR, VERIFYHR, VERIFYBOOL to have type checking)

_inline void IGNOREHR(HRESULT hr)
{
hr;
}

_inline void VERIFYHR(HRESULT hr)
{
    hr;
#ifdef DBG
    ASSERT(SUCCEEDED(hr));
#endif
}

_inline void VERIFYBOOL(BOOL br)
{
    br;
#ifdef DBG
    ASSERT(br);
#endif
}

//////////////////////////////////////////////////////////////////////////////////////////////
//
// Additional String Primitives
//

HRESULT StringAllocateWithNewAndCopy   (__out LPTSTR * ppszDestination, __in LPTSTR pszSource);
HRESULT StringAllocateWithMallocAndCopy(__out LPTSTR * ppszDestination, __in LPTSTR pszSource);

//////////////////////////////////////////////////////////////////////////////////////////////
//
// SAFE primitives
//

#define ZEROSTRUCT(X) ZeroMemory(X, sizeof(*(X)))

#define SIZEOFSZ(X) (sizeof(X[0]) * (_tcslen(X) + 1))
#define SIZEOFSTRUCT(X) (sizeof(X))
#define SIZEOFARRAY(X) (sizeof(X))

#define LENGTHOFARRAY(X) (sizeof(X) / sizeof(X[0]))

//////////////////////////////////////////////////////////////////////////////////////////////

// Not all of the projects that include tabinc.h use ole automation;
// so "BSTR" wouldn't be defined for them. We check here for "_OLEAUTO_H_"
// to determine if ole automation is used. For this reason, tabinc.h
// should be included after oleauto.h

#ifdef _OLEAUTO_H_

BOOL IsBadReadBstr(BSTR bstr, BOOL fCheckForAndDisallowEmbeddedNulls); // implemented in tablib.cpp

_inline BOOL IsBadWriteBstr(BSTR * pbstr)
{
    return FALSE; // Banned API -> IsBadWritePtr(pbstr, sizeof(*pbstr));
}

#endif


//////////////////////////////////////////////////////////////////////////////////////////////

//////////////////////////////////////////////////////////////////////////////////////////////
// Macros and helpers for CRITICAL_SECTION

#define TPG_INITIALIZE_CRITICAL_SECTION_PREALLOC(pCritSect) InitializeCriticalSectionAndSpinCount(pCritSect, (0x8001000 | 4000))
#define TPG_INITIALIZE_CRITICAL_SECTION_NOPREALLOC(pCritSect) InitializeCriticalSectionAndSpinCount(pCritSect, (0x8000000 | 4000))

//////////////////////////////////////////////////////////////////////////////////////////////

// closes the *pHandle, if it's not NULL, and NULLs it out
void SafeCloseHandle(__inout HANDLE * pHandle);

//////////////////////////////////////////////////////////////////////////////////////////////

#endif  // __TABINC_H_

