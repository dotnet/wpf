// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  Abstract:
//      Implementations of the render data debug extension.
//
//------------------------------------------------------------------------------

#include "precomp.hxx"

#include "cmdstruct.h"
#include "..\core\common\memreader.h"


VOID CopyPointer(ULONG64 *pDestination, VOID *pSource, UINT cbPointer)
{
    if (cbPointer == 8)
    {
        *pDestination = *reinterpret_cast<UINT64*>(pSource);
    }
    else
    {                
        *pDestination = *reinterpret_cast<UINT*>(pSource);              
    }
}

HRESULT ReadBatchAddressAndSize(
    LPCSTR BufferName,
    LPCSTR BufferSizeName,
    UINT64 *pTargetBufferPtr,
    UINT *pBufferSize,
    IDebugSymbols *pISymbols,
    IDebugDataSpaces *pIData,
    OutputControl *pOutCtl
    )
{
    HRESULT hr = S_OK;

    ULONG64 CmdBufferPtrPtr = 0;
    ULONG64 CmdBufferSizePtr = 0;

    ULONG64 CmdBufferPtr = 0;
    ULONG32 CmdBufferSize = 0;

    if (SUCCEEDED(hr))
    {
        hr = pISymbols->GetOffsetByName(BufferName, &CmdBufferPtrPtr);
        if (hr != S_OK)
        {
            pOutCtl->OutErr("Unable to locate %s\n", BufferName);
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = pISymbols->GetOffsetByName(BufferSizeName, &CmdBufferSizePtr);
        if (hr != S_OK)
        {
            pOutCtl->OutErr("Unable to locate %s\n", BufferSizeName);
        }
    }

    if (SUCCEEDED(hr))
    {
        hr = pIData->ReadPointersVirtual(1, CmdBufferPtrPtr, &CmdBufferPtr);
        *pTargetBufferPtr = CmdBufferPtr;
    }

    if (SUCCEEDED(hr))
    {
        hr = pIData->ReadVirtual(CmdBufferSizePtr, &CmdBufferSize, sizeof(UINT), NULL);
    }

    if (SUCCEEDED(hr))
    {
        *pBufferSize = CmdBufferSize;
    }   

    return hr;
}

HRESULT ReadBatchIntoBuffer(
    ULONG64 offsetOfListHead,
    void **ppvBuffer,
    const ULONG64 cbData,
    IDebugDataSpaces *pIData,
    OutputControl *pOutCtl    
    )
{
    HRESULT hr = S_OK;

    PVOID pvBuffer = NULL;

    BYTE *pbCurrentPosition;
    UINT cbRemainingBytes;

    const UINT CBPOINTER_32BIT = 4;
    const UINT CBPOINTER_64BIT = 8;
    UINT cbPointer;      

    BYTE headerBuffer[2 * CBPOINTER_64BIT +  2 * sizeof(UINT)];

    ULONG64 offsetOfCurrentBlock;    

    //
    // Validate input
    //

    if (cbData > UINT_MAX)
    {
        IFC(E_FAIL);
    }    

    cbRemainingBytes = static_cast<UINT>(cbData);

    //
    // Calculate sizes & offsets that are dependent on pointer size.
    //    

    IFC(pOutCtl->IsPointer64Bit());
    if (hr == S_OK)
    {
        cbPointer = CBPOINTER_64BIT;
    }
    else
    {
        cbPointer = CBPOINTER_32BIT;
        hr = S_OK;
    }

    //
    // Setup headers.  The first 2 items in the buffer we just allocated will be the
    // LIST_HEADER for this batch, and it's DataStreamBlock for this batch.
    //    

    // Size of Flink, Blink, cbAllocated, & cbWritten
    UINT CBLIST_ENTRY = 2 * cbPointer; // Size of flink & blink
    UINT CBDATASTREAMBLOCK_HEADER = CBLIST_ENTRY + 2 * sizeof(UINT);  // Size of LISTENTRY, cbAllocated, & cbWritten
    UINT OFFSET_CBWRITTEN = CBLIST_ENTRY + sizeof(UINT);

    // Attempt allocation
    
    pvBuffer = HeapAlloc(
        GetProcessHeap(), 
        0,   
        cbRemainingBytes                // Data
        );

    if (NULL == pvBuffer)
    {
        IFC(E_OUTOFMEMORY);
    }

    pbCurrentPosition = reinterpret_cast<BYTE*>(pvBuffer);

    //
    // Read list header
    //

    IFC(pIData->ReadVirtual(offsetOfListHead, headerBuffer, CBLIST_ENTRY, NULL));

    // Dereference ListHeader.Flink & copy to offsetOfCurrentBlock
    CopyPointer(&offsetOfCurrentBlock, headerBuffer, cbPointer);

    //        
    // Move forward through the list until the head of the list is encountered.
    //
    
    ULONG64 offsetOfPrevBlock = offsetOfListHead;

    while (offsetOfListHead != offsetOfCurrentBlock)
    {
        UINT cbWritten;

        //
        // Read the header to find list links and out how much data was written   
        //
        
        IFC(pIData->ReadVirtual(offsetOfCurrentBlock, headerBuffer, CBDATASTREAMBLOCK_HEADER, NULL));                

        // Check back link integrity
        ULONG64 CurrentBlockBlink;
        CopyPointer(&CurrentBlockBlink, headerBuffer+cbPointer, cbPointer);
        if (CurrentBlockBlink != offsetOfPrevBlock)
        {
            IGNORE_HR(pOutCtl->Output(
                "Malformed batch.  Block back link (0x%I64x) != prior block (0x%I64x).\n",
                CurrentBlockBlink,
                offsetOfPrevBlock
                ));
            IFC(E_FAIL);
        }

        cbWritten = *reinterpret_cast<UINT*>(headerBuffer + OFFSET_CBWRITTEN);

        // 
        // Attempt to read the current block
        //

        if (cbRemainingBytes >= cbWritten)
        {
            // Read the actual data into the buffer
            IFC(pIData->ReadVirtual(offsetOfCurrentBlock + CBDATASTREAMBLOCK_HEADER, pbCurrentPosition, cbWritten, NULL));
            pbCurrentPosition += cbWritten;
            cbRemainingBytes -= cbWritten;

            // Dereference CurrentBlock.Flink and copy to offsetOfCurrentBlock
            CopyPointer(&offsetOfCurrentBlock, headerBuffer, cbPointer);                        
        }
        else
        {
            IGNORE_HR(pOutCtl->Output(
                "Malformed batch.  Total batch size (%lu) is smaller than the sum of block's size so far (%lu).\n",
                cbData,
                cbData - cbRemainingBytes + cbWritten
                ));
            IFC(E_FAIL);
        }

        offsetOfPrevBlock = offsetOfCurrentBlock;
    }

    *ppvBuffer = pvBuffer;
    pvBuffer = NULL;

Cleanup:

    HeapFree(GetProcessHeap(), 0, pvBuffer);
    
    return hr;    
}

HRESULT OutputBatch(
    __inout_ecount(1) PDEBUG_CLIENT Client,
    __inout_ecount(1) IDebugSymbols *pISymbols,
    OutputControl *pOutCtl,
    PVOID pvBuffer,
    UINT cbSize)
{
    HRESULT hr = S_OK;

    CMilDataStreamReader cmdReader(pvBuffer, cbSize);

    UINT nItemID;
    PVOID pItemData;
    UINT cbItemSize;

    //
    // Now get the first item and start executing the render buffer.
    //

    IFC(cmdReader.GetFirstItemSafe(&nItemID, &pItemData, &cbItemSize));


    while (hr == S_OK)
    {
        //
        // Check to see if the data matches the type definitions. The
        // command ID should be one of the known ones and the size reported
        // in the data stream should match that expected for the given
        // command.
        //

        if (nItemID >= ARRAY_SIZE(MarshalCommands))
        {
            pOutCtl->Output(
                "command %d: out of range.\n",
                nItemID
                );
        }
        else
        {
            //   Debugger extensions should not have a table of types and associations
            //  Type information be read dynamically from the symbol files.
            MILCOMMAND &Command = MarshalCommands[nItemID];

            if (!Command.fTypePropertiesRead)
            {
                // Default to 0 indicating failed type read
                Command.size = 0;
                // Consider type properties read independent of success
                Command.fTypePropertiesRead = true;

                if (SUCCEEDED(GetTypeId(Client, Command.type, &Command.TypeId, &Command.TypeModule)))
                {
                    if (FAILED(pISymbols->GetTypeSize(
                        Command.TypeModule,
                        Command.TypeId,
                        &Command.size
                        )))
                    {
                        Command.size = 0;
                    }
                }

                if (Command.size == 0)
                {
                    IGNORE_HR(pOutCtl->OutWarn("Unable to read type size for Id %u (%s).\n",
                                     nItemID,
                                     Command.type
                                     ));
                }
            }

            if (Command.size == 0)
            {
                IGNORE_HR(pOutCtl->OutWarn("command %u: (%s - type info not available).\n",
                                 nItemID,
                                 Command.type
                                 ));
            }
            else if (   cbItemSize < Command.size
                     || (   cbItemSize > Command.size 
                         && !Command.fHasPayload))
            {
                pOutCtl->Output(
                    "command %d: incorrect size.\n",
                    nItemID,
                    Command.name
                    );
            }
            else
            {

                //
                // Output the record header - the type name and the ID.
                //

                pOutCtl->Output(
                    "%s (0x%x) SIZE:0x%x",
                    Command.name,
                    nItemID,
                    cbItemSize
                    );

                if (cbItemSize > Command.size)
                {
                    pOutCtl->Output(
                        "\nWarning: Size of the command as written to the byte stream is "
                        "larger than the size of the command obtained from the header files.\n"
                        "This is either a variable length command, or the debugger extension is "
                        "mismatched against against the binary you are debugging.\n\n"
                        );
                }

                if (cbItemSize > 0)            
                {
                    UINT cbLine = 0;
                    DWORD *pCurrent = reinterpret_cast<DWORD*>(pItemData);

                    // Display item data in DWORD-sized chunks
                    pOutCtl->Output("\n\t");        
                    while (cbItemSize >= sizeof(DWORD))
                    {
                        pOutCtl->Output("0x%.8x ", *pCurrent++);

                        // Newline every 4 DWORDs                    
                        if (cbLine++ == 3)
                        {
                            pOutCtl->Output("\n\t");
                            cbLine = 0;
                        }

                        cbItemSize -= sizeof(DWORD);                    
                    }

                    // Display last few remaining bytes
                    BYTE *pbCurrent = reinterpret_cast<BYTE*>(pCurrent);
                    while (cbItemSize > 0)
                    {
                        pOutCtl->Output("0x%.2hc ", *pbCurrent++);                    

                        cbItemSize -= sizeof(BYTE);                    
                    }

                    pOutCtl->Output("\n");
                }
            }
        }

        //
        // Find the next the command in batch.
        //

        IFC(cmdReader.GetNextItemSafe(
            &nItemID,
            &pItemData,
            &cbItemSize
            ));
    }

    //
    // S_FALSE means that we reached the end of the stream. Hence we executed
    // the stream correctly and therefore we should return S_OK.
    //

    if (hr == S_FALSE)
    {
        hr = S_OK;
    }

Cleanup:
    return hr;
}

DECLARE_API(cmd)
{
    BEGIN_API( cmd );

    HRESULT hr = S_OK;
    ULONG RemainderIndex;
    DEBUG_VALUE dvAddress;
    DEBUG_VALUE dvSize;
    OutputControl OutCtl(Client);

    PVOID pvBuffer = NULL;

    IDebugDataSpaces *pIData = NULL;
    IDebugSymbols *pISymbols = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&pIData));
    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&pISymbols));

    //
    // skip spaces till the first arg.
    //

    while (*args && isspace(static_cast<unsigned char>(*args))) args++;

    IFC(Evaluate(Client, args, DEBUG_VALUE_INT64, 0, &dvAddress, &RemainderIndex));

    // Advance past address
    args += RemainderIndex;

    //
    // skip spaces before the next arg.
    //

    while (*args && isspace(static_cast<unsigned char>(*args))) args++;

    IFC(Evaluate(Client, args, DEBUG_VALUE_INT64, 0, &dvSize));

    //
    // Retrieve the contents of memory from the address into a buffer.
    //

    IFC(ReadBatchIntoBuffer(dvAddress.I64, &pvBuffer, dvSize.I64, pIData, &OutCtl));

    //   Debugger extensions should not have a table of types and associations
    //  Type information be read dynamically from the symbol files.
    IGNORE_HR(OutCtl.OutWarn("Warning: Command type (and size) is based on wpfx and not symbol information.\n"));
    IGNORE_HR(OutCtl.OutWarn("Warning: Command type (and size) table hardcoded in wpfx changed in Sept 2017, and this information could be incorrect.\n"));
    IGNORE_HR(OutCtl.OutWarn("Warning: Consider building wpfx from older sources (or use an older copy of wpfx.dll) if this information seems wrong"));

    IFC(OutputBatch(Client, pISymbols, &OutCtl, pvBuffer, static_cast<UINT>(dvSize.I64)));

Cleanup:

    if (FAILED(hr))
    {
        OutCtl.Output("Error HRESULT=0x%x\n", hr);
    }

    if (pvBuffer)
    {
        HeapFree(GetProcessHeap(), 0, pvBuffer);
    }

    ReleaseInterface(pIData);
    ReleaseInterface(pISymbols);

    Client->FlushCallbacks();

    return hr;
}

DECLARE_API(lcb)
{
    BEGIN_API( lcb );

    UNREFERENCED_PARAMETER(args);

    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);
    IDebugSymbols *pISymbols = NULL;
    IDebugDataSpaces *pIData = NULL;    
    
    ULONG32 CmdBufferSize = 0;
    ULONG64 TargetCmdBufferPtr = 0;   

    char PointerName[80];
    char SizeName[80];

    PVOID CmdBuffer = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&pISymbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&pIData));

    //
    // Read s_CurrentProcessBatch & s_CurrentProcessBatchSize
    //

    IFC(StringCchPrintfA(ARRAY_COMMA_ELEM_COUNT(PointerName), "%s!s_CurrentProcessBatch", Milcore_Module.Name));
    IFC(StringCchPrintfA(ARRAY_COMMA_ELEM_COUNT(SizeName), "%s!s_CurrentProcessBatchSize", Milcore_Module.Name));

    IGNORE_HR(OutCtl.OutVerb("Looking for current batch and size at \n"
                             "  %s\n"
                             "  %s\n",
                             PointerName,
                             SizeName
                             ));

    //   Debugger extensions should not have a table of types and associations
    //  Type information be read dynamically from the symbol files.
    IGNORE_HR(OutCtl.OutWarn("Warning: command types and sizes are based on wpfx and not symbol information.\n"));
    IGNORE_HR(OutCtl.OutWarn("Warning: Command type (and size) table hardcoded in wpfx changed in Sept 2017, and this information could be incorrect.\n"));
    IGNORE_HR(OutCtl.OutWarn("Warning: Consider building wpfx from older sources (or use an older copy of wpfx.dll) if this information seems wrong"));

    IFC(ReadBatchAddressAndSize(PointerName, SizeName, &TargetCmdBufferPtr, &CmdBufferSize, pISymbols, pIData, &OutCtl));

    if (CmdBufferSize == 0)
    {
        OutCtl.Output("Current batch is empty.\n");
    }
    else
    {        
        //
        // Allocate data for the batch and read it
        //    

        IFC(ReadBatchIntoBuffer(TargetCmdBufferPtr, &CmdBuffer, CmdBufferSize, pIData, &OutCtl));

        //
        // Output the batch
        //

        IFC(OutputBatch(Client, pISymbols, &OutCtl, CmdBuffer, CmdBufferSize));
    }

Cleanup:
    Client->FlushCallbacks();

    ReleaseInterface(pISymbols);
    ReleaseInterface(pIData);    

    if (CmdBuffer != NULL)
    {
        HeapFree(GetProcessHeap(), 0, CmdBuffer);
    }

    return hr;
}

