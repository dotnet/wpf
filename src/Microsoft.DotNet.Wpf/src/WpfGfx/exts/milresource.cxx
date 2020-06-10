// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//---------------------------------------------------------------------------------
//

//
// File: dumptable.cxx
//---------------------------------------------------------------------------------

#include "precomp.hxx"

#define DBG_RESOURCE 0

//+-----------------------------------------------------------------------------
//
//    Function:
//        GetClientChannelHandle
// 
//    Synopsis:
//        Given a MIL channel pointer, returns its MIL handle (HMIL_CHANNEL).
// 
//------------------------------------------------------------------------------

HRESULT
GetClientChannelHandle(
    PDEBUG_CLIENT Client, 
    ULONG64 ulpMilChannel, 
    __out ULONG64* pulhMilChannel
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);

    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));

    ULONG ul_m_hChannel = 0;

    IFC(ReadTypedField(Client, ulpMilChannel, "milcore!CMilChannel", "m_hChannel", &ul_m_hChannel));

    *pulhMilChannel = ul_m_hChannel;

#if DBG_RESOURCE
    OutCtl.Output("GetClientChannelHandle: %p\n", ul_m_hChannel);
#endif

Cleanup:
    return hr;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        IsTransportGroup
// 
//    Synopsis:
//        Checks if the given IMilCommandTransport is a CMILTransportGroup...
// 
//------------------------------------------------------------------------------

bool
IsTransportGroup(
    PDEBUG_CLIENT client, 
    ULONG64 ulpTransport
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(client);

    char name[MAX_PATH];
    ULONG nameSize = 0;
    bool result = false;

    IFC(ReadSymbolNameByOffset(
        client,
        ulpTransport,
        ARRAY_SIZE(name),
        name,
        &nameSize
        ));

    result = (strcmp(name, "milcore!CMILTransportGroup::`vftable'") == 0);

Cleanup:
    return result;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        IsCrossThreadTransport
// 
//    Synopsis:
//        Checks if the given IMilCommandTransport is a CMilCrossThreadTransport...
// 
//------------------------------------------------------------------------------

bool
IsCrossThreadTransport(
    PDEBUG_CLIENT client, 
    ULONG64 ulpTransport
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(client);

    char name[MAX_PATH];
    ULONG nameSize = 0;
    bool result = false;

    IFC(ReadSymbolNameByOffset(
        client,
        ulpTransport,
        ARRAY_SIZE(name),
        name,
        &nameSize
        ));

    result = (strcmp(name, "milcore!CMilCrossThreadTransport::`vftable'") == 0);

Cleanup:
    return result;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        GetConnectionContextPointer
// 
//    Synopsis:
//        Given a MIL client channel pointer, returns its corresponding
//        MIL connection context object pointer.
// 
//------------------------------------------------------------------------------

HRESULT
GetConnectionContextPointer(
    PDEBUG_CLIENT Client, 
    ULONG64 ulpMilChannel, 
    __out ULONG64* pulpConnectionContext
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);

    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));

    ULONG64 ulpClientConnection = 0;
    ULONG64 ulpClientTransport = 0;

    IFC(ReadPointerField(Client, ulpMilChannel, "milcore!CMilChannel", "m_pConnection", &ulpClientConnection));
    IFC(ReadPointerField(Client, ulpClientConnection, "milcore!CMilConnection", "m_pCmdTransport", &ulpClientTransport));

    //
    // Check the transport type: for the purpose of this extension, we only
    // support the cross-thread transport and group transport with a cross-
    // -thread transport being the primary transport.
    //

    if (IsTransportGroup(Client, ulpClientTransport)) 
    {
        ULONG64 ulpPrimaryTransport = 0;

        IFC(ReadPointerField(Client, ulpClientTransport, "milcore!CMILTransportGroup", "m_pPrimaryTransport", &ulpPrimaryTransport));

        ulpClientTransport = ulpPrimaryTransport;
    }

    if (!IsCrossThreadTransport(Client, ulpClientTransport)) 
    {
        OutCtl.Output("GetConnectionContextPointer: %p is not a cross-thread transport...\n", ulpClientTransport);
        IFC(E_FAIL);
    }

    IFC(ReadPointerField(Client, ulpClientTransport, "milcore!CMilCrossThreadTransport", "m_pConnectionContext", pulpConnectionContext));

#if DBG_RESOURCE
    OutCtl.Output("GetConnectionContextPointer: %p\n", *pulpConnectionContext);
#endif

Cleanup:
    return hr;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        GetMILHandleTableEntry
// 
//    Synopsis:
//        Given a MIL HANDLE_TABLE pointer and a MIL handle, returns 
//        the corresponding handle table entry and its size
// 
//------------------------------------------------------------------------------

HRESULT
GetMILHandleTableEntry(
    PDEBUG_CLIENT Client,
    ULONG64 ulpHandleTable,
    ULONG64 ulhEntry,
    __out ULONG64 *pulpEntry
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);

    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));

    ULONG ul_m_cbEntry = 0;
    ULONG64 ul_m_pvTable = 0;

    IFC(ReadTypedField(Client, ulpHandleTable, "milcore!HANDLE_TABLE", "m_cbEntry", &ul_m_cbEntry));
    IFC(ReadPointerField(Client, ulpHandleTable, "milcore!HANDLE_TABLE", "m_pvTable", &ul_m_pvTable));

#if DBG_RESOURCE
    OutCtl.Output("GetMILHandleTableEntry: ul_m_cbEntry: %p\n", ul_m_cbEntry);
    OutCtl.Output("GetMILHandleTableEntry: ul_m_pvTable: %p\n", ul_m_pvTable);
#endif

    *pulpEntry = ul_m_pvTable + ulhEntry * ul_m_cbEntry;

#if DBG_RESOURCE
    OutCtl.Output("GetMILHandleTableEntry: %p\n", *pulpEntry);
#endif

Cleanup:
    return hr;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        GetServerChannelPointer
// 
//    Synopsis:
//        Given a MIL connection context pointer and a MIL channel handle, 
//        returns a MIL server channel pointer.
// 
//------------------------------------------------------------------------------

HRESULT
GetServerChannelPointer(
    PDEBUG_CLIENT Client,
    ULONG64 ulpConnectionContext,
    ULONG64 ulhMilChannel,
    __out ULONG64 *pulpServerChannel
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);

    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));

    ULONG offset_m_channelTable = 0;

    IFC(GetFieldOffset(Client, "milcore!CConnectionContext", "m_channelTable", &offset_m_channelTable));

    ULONG64 ulpServerChannelHandleEntry = 0;

    IFC(GetMILHandleTableEntry(Client, ulpConnectionContext + offset_m_channelTable, ulhMilChannel, &ulpServerChannelHandleEntry));

    IFC(ReadPointerField(Client, ulpServerChannelHandleEntry, "milcore!SERVER_CHANNEL_HANDLE_ENTRY", "pServerChannel", pulpServerChannel));

#if DBG_RESOURCE
    OutCtl.Output("GetServerChannelPointer: %p\n", *pulpServerChannel);
#endif

Cleanup:
    return hr;
}


//+-----------------------------------------------------------------------------
//
//    Function:
//        GetServerHandleTablePointer
// 
//    Synopsis:
//        Inspects a CMilServerChannel object, follows its m_pServerTable
//        CMilSlaveHandleTable pointer and returns a pointer to its
//        m_handletable (of HANDLE_TABLE type).
// 
//------------------------------------------------------------------------------

HRESULT
GetServerHandleTablePointer(
    PDEBUG_CLIENT Client,
    ULONG64 ulpServerChannel,
    __out ULONG64 *pulpServerChannelHandleTable
    )
{
    HRESULT hr = S_OK;

    OutputControl OutCtl(Client);

    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));

    ULONG64 ulpSlaveHandleTable = 0;
    ULONG offset_m_handletable = 0;

    IFC(ReadPointerField(Client, ulpServerChannel, "milcore!CMilServerChannel", "m_pServerTable", &ulpSlaveHandleTable));

#if DBG_RESOURCE
    OutCtl.Output("GetServerHandleTablePointer: ulpSlaveHandleTable: %p\n", ulpSlaveHandleTable);
#endif

    IFC(GetFieldOffset(Client, "milcore!CMilSlaveHandleTable", "m_handletable", &offset_m_handletable));

#if DBG_RESOURCE
    OutCtl.Output("GetServerHandleTablePointer: offset_m_handletable: %p\n", offset_m_handletable);
#endif

    *pulpServerChannelHandleTable = ulpSlaveHandleTable + offset_m_handletable;

#if DBG_RESOURCE
    OutCtl.Output("GetServerHandleTablePointer: *pulpServerChannelHandleTable: %p\n", *pulpServerChannelHandleTable);
#endif

Cleanup:
    return hr;
}


/**************************************************************************\
* ResolveHMilResource
*
* Look up an HMIL_RESOURCE and resolve it to a HANDLE_ENTRY on the slave side
* through a MIL_CHANNEL (defaults to dwmredir!g_windowManager.m_pResourceChannel).
* HANDLE_ENTRY contains the resource type and CMilSlaveResource*.
*
* Wrote it.
\**************************************************************************/
HRESULT ResolveHMilResource(
    PDEBUG_CLIENT Client, 
    ULONG64 ulhResource, 
    ULONG64 ulpMilChannel, 
    __out ULONG64* pulpHANDLE_ENTRY)
{
    HRESULT hr = S_OK;
    OutputControl OutCtl(Client);
    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));
    
    if (ulpMilChannel == 0)
    {
        DEBUG_VALUE dv_gwindowManager;
        if (FAILED(hr = OutCtl.Evaluate("dwmredir!g_windowManager", DEBUG_VALUE_INT64, &dv_gwindowManager, NULL)))
        {
            OutCtl.Output("Couldn't get dwmredir!g_windowManager: %p\n", hr);
            goto Cleanup;
        }

        IFC(ReadPointerField(Client, dv_gwindowManager.I64, "dwmredir!CMilWindowManager", "m_pWmChannel", &ulpMilChannel));
    }

    ULONG64 ulhMilChannel;
    ULONG64 ulpConnectionContext = 0;
    ULONG64 ulpServerChannel = 0;
    ULONG64 ulpServerHandleTable = 0;

#if DBG_RESOURCE
    OutCtl.Output("ResolveHMilResource: ulpMilChannel: %p\n", ulpMilChannel);
#endif

    IFC(GetClientChannelHandle(Client, ulpMilChannel, &ulhMilChannel));
    IFC(GetConnectionContextPointer(Client, ulpMilChannel, &ulpConnectionContext));
    IFC(GetServerChannelPointer(Client, ulpConnectionContext, ulhMilChannel, &ulpServerChannel));
    IFC(GetServerHandleTablePointer(Client, ulpServerChannel, &ulpServerHandleTable));

#if DBG_RESOURCE
    OutCtl.Output("ResolveHMilResource: ulpServerHandleTable: %p\n", ulpServerHandleTable);
#endif

    IFC(GetMILHandleTableEntry(Client, ulpServerHandleTable, ulhResource, pulpHANDLE_ENTRY));

Cleanup:
    return hr;
}


/******************************Public*Routine******************************\
* resource
*
* Looks up a MIL resource handle and retrieves the CMilSlaveResource 
* corresponding to it.
*
* Wrote it.
\**************************************************************************/
CPPMOD HRESULT CALLBACK resource(PDEBUG_CLIENT Client, PCSTR args)
{    
    HRESULT hr = S_OK;
    OutputControl   OutCtl(Client);

    CommandLine* pCommandLine = NULL;
    bool fShowHelp = false;
    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    IFC(Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols));
    IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data));
    IFC(CommandLine::CreateFromString(OutCtl, args, &pCommandLine));

    if (pCommandLine->GetCount() == 0 || (pCommandLine->GetCount() == 1 && (*pCommandLine)[0].fIsOption))
    {
        fShowHelp = true;
    }
    else
    {
        CommandLine& commandLine = *pCommandLine;

        DEBUG_VALUE dvhMilResource = { 0 };
        DEBUG_VALUE dvhMilChannel = { 0 };

        for (UINT i = 0; i < commandLine.GetCount(); i++)
        {
            if (!commandLine[i].fIsOption)
            {
                DEBUG_VALUE dvhParam = { 0 };

                if (FAILED(hr = OutCtl.Evaluate(commandLine[i].string, DEBUG_VALUE_INT64, &dvhParam, NULL))) 
                {
                    OutCtl.Output("Could not evaluate argument %s\n", commandLine[i].string);
                    goto Cleanup;
                }

                if (dvhMilResource.I64 == 0) 
                {
                    dvhMilResource = dvhParam;
                }
                else if (dvhMilChannel.I64 == 0) 
                {
                    dvhMilChannel = dvhParam;
                }
                else
                {
                    OutCtl.Output("Unexpected command line argument %s\n", commandLine[i].string);
                    goto Cleanup;
                }
            }
        }

        if (dvhMilResource.I64 == 0)
        {
            fShowHelp = true;
            goto Cleanup;
        }

        ULONG64 ulpHANDLE_ENTRY;
        IFC(ResolveHMilResource(Client, dvhMilResource.I64, dvhMilChannel.I64, &ulpHANDLE_ENTRY));

        OutputInstance(Client, "milcore!CMilSlaveHandleTable::HANDLE_ENTRY", ulpHANDLE_ENTRY, true);
    }

Cleanup:
    ReleaseInterface(Data);
    ReleaseInterface(Symbols);
    if (fShowHelp)
    {
        OutCtl.Output(
            "\n!resource <hmil_resource> [<mil_channel>]\n"
            );
    }

    if (pCommandLine)
    {
        delete pCommandLine;
    }

    return hr;
}




