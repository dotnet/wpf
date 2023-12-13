// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:  Implementations of the stack capture debug extension.
//
//-----------------------------------------------------------------------------

#pragma once

//+----------------------------------------------------------------------------
//
//  Structure:
//      StackCaptureFrame
//
//  Synopsis:
//      Stores captured stack failures.  The offsets to the target fields are
//      read into CStackCaptureFrameConverter::m_rgTargetOffset.  Local offsets
//      ans sizes are static and stored in s_rgFieldLocalType.
//
//-----------------------------------------------------------------------------

typedef struct
{
    HRESULT hrFailure;
    DWORD   dwThreadId;
    UINT    uLineNumber;
    ULONG64 rgCapturedFrame[3];
} StackCaptureFrame;


//+----------------------------------------------------------------------------
//
//  Function:  DumpCaptureImpl
//
//  Synopsis:  Prints out the last uNumberOfCaptureCollections stack captures.
//
//-----------------------------------------------------------------------------

HRESULT DumpCaptureImpl(
    __in_ecount(1) OutputControl *pOutCtl,
    __in_ecount(1) IDebugDataSpaces *pIData, 
    __in_ecount(1) IDebugSymbols3 *pISymbols,
    __in_ecount(1) IDebugSystemObjects4 *pISystemObjects,
    DWORD StackOutputFlags,
    __in_ecount(1) DEBUG_VALUE const &ThreadIdFilter,
    __in_ecount(1) DEBUG_VALUE const &HRESULTFilter,
    __in PCSTR szModuleName,
    ULONG uNumberOfCaptureCollections,
    __out_ecount_opt(1) StackCaptureFrame *pLastCapturedFrame
    );


//+----------------------------------------------------------------------------
//
//  Function:
//      DumpStackCaptureFrame
//
//  Synopsis:
//      Prints a line of the stack capture dump.
//
//-----------------------------------------------------------------------------

HRESULT DumpStackCaptureFrame(
    __in_ecount(1) OutputControl *pOutCtl,
    __in_ecount(1) IDebugSymbols3 *pISymbols,
    DWORD Flags,
    ULONG64 u64CaptureSymbol,
    ULONG uCaptureLine
    );


