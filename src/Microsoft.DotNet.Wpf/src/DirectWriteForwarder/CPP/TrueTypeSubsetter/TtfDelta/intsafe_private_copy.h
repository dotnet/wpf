// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __INTSAFE_PRIVATE_COPY_H
#define __INTSAFE_PRIVATE_COPY_H

// The following code is copied from intsafe.h since we needed to mark them
// as SecurityCritical because they have unverfiable code and we could not
// negotiate with the CRT team to fix it.

#define INTSAFE_E_ARITHMETIC_OVERFLOW   ((HRESULT)0x80070216L)  // 0x216 = 534 = ERROR_ARITHMETIC_OVERFLOW
typedef unsigned __int64    ULONGLONG;
#define UINT_ERROR      0xffffffff
#define ULONG_ERROR     0xffffffffUL

//
// UInt32x32To64 macro
//
#if defined(MIDL_PASS) || defined(RC_INVOKED) || defined(_M_CEE_PURE) \
    || defined(_68K_) || defined(_MPPC_) \
    || defined(_M_IA64) || defined(_M_AMD64)
// DevDiv LKG RC Changes
// Description: Define only if it is not already defined.
// added conditional to note conflict with public\ddk\inc\ntdef.h
#ifndef UInt32x32To64
#define UInt32x32To64(a, b) (((unsigned __int64)((unsigned int)(a))) * ((unsigned __int64)((unsigned int)(b))))
#endif
#elif defined(_M_IX86) || defined(_M_ARM)
#ifndef UInt32x32To64
#define UInt32x32To64(a, b) ((unsigned __int64)(((unsigned __int64)((unsigned int)(a))) * ((unsigned int)(b))))
#endif
#else
#error Must define a target architecture.
#endif

#if defined(_ARM_WORKAROUND_)
// without this workaround we get an internal compiler error
#undef UInt32x32To64
#define UInt32x32To64(a, b) ((unsigned __int64)(((unsigned int)(a)) * ((unsigned int)(b))))
#endif

//End DevDiv LKG RC Changes


//
// UINT addition
//
__checkReturn
__inline
HRESULT
UIntAdd(
    __in UINT uAugend,
    __in UINT uAddend,
    __out __deref_out_range(==, uAugend + uAddend) UINT* puResult)
{
    HRESULT hr;

    if ((uAugend + uAddend) >= uAugend)
    {
        *puResult = (uAugend + uAddend);
        hr = S_OK;
    }
    else
    {
        *puResult = UINT_ERROR;
        hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
    
    return hr;
}

//
// ULONGLONG -> ULONG conversion
//
__checkReturn
__inline
HRESULT
ULongLongToULong(
    __in ULONGLONG ullOperand,
    __out __deref_out_range(==, ullOperand) ULONG* pulResult)
{
    HRESULT hr;
    
    if (ullOperand <= ULONG_MAX)
    {
        *pulResult = (ULONG)ullOperand;
        hr = S_OK;
    }
    else
    {
        *pulResult = ULONG_ERROR;
        hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
    
    return hr;
}

//
// ULONG multiplication
//
__checkReturn
__inline
HRESULT
ULongMult(
    __in ULONG ulMultiplicand,
    __in ULONG ulMultiplier,
    __out __deref_out_range(==, ulMultiplicand * ulMultiplier) ULONG* pulResult)
{
    ULONGLONG ull64Result = UInt32x32To64(ulMultiplicand, ulMultiplier);
    
    return ULongLongToULong(ull64Result, pulResult);
}

//
// ULONG subtraction
//
__checkReturn
__inline
HRESULT
ULongSub(
    __in ULONG ulMinuend,
    __in ULONG ulSubtrahend,
    __out __deref_out_range(==, ulMinuend - ulSubtrahend) ULONG* pulResult)
{
    HRESULT hr;

    if (ulMinuend >= ulSubtrahend)
    {
        *pulResult = (ulMinuend - ulSubtrahend);
        hr = S_OK;
    }
    else
    {
        *pulResult = ULONG_ERROR;
        hr = INTSAFE_E_ARITHMETIC_OVERFLOW;
    }
    
    return hr;
}
#endif //__INTSAFE_PRIVATE_COPY_H
