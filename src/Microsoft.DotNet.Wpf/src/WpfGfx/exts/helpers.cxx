// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:
//      This file contains generic helper routines for debugger extensions.
//
//-----------------------------------------------------------------------------

#include "precomp.hxx"


//+----------------------------------------------------------------------------
//
//  Function:
//      GetFirstSymbolEntry
//
//  Synopsis:
//      Return the first symbol entry of a symbol identified by name.
//
//-----------------------------------------------------------------------------

HRESULT GetFirstSymbolEntry(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in PCSTR szName,
    __out_ecount(1) PDEBUG_SYMBOL_ENTRY pInfo,
    __inout_ecount_opt(1) OutputControl *pOutCtl
    )
{
    HRESULT hr;

    DEBUG_MODULE_AND_ID SymbolId;
    ULONG Entries = 1;

    IFC(Symbols->GetSymbolEntriesByName(
        szName,
        0,
        &SymbolId,
        1,
        NULL//&Entries
        ));

    if (pOutCtl && Entries > 1)
    {
        pOutCtl->OutWarn("Found %u symbol entries for %s.  Using first entry.\n",
                         Entries, szName);
    }

    IFC(Symbols->GetSymbolEntryInformation(&SymbolId, pInfo));

Cleanup:

    if (pOutCtl && FAILED(hr))
    {
        pOutCtl->OutErr("Symbol entry lookup failed for %s.\n", szName);
    }    

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:  GetOffsetByNameAndPrintErrors
//
//  Synopsis:  Return the location of a symbol identified by name.
//
//-----------------------------------------------------------------------------

HRESULT GetOffsetByNameAndPrintErrors(
    __inout_ecount(1) OutputControl *pOutCtl,
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in PCSTR szName,
    __out_ecount(1) ULONG64* puOffset
    )
{
    HRESULT hr = Symbols->GetOffsetByName(szName, puOffset);

    if (FAILED(hr))
    {
        pOutCtl->OutErr("Symbol lookup failed.  Unable to locate %s\n", szName);
    }    

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:  GetNameByOffset
//
//  Synopsis:  Returns the name of the symbol at the specified location 
//             in the target's virtual address space.
//
//-----------------------------------------------------------------------------

HRESULT GetNameByOffset(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    ULONG64 u64Offset,
    ULONG cbName,
    __out_bcount(cbName) PSTR szName,
    __out_ecount_opt(1) ULONG64* pu64Displacement,
    __inout_ecount_opt(1) OutputControl *pOutCtl
    )
{
    HRESULT hr = Symbols->GetNameByOffset(
        u64Offset,
        szName,
        cbName,
        NULL,
        pu64Displacement
        );

    if (hr == S_FALSE)
    {
        if (pOutCtl)
        {
            pOutCtl->OutErr("Symbol lookup truncated name at offset %I64x\n", u64Offset);
        }

        hr = S_OK;
    }
    else if (hr == E_FAIL)
    {
        if (pOutCtl)
        {
            pOutCtl->OutErr("Symbol lookup failed to find symbol at offset %I64x\n", u64Offset);
        }
    }

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:  GetModuleByModuleNameAndPrintErrors
//
//  Synopsis:  Searches through the target's modules for one with 
//             the specified name.
//
//-----------------------------------------------------------------------------

HRESULT GetModuleByModuleNameAndPrintErrors(
    __inout_ecount(1) OutputControl *pOutCtl,
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    __in PCSTR szModuleName,
    __out_ecount(1) ULONG64 *puModule
    )
{
    HRESULT hr = 
        Symbols->GetModuleByModuleName(
            szModuleName, 
            /* StartIndex */ 0, 
            /* Index */ NULL, 
            puModule
            );

    if (hr == E_NOINTERFACE)
    {
        pOutCtl->OutErr("No module with specified name '%s' found.\n", szModuleName);
    }
    else if (FAILED(hr))
    {
        pOutCtl->OutErr("Module lookup by module name '%s' failed\n", szModuleName);
    }

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:
//      GetFieldEntry
//
//  Synopsis:
//      Returns the field type Id and offset of a named field from given type.
//
//-----------------------------------------------------------------------------

#define HANDLE_INDEXED_FIELDS   1

HRESULT GetFieldEntry(
    __inout_ecount(1) IDebugSymbols3 *Symbols,
    ULONG64 ContainerModuleBase,
    ULONG ContainerTypeId,
    __in PCSTR szFieldName,
    __out_ecount(1) PDEBUG_FIELD_ENTRY pFieldInfo,
    __inout_ecount_opt(1) OutputControl *pOutCtl
    )
{
    HRESULT hr;

    pFieldInfo->ModuleBase = ContainerModuleBase;
    pFieldInfo->ContainerTypeId = ContainerTypeId;
    pFieldInfo->Flags = 0;

    PCSTR pszFieldName = szFieldName;

    #if HANDLE_INDEXED_FIELDS
    char szBareFieldName[128];
    char *pszBareFieldName = szBareFieldName;
    ULONG SizeOf_pszBareFieldName = sizeof(szBareFieldName);

    PCSTR pArrayDelimiter = strchr(szFieldName, '[');
    ULONG uIndex = 0;

    // Make sure we have a valid index
    if (pArrayDelimiter)
    {
        if (pArrayDelimiter[1] == ']')
        {
            pArrayDelimiter = NULL;
        }
        else
        {
            if (!isdigit(static_cast<unsigned char>(pArrayDelimiter[1])))
            {
                IFC(E_INVALIDARG);
            }

            uIndex = strtoul(pArrayDelimiter+1, NULL, 0);

            if (uIndex == ULONG_MAX) { IFC(E_INVALIDARG); }
        }
    }

    if (pArrayDelimiter)
    {
        size_t BareNameSize = (pArrayDelimiter - szFieldName) + sizeof(szBareFieldName[0]);
        if (BareNameSize > SizeOf_pszBareFieldName)
        {
            pszBareFieldName = new char [BareNameSize/sizeof(szBareFieldName[0])];
            if (!pszBareFieldName || BareNameSize > ULONG_MAX)
            {
                IFC(E_OUTOFMEMORY);
            }
            SizeOf_pszBareFieldName = static_cast<ULONG>(BareNameSize);
        }

        BareNameSize -= sizeof(szBareFieldName[0]);
        RtlCopyMemory(pszBareFieldName, szFieldName, BareNameSize);
        pszBareFieldName[BareNameSize/sizeof(szBareFieldName[0])] = 0;
        pszFieldName = pszBareFieldName;
    }
    #endif

    hr = Symbols->GetFieldTypeAndOffset(
            ContainerModuleBase,
            ContainerTypeId,
            pszFieldName,
            &pFieldInfo->TypeId,
            &pFieldInfo->Offset
        );

    if (FAILED(hr))
    {
        if (pOutCtl)
        {
            if (hr == E_NOINTERFACE)
            {
                pOutCtl->OutErr("No field with specified name '%s' found.\n", szFieldName);
            }
            else
            {
                pOutCtl->OutErr("Field lookup by field name '%s' failed\n", szFieldName);
            }
        }
    }
    else
    {
        #if HANDLE_INDEXED_FIELDS
        if (pArrayDelimiter)
        {
            ULONG uNameSize = 0;
            IFC(Symbols->GetTypeName(
                pFieldInfo->ModuleBase,
                pFieldInfo->TypeId,
                NULL, 0,
                &uNameSize
                ));

            if (uNameSize > SizeOf_pszBareFieldName)
            {
                if (pszBareFieldName != szBareFieldName) { delete [] pszBareFieldName; }
                pszBareFieldName = new char[uNameSize/sizeof(*pszBareFieldName)];
                if (!pszBareFieldName) { IFC(E_OUTOFMEMORY); }
                SizeOf_pszBareFieldName = uNameSize;
            }

            IFC(Symbols->GetTypeName(
                pFieldInfo->ModuleBase,
                pFieldInfo->TypeId,
                pszBareFieldName,
                SizeOf_pszBareFieldName,
                NULL
                ));

            PSTR pTypeArrayDelimiter = strchr(pszBareFieldName, '[');
            if (!pTypeArrayDelimiter) { IFC(E_UNEXPECTED); }
            *pTypeArrayDelimiter = 0;

            IFC(Symbols->GetTypeId(
                pFieldInfo->ModuleBase,
                pszBareFieldName,
                &pFieldInfo->TypeId
                ));
        }
        #endif

        hr = Symbols->GetTypeSize(
            pFieldInfo->ModuleBase,
            pFieldInfo->TypeId,
            &pFieldInfo->Size
            );

        if (FAILED(hr) && pOutCtl)
        {
            pOutCtl->OutErr("Type size look up for field '%s' failed\n", pszFieldName);
        }

        #if HANDLE_INDEXED_FIELDS
        if (SUCCEEDED(hr) && pArrayDelimiter)
        {
            // Increase offset according to given index
            pFieldInfo->Offset += pFieldInfo->Size * uIndex;
        }
        #endif
    }

#if HANDLE_INDEXED_FIELDS
Cleanup:
    if (pszBareFieldName != szBareFieldName)
    {
        delete [] pszBareFieldName;
    }
#endif

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Function:  IsOutOfMemory
//
//  Synopsis:  Returns true if the HRESULT is a known out-of-memory code.
//
//-----------------------------------------------------------------------------

bool IsOutOfMemory(HRESULT hr)
{
    return (hr == E_OUTOFMEMORY)
        || (hr == HRESULT_FROM_WIN32(ERROR_OUTOFMEMORY))
        || (hr == HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY))
        || (hr == HRESULT_FROM_WIN32(ERROR_NO_SYSTEM_RESOURCES))
        || (hr == HRESULT_FROM_NT(STATUS_INSUFFICIENT_RESOURCES))
        || (hr == HRESULT_FROM_NT(STATUS_COMMITMENT_LIMIT));
}


