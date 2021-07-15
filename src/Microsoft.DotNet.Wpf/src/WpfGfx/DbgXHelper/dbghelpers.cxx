// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//---------------------------------------------------------------------------------
//

//
// File: helpers.cxx
//---------------------------------------------------------------------------------

#include "precomp.hxx"


/**************************************************************************\
* OutputInstance
*
* Output an instance of a type.
*
* Wrote it.
\**************************************************************************/
void OutputInstance(
    PDEBUG_CLIENT Client,
    PCSTR typeName,
    ULONG64 typeAddress,
    bool fVerbose
    )
{
    OutputControl OutCtl(Client);

    if (fVerbose)
    {
        char szDTCommand[1000];

        if (SUCCEEDED(StringCchPrintfA(szDTCommand, ARRAYSIZE(szDTCommand), "dt %s %p", typeName, typeAddress)))
        {
            OutCtl.Output("%s\n", szDTCommand);
            OutCtl.Execute(szDTCommand, 0);
        }
    }
    else
    {
        OutCtl.Output("%s %p\n", typeName, typeAddress);
    }
}

/**************************************************************************\
* GetFieldOffset
*
* Gets the offset of a field from a given type.
*
* Wrote it.
\**************************************************************************/
HRESULT GetFieldOffset(
    PDEBUG_CLIENT Client,
    PCSTR typeName,
    PCSTR fieldName,
    __out ULONG* pFieldOffset
    )
{
    HRESULT hr = S_OK;
    OutputControl OutCtl(Client);
    PDEBUG_SYMBOLS Symbols = NULL;
    ULONG typeId;
    ULONG64 module;

    *pFieldOffset = ULONG_MAX;

    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols))) goto Cleanup;    

    if (FAILED(hr = Symbols->GetSymbolTypeId(typeName, &typeId, &module)))
    {
        OutCtl.Output("Couldn't find type %s: %p\n", typeName, hr);
        goto Cleanup;
    }

    if (FAILED(hr = Symbols->GetFieldOffset(module, typeId, fieldName, pFieldOffset)))
    {
        OutCtl.Output("Couldn't find field %s on type %s: %p\n", fieldName, typeName, hr);
        goto Cleanup;
    }

Cleanup:
    ReleaseInterface(Symbols);
    return hr;
}

/**************************************************************************\
* ReadPointerField
*
* Reads a pointer-sized field for a type from a specified address.
*
* Wrote it.
\**************************************************************************/
HRESULT ReadPointerField(
    PDEBUG_CLIENT Client,
    ULONG64 typeAddress,
    PCSTR typeName,
    PCSTR fieldName,
    __out ULONG64* pFieldValue
    )
{
    HRESULT hr = S_OK;
    OutputControl OutCtl(Client);
    PDEBUG_DATA_SPACES Data = NULL;
    ULONG fieldOffset;

    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data))) goto Cleanup;

    if (FAILED(hr = GetFieldOffset(Client, typeName, fieldName, &fieldOffset))) goto Cleanup;

    if (FAILED(hr = Data->ReadPointersVirtual(1, typeAddress + fieldOffset, pFieldValue)))
    {
        //OutCtl.Output("Couldn't read %s.%s off pointer %p (offset %p): %p\n", typeName, fieldName, typeAddress, fieldOffset, hr);
        goto Cleanup;
    }

Cleanup:
    ReleaseInterface(Data);
    return hr;
}

/**************************************************************************\
* ReadNonPointerField
*
* Reads a specified-sized field for a type from a specified address.
*
* Wrote it.
\**************************************************************************/
HRESULT ReadNonPointerField(
    PDEBUG_CLIENT Client,
    ULONG64 typeAddress,
    PCSTR typeName,
    PCSTR fieldName,
    ULONG fieldSize,
    __out VOID* pFieldValue
    )
{
    HRESULT hr = S_OK;
    OutputControl OutCtl(Client);
    PDEBUG_DATA_SPACES Data = NULL;
    ULONG fieldOffset;

    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data))) goto Cleanup;

    if (FAILED(hr = GetFieldOffset(Client, typeName, fieldName, &fieldOffset))) goto Cleanup;

    if (FAILED(hr = Data->ReadVirtual(typeAddress + fieldOffset, pFieldValue, fieldSize, NULL)))
    {
        //OutCtl.Output("Couldn't read %s.%s off pointer %p (offset %p, size %p): %p\n", typeName, fieldName, typeAddress, fieldOffset, fieldSize, hr);
        goto Cleanup;
    }

Cleanup:
    ReleaseInterface(Data);
    return hr;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        ReadSymbolNameByOffset
// 
//    Synopsis:
//        Reads the symbol name given an offset.
// 
//------------------------------------------------------------------------------

HRESULT
ReadSymbolNameByOffset(
    PDEBUG_CLIENT client,
    ULONG64 offset,
    ULONG nameBufferSize,
    __out_ecount_part(nameBufferSize, *pNameSize) PSTR nameBuffer,
    __out_ecount(1) ULONG *pNameSize
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(client);

    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    ULONG64 symbol = 0;

    IFC(client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));

    if (FAILED(hr = Data->ReadPointersVirtual(1, offset, &symbol)))
    {
        OutCtl.Output("ReadSymbolNameByOffset: failed to read symbol value from offset %p with HRESULT 0x%08x\n", offset, hr);
        goto Cleanup;
    }

    if (FAILED(hr = Symbols->GetNameByOffset(
            symbol,
            nameBuffer,
            nameBufferSize,
            pNameSize,
            NULL // Displacement
            )))
    {
        OutCtl.Output("ReadSymbolNameByOffset: failed to read name by offset %p with HRESULT 0x%08x\n", offset, hr);
        goto Cleanup;
    }

Cleanup:
    ReleaseInterface(Symbols);
    ReleaseInterface(Data);

    return hr;
}


/**************************************************************************\
* SearchTable
*
* Searches an NTRTL table for a particular value
*
* Wrote it.
\**************************************************************************/
HRESULT SearchTable(
    PDEBUG_CLIENT Client,
    ULONG64 ulpTableRoot, 
    ULONG ulFieldOffset, 
    ULONG64 ulValueToLookFor,
    __out ULONG64* ulpEntry
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);
    PDEBUG_DATA_SPACES Data = NULL;

    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data))) goto Cleanup;    

    ULONG offsetInsertOrderList;
    ULONG offsetFlink = 0;
    const int MAX_ELEMENTS = 5000;
    int numElements = 0;

    *ulpEntry = NULL;

    if (FAILED(hr = GetFieldOffset(Client, "RTL_GENERIC_TABLE", "InsertOrderList", &offsetInsertOrderList))) goto Cleanup;
    
    ULONG64 InsertOrderList = ulpTableRoot + offsetInsertOrderList;

    ULONG64 listCurrent = InsertOrderList;
    ULONG offsetToUserData = (OutCtl.IsPointer64Bit() == S_OK) ? 16 : 12;

    do
    {
        ULONG64 currentFieldValue;
        if (OutCtl.GetInterrupt() == S_OK)
        {
            OutCtl.Output("\n\nStop on user-interrupt.\n\n");
            hr = E_ABORT;
            break;
        }

        if (FAILED(hr = Data->ReadPointersVirtual(1, listCurrent+offsetFlink, &listCurrent)))
        {
            OutCtl.Output("Couldn't read listCurrent->Flink (pointer = %p): hr = %p\n", listCurrent, hr);
            break;
        }

        if (FAILED(hr = Data->ReadPointersVirtual(1, listCurrent + offsetToUserData + ulFieldOffset, &currentFieldValue)))
        {
            OutCtl.Output("Couldn't read field off table element pointer = %p, offset = %p: hr = %p\n", listCurrent, offsetToUserData + ulFieldOffset, hr);
            break;
        }

        if (currentFieldValue == ulValueToLookFor)
        {
            *ulpEntry = listCurrent + offsetToUserData;
            break;
        }
    } while (listCurrent != InsertOrderList && ++numElements < MAX_ELEMENTS);

    if (numElements >= MAX_ELEMENTS)
    {
        OutCtl.Output("\n\nReached max number of elements, stopping.\n\n");
    }

Cleanup:
    ReleaseInterface(Data);
    return hr;
}




