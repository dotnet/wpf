// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//---------------------------------------------------------------------------------
//

//
// File: hwndlookup.cxx
//---------------------------------------------------------------------------------

#include "precomp.hxx"

/**************************************************************************\
* LookpuCMilWindowContext
*
* Resolve an hwnd to its CMilWindowContext representation.
*
* Wrote it.
\**************************************************************************/
HRESULT LookupCMilWindowContext(
    PDEBUG_CLIENT Client,
    ULONG64 hwnd,
    __out ULONG64* pulpCMilWindowContext)
{
    HRESULT hr = S_OK;
    OutputControl OutCtl(Client);

    DEBUG_VALUE dv_gwindowManager;
    ULONG offset_tblLookup;
    ULONG offset_hwnd;
    ULONG64 ulpHWND_WINDOW_CONTEXT_MAP_ENTRY;

    if (FAILED(hr = OutCtl.Evaluate("dwmredir!g_windowManager", DEBUG_VALUE_INT64, &dv_gwindowManager, NULL)))
    {
        OutCtl.Output("Couldn't get dwmredir!g_windowManager: %p\n", hr);
        goto Cleanup;
    }

    if (FAILED(hr = GetFieldOffset(Client, "dwmredir!CMilWindowManager", "m_tblWindowLookup", &offset_tblLookup))) goto Cleanup;
    if (FAILED(hr = GetFieldOffset(Client, "dwmredir!CMilWindowManager::HWND_WINDOW_CONTEXT_MAP_ENTRY", "hwnd", &offset_hwnd))) goto Cleanup;

    if (FAILED(hr = SearchTable(Client, 
                                dv_gwindowManager.I64 + offset_tblLookup,
                                offset_hwnd, 
                                hwnd,
                                &ulpHWND_WINDOW_CONTEXT_MAP_ENTRY)))
    {
        goto Cleanup;
    }

    if (ulpHWND_WINDOW_CONTEXT_MAP_ENTRY == 0)
    {
        OutCtl.Output("Couldn't find CMilWindowContext matching hwnd: %p\n", hwnd);
        hr = E_FAIL;
        goto Cleanup;
    }

    if (FAILED(hr = ReadPointerField(Client, ulpHWND_WINDOW_CONTEXT_MAP_ENTRY, "dwmredir!CMilWindowManager::HWND_WINDOW_CONTEXT_MAP_ENTRY", "pwnd", pulpCMilWindowContext))) goto Cleanup;

Cleanup:
    return hr;
}

/******************************Public*Routine******************************\
* hwnd
*
* Searches DWM internals for a particular hwnd representation.
*
* Wrote it.
\**************************************************************************/

CPPMOD HRESULT CALLBACK hwnd(PDEBUG_CLIENT Client, PCSTR args)
{    
    HRESULT hr = S_OK;
    OutputControl   OutCtl(Client);

    CommandLine* pCommandLine = NULL;
    bool fShowHelp = false;
    PDEBUG_SYMBOLS Symbols = NULL;
    PDEBUG_DATA_SPACES Data = NULL;

    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugSymbols), (void **)&Symbols))) goto Cleanup;    
    if (FAILED(hr = Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data))) goto Cleanup;
    if (FAILED(hr = CommandLine::CreateFromString(OutCtl, args, &pCommandLine))) goto Cleanup;

    if (pCommandLine->GetCount() == 0 || (pCommandLine->GetCount() == 1 && (*pCommandLine)[0].fIsOption))
    {
        fShowHelp = true;
    }
    else
    {
        CommandLine& commandLine = *pCommandLine;

        bool fVerbose = false;
        bool fCMilWindowContext = false;
        bool fCWindowData = false;
        bool fCTopLevelWindow = false;
        bool fDceVisual = false;

        DEBUG_VALUE dvHwnd = { 0 };

        for (UINT i = 0; i < commandLine.GetCount(); i++)
        {
            if (commandLine[i].fIsOption)
            {
                for (UINT j = 0; j < commandLine[i].cchLength; j++)
                {
                    switch (commandLine[i].string[j])
                    {
                        case 'm': fCMilWindowContext = true; break;
                        case 'w': fCWindowData = true; break;
                        case 't': fCTopLevelWindow = true; break;
                        case 'd': fDceVisual = true; break;
                        case 'v': fVerbose = true; break;
                        default: 
                            OutCtl.Output("Unknown option %s\n", commandLine[i].string);
                            fShowHelp = true; goto Cleanup;
                    }
                }
            }
            else
            {
                hr = OutCtl.Evaluate(commandLine[i].string, DEBUG_VALUE_INT64, &dvHwnd, NULL);

                if (FAILED(hr))
                {
                    OutCtl.Output("Could not evaluate argument: %s\n", commandLine[i].string);
                    goto Cleanup;
                }
            }
        }

        if (dvHwnd.I64 == 0)
        {
            fShowHelp = true;
            OutCtl.Output("hwnd not provided\n");
        }
        else
        {
            ULONG64 ulpCMilWindowContext = { 0 };
            ULONG64 ulpCWindowData = { 0 };
            ULONG64 ulpCTopLevelWindow = { 0 };
            //ULONG64 ulpDceVisual = { 0 };

            if (fCMilWindowContext || fCWindowData || fCTopLevelWindow)
            {
                if (FAILED(hr = LookupCMilWindowContext(Client, dvHwnd.I64, &ulpCMilWindowContext))) goto Cleanup;

                if (fCMilWindowContext)
                {
                    OutputInstance(Client, "dwmredir!CMilWindowContext", ulpCMilWindowContext, fVerbose);
                }
            }

            if (fCWindowData || fCTopLevelWindow)
            {
                if (FAILED(hr = ReadPointerField(Client, ulpCMilWindowContext, "dwmredir!CMilWindowContext", "m_pvClientData", &ulpCWindowData))) goto Cleanup;

                if (fCWindowData)
                {
                    OutputInstance(Client, "udwm!CWindowData", ulpCWindowData, fVerbose);
                }
            }

            if (fCTopLevelWindow)
            {
                if (FAILED(hr = ReadPointerField(Client, ulpCWindowData, "udwm!CWindowData", "pWindow", &ulpCTopLevelWindow))) goto Cleanup;

                OutputInstance(Client, "udwm!CTopLevelWindow", ulpCTopLevelWindow, fVerbose);
            }
        }
    }

Cleanup:
    ReleaseInterface(Data);
    ReleaseInterface(Symbols);
    if (fShowHelp)
    {
        OutCtl.Output(
            "\n!hwnd [options] <hwnd>\n"
            "   -m  CMilWindowContext\n"
            "   -w  CWindowData\n"
            "   -t  CTopLevelWindow\n"
            "   -d  DceVisual\n"
            "   -v  verbose\n"
            );
    }
    
    delete pCommandLine;
        
    return hr;
}




