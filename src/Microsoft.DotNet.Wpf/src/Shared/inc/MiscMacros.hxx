// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//*****************************************************************************
//
// File: MiscMacros.hxx
//
//    Defines useful macros for neat safe code
//
//
//
//
//
// 
//*****************************************************************************

//*****************************************************************************
//
//   THIS FILE IS INCLUDED BY UNMANAGED CODE. DO NOT INTRODUCE ANY MCPP SYNTAX. 
//
//*****************************************************************************

#ifndef MISC_MACROS_HXX
#define MISC_MACROS_HXX

#pragma once

#ifndef ARRAYSIZE
#define ARRAYSIZE RTL_NUMBER_OF_V2 // from DevDiv's WinNT.h
#endif

// Capturing the IP addresses at failure points in done in the projects under wcp\host 
// (PresentationHost, XPSViewer). See host\Shared\WatsonReportingShared.cxx.
#ifdef LOG_FAILURE_ADDRESSES
__declspec(noinline)
void __cdecl LogIPAddress();
#else
#define LogIPAddress() ;
#endif

// In order for these macros to work, one should use
// the following coding conventions:
//
//  "Cleanup:" needs to label the clean up section of your code for all the Check Macros
//  HRESULT hr = S_OK; (or HRESULT hr = E_FAIL;) needs to be declared for all the HR macros
//  BOOL fResult = TRUE; (or BOOL fResult = TRUE;) needs to be declared for all the BOOL macros  
// 

#define CHECK_HR(statement)                  \
{                                            \
    hr = statement;                          \
                                             \
    if (FAILED(hr))                          \
    {                                        \
       LogIPAddress();                       \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_BOOL(statement)                \
{                                            \
    fResult = statement;                     \
                                             \
    if (fResult == FALSE)                    \
    {                                        \
       LogIPAddress();                       \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_NONNULL(expr, failHr)          \
{                                            \
    if ((expr) != 0)                         \
    {                                        \
        hr = S_OK;                           \
    }                                        \
    else                                     \
    {                                        \
       LogIPAddress();                       \
       hr = failHr;                          \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_LRESULT(statement)             \
{                                            \
    lResult = statement;                     \
                                             \
    if (lResult != ERROR_SUCCESS)            \
    {                                        \
       LogIPAddress();                       \
       hr = E_FAIL;                          \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_SUCCESS_BOOL_TO_HR(fSuccess)   \
{                                            \
    fResult = statement;                     \
                                             \
    if (fResult == FALSE)                    \
    {                                        \
       LogIPAddress();                       \
       hr = E_FAIL;                          \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_SUCCESS_HR_TO_BOOL(fSuccess)   \
{                                            \
    hr = statement;                          \
                                             \
    if (FAILED(hr))                          \
    {                                        \
       LogIPAddress();                       \
       fResult = FALSE;                      \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_POINTER_ALLOC(ptr)             \
                                             \
    if ((ptr) == NULL)                       \
    {                                        \
       LogIPAddress();                       \
       hr = E_OUTOFMEMORY;                   \
       goto Cleanup;                         \
    }                                        \

#define CHECK_POINTER_ALLOC_BOOL(ptr)        \
{                                            \
    if ((ptr) == NULL)                       \
    {                                        \
       LogIPAddress();                       \
       fResult = FALSE;                      \
       goto Cleanup;                         \
    }                                        \
}
#define CHECK_POINTER_ARG(ptr)               \
                                             \
    if ((ptr) == NULL)                       \
    {                                        \
        LogIPAddress();                      \
        hr = E_POINTER;                      \
        goto Cleanup;                        \
    }                                        \

#define CHECK_POINTER(ptr)                   \
                                             \
    if ((ptr) == NULL)                       \
    {                                        \
        LogIPAddress();                      \
        hr = E_FAIL;                         \
        goto Cleanup;                        \
    }                                        \

#define CHECK_POINTER_BOOL(ptr)              \
                                             \
    if ((ptr) == NULL)                       \
    {                                        \
        LogIPAddress();                      \
        fResult = FALSE;                     \
        goto Cleanup;                        \
    }                                        \

#define CHECK_POINTER_ARG_BOOL(ptr)          \
                                             \
    if ((ptr) == NULL)                       \
    {                                        \
        LogIPAddress();                      \
        fResult = FALSE;                     \
        goto Cleanup;                        \
    }                                        \

#define RELEASE_POINTER(ptr)                 \
                                             \
       (ptr)->Release();                     \
       ptr = NULL;                           \

#define SAFERELEASE_POINTER(ptr)             \
{                                            \
       if (ptr)                              \
       {                                     \
            (ptr)->Release();                \
            ptr = NULL;                      \
       }                                     \
}
#ifndef ReleaseInterface
#define ReleaseInterface SAFERELEASE_POINTER
#endif

#define ReplaceInterface(x,y) {if (x) {(x)->Release();} (x)=(y); if (y) {(y)->AddRef();}}

#define DELETE_POINTER(ptr)                  \
{                                            \
       delete ptr;                           \
       ptr = NULL;                           \
}
#define DELETE_ARRAY_POINTER(ptr)            \
{                                            \
       delete[] ptr;                         \
       ptr = NULL;                           \
}
#define DELETE_BSTR(bstrXYZ)                 \
{                                            \
       SysFreeString(bstrXYZ);               \
       bstrXYZ = NULL;                       \
}
#define CHECK_NULL_FROM_WIN32(_p)            \
    if ( (_p) == NULL )                      \
    {                                        \
        LogIPAddress();                      \
        hr = HRESULT_FROM_WIN32( ::GetLastError() ); \
        goto Cleanup;                        \
    }                                        \

#define CHECK_ZERO_FROM_WIN32(_i)            \
    if ( (_i) == 0)                          \
    {                                        \
        LogIPAddress();                      \
        hr = HRESULT_FROM_WIN32( ::GetLastError() ); \
        goto Cleanup;                        \
    }                                        \

#define CHECK_BOOL_FROM_WIN32(_b)            \
    if ( (_b) == FALSE )                     \
    {                                        \
        LogIPAddress();                      \
        hr = HRESULT_FROM_WIN32( ::GetLastError() ); \
        goto Cleanup;                        \
    }                                        \

#define CHECK_ERROR_CODE(_win32error)        \
    if ( (_win32error) != NO_ERROR )         \
    {                                        \
        LogIPAddress();                      \
        hr = HRESULT_FROM_WIN32(_win32error);\
        goto Cleanup;                        \
    }                                        \

#define CHECK_WAIT_FOR_OBJECT(_o) \
    if (::WaitForSingleObject( (_o), INFINITE) != WAIT_OBJECT_0) \
    {                                        \
        hr = HRESULT_FROM_WIN32( ::GetLastError() ); \
        goto Cleanup;                        \
    }                                        \


#define BOOL_TO_HR(exp)           ((exp) ? S_OK : E_FAIL)
#define BOOL_TO_RETURN_CODE(exp)  ((exp) ? 0 : -1)

#define HR_TO_BOOL(hr)            (SUCCEEDED(hr) ? TRUE : FALSE)
#define HR_TO_RETURN_CODE(hr)     (SUCCEEDED(hr) ? 0 : -1)

#define RETURN_CODE_TO_HR(rc)     ((rc == 0) ? S_OK : E_FAIL)        
#define RETURN_CODE_TO_BOOL(rc)   ((rc == 0) ? TRUE : FALSE)

//*****************************************************************************
//
// Short forms
//
//*****************************************************************************

#define CKHR           CHECK_HR
#define CKB            CHECK_SUCCESS_BOOL
#define CKLR           CHECK_LRESULT
#define CKBHR          CHECK_SUCCESS_BOOL_TO_HR
#define CK_ALLOC       CHECK_POINTER_ALLOC
#define CK_ALLOCB      CHECK_POINTER_ALLOC_BOOL
#define CK_PTR         CHECK_POINTER_ALLOC
#define CK_PTRB        CHECK_POINTER_ALLOC_BOOL
#define CK_PARG        CHECK_POINTER_ARG
#define CK_PARGB       CHECK_POINTER_ARG_BOOL
#define DEL_PTR        DELETE_POINTER
#define DEL_ARRAY      DELETE_ARRAY_POINTER
#define DEL_BSTR       DELETE_BSTR

#ifndef ARRAY_SIZE  // Check that the Ashraf version is not defined
#define ARRAY_SIZE(x) (sizeof(x) / sizeof(x[0]))
#endif

//*****************************************************************************
//
//   Some of my macro's have coorespong versions amoung Ashraf's favorites
//
//   I like my short forms better, but for compatibility I provide the same
//   defined name where my naming differs.
//
//*****************************************************************************

#ifndef IFC // Check that the Ashraf version is not defined
#define IFC CHECK_HR
#endif 

#ifndef IFCOOM // Check that the Ashraf version is not defined
#define IFCOOM CHECK_POINTER_ALLOC
#endif

#ifndef CHECKPTRARG // Check that the Ashraf version is not defined
#define CHECKPTRARG CHECK_POINTER_ARG
#endif

#endif  // MISC_MACROS_HXX
