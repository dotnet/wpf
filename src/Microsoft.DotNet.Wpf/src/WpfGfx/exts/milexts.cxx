// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:        This file contains the generic routines and initialization
//                   code for the debugger extensions dll.
//

#include "precomp.hxx"


//
// globals
//
BOOL                    gbVerbose = FALSE;

//
// Required initialize event callbacks
//

HRESULT
OnExtensionInitialize(
    __inout PDEBUG_CLIENT /*DebugClient*/
    )
{
    return S_OK;
}

void
OnExtensionUninitialize(
    )
{
}

//+----------------------------------------------------------------------------
//
//  Function:
//      BaseModule
//
//  Synopsis:
//      Show/set base (default) module for symbolic information.  Internally
//      base module is known as Type_Module as it is the module to use for
//      unqualified type look ups.
//

DECLARE_API(basemodule)
{
    HRESULT hr = S_OK;

    // No BEGIN_API(basemodule);

    OutputControl OutCtl(Client);

    //
    // Process options
    //

    bool BadSwitch = false;
    bool ShowUsage = false;

    while (!BadSwitch)
    {
        while (isspace(*args)) args++;

        if (*args != '-') break;

        args++;
        BadSwitch = (*args == '\0' || isspace(*args));

        while (*args != '\0' && !isspace(*args))
        {
            switch (*args)
            {
            case '?': ShowUsage = true; break;
            default:
                IGNORE_HR(OutCtl.OutErr("Error: Unknown option at '%s'\n", args));
                BadSwitch = true;
                break;
            }

            if (BadSwitch) break;
            args++;
        }
    }

    if (BadSwitch || ShowUsage)
    {
        IGNORE_HR(OutCtl.Output(
            "Usage:  !basemodule [-?] [module name]\n"
            "\n"
            "    Shows/sets the default module to look up data from.\n"
            "\n"
            "Example: !basemodule dwm.exe\n"
            ));
    }
    else
    {
        IGNORE_HR(OutCtl.Output("Current base module is %s.\n",
                                Type_Module.Base ?
                                Type_Module.Name :
                                "NOT INTIALIZED"));

        //
        // If there is any remaining argument, it is assumed to be a module
        //

        if (*args)
        {
            ModuleParameters NewBaseModule = { 0, DEBUG_ANY_ID, "", "" };

            //
            // Look for and setup potential file extension
            //

            const char* pszExt = strrchr(args, '.');

            if (pszExt)
            {
                size_t ExtLength = strlen(pszExt)-1;    // minus 1 for .
                if (ExtLength > 0 && ExtLength < ARRAYSIZE(NewBaseModule.Ext))
                {
                    pszExt = 0;
                    pszExt++;
                    IGNORE_HR(StringCchCopyA(ARRAY_COMMA_ELEM_COUNT(NewBaseModule.Ext), pszExt));
                }
                else
                {
                    pszExt = NULL;
                }
            }

            //
            // Copy module name in place
            //

            hr = StringCchCopyA(ARRAY_COMMA_ELEM_COUNT(NewBaseModule.Name), args);

            if (FAILED(hr))
            {
                IGNORE_HR(OutCtl.OutErr("Failed to setup module name - error 0x%X.\n", hr));
            }
            else
            {
                //
                // Try to load basic module information
                //

                hr = GetModuleParameters(Client, &NewBaseModule, pszExt != NULL);

                // Maybe ext was not really an extension - restore and try again
                if (   hr != S_OK
                    && pszExt != NULL)
                {
                    if (   SUCCEEDED(StringCchCatA(ARRAY_COMMA_ELEM_COUNT(NewBaseModule.Name), "."))
                        && SUCCEEDED(StringCchCatA(ARRAY_COMMA_ELEM_COUNT(NewBaseModule.Name), NewBaseModule.Ext)))
                    {
                        NewBaseModule.Ext[0] = 0;
                        hr = GetModuleParameters(Client, &NewBaseModule, FALSE);
                    }
                }

                if (hr == S_OK)
                {
                    //
                    // Set new base module
                    //

                    Type_Module = NewBaseModule;

                    IGNORE_HR(OutCtl.Output("New base module is %s.\n",
                                            NewBaseModule.Name));
                }
                else
                {
                    IGNORE_HR(OutCtl.OutErr("Failed to get module info - error 0x%X.\n", hr));
                }
            }
        }
    }

    return hr;
}


//+----------------------------------------------------------------------------
//
//  Extension:
//      verbose
//
//  Synopsis:
//      Toggle extension output verbosity level
//
//-----------------------------------------------------------------------------

DECLARE_API(verbose)
{
    UNREFERENCED_PARAMETER(args);

    gbVerbose = !gbVerbose;

    OutputControl OutCtl(Client);
    OutCtl.Output("TARGETNAME_STR verbose mode is now %s.\n", gbVerbose ? "ON" : "OFF");

    return S_OK;
}


//+----------------------------------------------------------------------------
//
//  Extension:
//      annot
//
//  Synopsis:
//      List symbol annotations near given address or first N in base module
//

#define _NO_CVCONST_H
#include <dbghelp.h>

DECLARE_API(annot)
{
    HRESULT hr = S_OK;

    BEGIN_API(annot);

    OutputControl OutCtl(Client);

    IDebugAdvanced3 *pIDbgAdv3 = NULL;

    // Obtain debug library interfaces for looking up symbols, etc.    
    IFC(Client->QueryInterface(__uuidof(IDebugAdvanced3), (void **)&pIDbgAdv3));

    ULONG64 Addr = 0;
    ULONG64 Offset[32];
    ULONG NumOffs;
    WCHAR Str[1024];
    ULONG StrChars;

    while (isspace(*args)) args++;

    if (*args)
    {
        DEBUG_VALUE Argument;
        if (SUCCEEDED(OutCtl.Evaluate(args, DEBUG_VALUE_INT64, &Argument, NULL)))
        {
            Addr = Argument.I64;
        }
    }

    if (Addr == 0)
    {
        Addr = Type_Module.Base;
        IFC(pIDbgAdv3->GetSymbolInformationWide(
            DEBUG_SYMINFO_GET_MODULE_SYMBOL_NAMES_AND_OFFSETS,
            Addr, SymTagAnnotation,
            Offset, sizeof(Offset), &NumOffs,
            Str, ARRAYSIZE(Str), &StrChars));
    }
    else
    {
        IFC(pIDbgAdv3->GetSymbolInformationWide(
            DEBUG_SYMINFO_GET_SYMBOL_NAME_BY_OFFSET_AND_TAG_WIDE,
            Addr, SymTagAnnotation,
            Str, sizeof(Str), &StrChars,
            NULL, 0, NULL));
        StrChars /= sizeof(Str[0]);
        NumOffs = sizeof(ULONG64);
        Offset[0] = Addr;
    }

    ULONG i;
    PWSTR Scan;

    NumOffs /= sizeof(ULONG64);
    OutCtl.Output(
        "Annotations: %u, %u chars\n",
        NumOffs, StrChars);

    if (NumOffs > ARRAYSIZE(Offset))
    {
        NumOffs = ARRAYSIZE(Offset);
        OutCtl.OutWarn(" Only showing first %u annotations\n", NumOffs);
    }

    if (StrChars > ARRAYSIZE(Str))
    {
        StrChars = ARRAYSIZE(Str);
        OutCtl.OutWarn(" Only showing first %u characters of annotation text\n",
                       StrChars);
    }

    Scan = Str;
    for (i = 0; i < NumOffs; i++)
    {
        OutCtl.Output("%02u: %p -", i, Offset[i]);
        while (*Scan && (ULONG)(Scan - Str) < StrChars)
        {
            OutCtl.Output(" \"%ws\"", Scan);
            Scan += wcslen(Scan) + 1;
        }
        OutCtl.Output("\n");
        Scan++;
    }

Cleanup:
    RRETURN(hr);
}



