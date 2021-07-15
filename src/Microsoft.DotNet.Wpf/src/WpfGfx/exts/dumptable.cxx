// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//---------------------------------------------------------------------------------
//

//
// File: dumptable.cxx
//---------------------------------------------------------------------------------

#include "precomp.hxx"

/******************************Public*Routine******************************\
* dumptable
*
* Dumps the entries in an RTL table, dumping the contents of the entries
* if a type is provided as well.
*

\**************************************************************************/

CPPMOD HRESULT CALLBACK dumptable(PDEBUG_CLIENT Client, PCSTR args)
{    
    HRESULT hr = S_OK;
    OutputControl   OutCtl(Client);

    CommandLine* pCommandLine = NULL;
    DEBUG_VALUE address;
    const int MAX_ELEMENTS = 5000;
    int numElements = 0;

    if (FAILED(hr = CommandLine::CreateFromString(OutCtl, args, &pCommandLine)))
    {
        return hr;
    }

    if (pCommandLine->GetCount() == 0 || (pCommandLine->GetCount() == 1 && (*pCommandLine)[0].fIsOption))
    {
        OutCtl.Output(
            "!dumptable address type [fields]\n"
            );
    }
    else
    {
#if NEVER
        for (UINT i = 0; i < pCommandLine->GetCount(); i++)
        {
            OutCtl.Output(
                "Argument %d: *%s* (%s) length = %d\n", 
                i, 
                (*pCommandLine)[i].string, 
                (((*pCommandLine)[i].fIsOption) ? ("option") : ("NOT option")), 
                (*pCommandLine)[i].cchLength
                );
        }
#endif
        UINT uArg = 0;

        bool fVerbose = false;

        if ((*pCommandLine)[uArg].fIsOption && tolower((*pCommandLine)[uArg].string[0]) == 'v')
        {
            fVerbose = true;
            uArg++;
        }

        hr = OutCtl.Evaluate((*pCommandLine)[uArg++].string, DEBUG_VALUE_INT64, &address, NULL);

        if (FAILED(hr))
        {
            OutCtl.Output("Could not evaluate argument: %s\n", (*pCommandLine)[0].string);
        }
        else
        {
            PDEBUG_DATA_SPACES Data = NULL;

            hr = Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&Data);

            if (SUCCEEDED(hr))
            {
                ULONG64 listHead;
                ULONG64 listCurrent;
                ULONG uSizePVOID = (OutCtl.IsPointer64Bit() == S_OK) ? 8 : 4;
                ULONG uClientDataOffset = (OutCtl.IsPointer64Bit() == S_OK) ? 16 : 12;
                char szDTCommandTemplate[10000] = "dt";

                bool fCallDT = false;

                while (uArg < pCommandLine->GetCount())
                {
                    StringCchCatA(szDTCommandTemplate, ARRAY_SIZE(szDTCommandTemplate), " ");
                    StringCchCatA(szDTCommandTemplate, ARRAY_SIZE(szDTCommandTemplate), (*pCommandLine)[uArg].string);

                    uArg++;

                    // Okay, there were some params so we can call dt
                    fCallDT = true;
                }

                listHead = address.I64 + uSizePVOID;

                Data->ReadPointersVirtual(1, listHead, &listCurrent);

                OutCtl.Output("ListHead = %p\n", listHead);

                hr = Data->ReadPointersVirtual(1, listHead, &listCurrent);
                while (listCurrent != listHead && SUCCEEDED(hr) && ++numElements < MAX_ELEMENTS)
                {
                    if (!fCallDT)
                    {
                        OutCtl.Output("Entry at %p\n", listCurrent);
                    }
                    else
                    {
                        char szDTCommand[ARRAY_SIZE(szDTCommandTemplate)];

                        StringCchPrintfA(szDTCommand, ARRAY_SIZE(szDTCommand), "%s %p", szDTCommandTemplate, listCurrent + uClientDataOffset);

                        OutCtl.Output("%s\n", szDTCommand);

                        hr = OutCtl.Execute(szDTCommand, 0);

                        if (hr != S_OK)
                        {
                            OutCtl.Output("\ndt failed: hr = %p\n", hr);
                            break;
                        }
                    }

                    hr = Data->ReadPointersVirtual(1, listCurrent, &listCurrent);

                    if (OutCtl.GetInterrupt() == S_OK)
                    {
                        OutCtl.Output("\n\nStop on user-interrupt...\n\n");
                        hr = E_ABORT;
                        break;
                    }
                }

                Data->Release();
            }

            if (SUCCEEDED(hr))
            {
                OutCtl.Output("Total elements = %d\n", numElements);
            }
            
            if (numElements >= MAX_ELEMENTS)
            {
                OutCtl.Output("\n\nReached max number of elements, stopping.\n\n");
            }
        }
    }

    delete pCommandLine;
        
    return hr;
}



