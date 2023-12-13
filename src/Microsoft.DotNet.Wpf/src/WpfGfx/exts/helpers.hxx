// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:
//      This file contains type and helper routines declarations for debugger
//      extensions.
//
//-----------------------------------------------------------------------------

#pragma once


//+----------------------------------------------------------------------------
//
//  Structure:
//      DEBUG_TYPE_ENTRY
//
//  Synopsis:
//      Type Entry similar to DEBUG_SYMBOL_ENTRY, but without symbol specific
//      data
//
//-----------------------------------------------------------------------------

typedef struct _DEBUG_TYPE_ENTRY
{
    ULONG64 ModuleBase;
    ULONG Size;
    ULONG Flags;
    ULONG TypeId;
} DEBUG_TYPE_ENTRY, *PDEBUG_TYPE_ENTRY;

//+----------------------------------------------------------------------------
//
//  Structure:
//      DEBUG_FIELD_ENTRY
//
//  Synopsis:
//      Type Entry (DEBUG_TYPE_ENTRY) but field specific data
//
//-----------------------------------------------------------------------------

typedef struct _DEBUG_FIELD_ENTRY : public DEBUG_TYPE_ENTRY
{
    ULONG Offset;
    ULONG ContainerTypeId;
} DEBUG_FIELD_ENTRY, *PDEBUG_FIELD_ENTRY;



HRESULT GetFirstSymbolEntry(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in PCSTR szName,
    __out_ecount(1) PDEBUG_SYMBOL_ENTRY pInfo,
    __inout_ecount_opt(1) OutputControl *pOutCtl
    );

HRESULT GetOffsetByNameAndPrintErrors(
    __inout_ecount(1) OutputControl *pOutCtl,
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in PCSTR szName,
    __out_ecount(1) ULONG64* puOffset
    );

HRESULT GetNameByOffset(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    ULONG64 u64Offset,
    ULONG cbName,
    __out_bcount(cbName) PSTR szName,
    __out_ecount_opt(1) ULONG64* pu64Displacement,
    __inout_ecount_opt(1) OutputControl *pOutCtl = NULL
    );

HRESULT GetModuleByModuleNameAndPrintErrors(
    __inout_ecount(1) OutputControl *pOutCtl,
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in PCSTR szModuleName,
    __out_ecount(1) ULONG64 *puModule
    );

HRESULT GetFieldEntry(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    ULONG64 Module,
    ULONG ContainerTypeId,
    __in PCSTR szFieldName,
    __out_ecount(1) PDEBUG_FIELD_ENTRY pFieldInfo,
    __inout_ecount_opt(1) OutputControl *pOutCtl = NULL
    );

inline HRESULT GetFieldEntry(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in_ecount(1) DEBUG_TYPE_ENTRY const *pContainerType,
    __in PCSTR szFieldName,
    __out_ecount(1) PDEBUG_FIELD_ENTRY pFieldInfo,
    __inout_ecount_opt(1) OutputControl *pOutCtl = NULL
    )
{
    return GetFieldEntry(
        Symbols,
        pContainerType->ModuleBase,
        pContainerType->TypeId,
        szFieldName,
        pFieldInfo,
        pOutCtl
        );
}

bool IsOutOfMemory(HRESULT hr);

HRESULT PrettyPrintPointer(
    __inout_ecount(1) OutputControl *pOutCtl,
    ULONG64 u64Pointer,
    __out_ecount(ceBuffer) PSTR szBuffer,
    size_t ceBuffer
    );


