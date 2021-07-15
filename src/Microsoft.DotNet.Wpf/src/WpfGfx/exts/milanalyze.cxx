// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+-----------------------------------------------------------------------------
//

//
//  Abstract:  Implementation of the MILAnalyze debug extension.
//
//------------------------------------------------------------------------------

#include "precomp.hxx"

#include "DbgHelp.h"    // Needed for SYMOPT_LOAD_LINES definition

const UINT MAX_STACK_FRAMES = 50;

//+----------------------------------------------------------------------------
//
//  Enumeration:  
//      MILEventType
//
//  Synopsis:
//      Event types as triaged by ClassifyMILEventType.
//
//-----------------------------------------------------------------------------

namespace MILEventType
{
    enum Enum
    {
        // Not classified
        Unclassified = 0,

        // MilUnexpectedError on stack
        UnexpectedError,

        // MilInstrumentationBreak on stack
        InstrumentationBreak,
    };
}


//+----------------------------------------------------------------------------
//
//  Function:  ClassifyMILEventType
//
//  Synopsis:  Inspects the call stack and determines the event type.
//
//-----------------------------------------------------------------------------

HRESULT ClassifyMILEventType(
    __in_ecount(1) OutputControl* pOutCtl, 
    __in_ecount(1) IDebugSymbols3* pISymbols,
    ULONG64 u64Offset,
    __out_ecount(ceModuleName) LPSTR szModuleName,
    size_t ceModuleName,
    __out_ecount(1) MILEventType::Enum* peResult
    )
{
    MILX_TRACE_ENTRY;

    HRESULT hr = S_OK;
    
    MILEventType::Enum eResult = MILEventType::Unclassified;
    char szSymbolName[256], szFunctionName[256];

    IFC(GetNameByOffset(
        pISymbols,
        u64Offset,
        sizeof(szSymbolName),
        szSymbolName,
        NULL,
        pOutCtl
        ));

    // Break the symbol name into module - function name pair
    {
        char* szDelimiterPosition = strchr(szSymbolName, '!');

        if (szDelimiterPosition == NULL)
        {
            // how to get module name for '03f6e268 540f9616 CLRStub[StubLinkStub]@d0a81b' etc.?
            IFC(StringCchCopyA(
                    szModuleName,
                    ceModuleName,
                    "unknown_module"
                    ));

            IFC(StringCchCopyA(
                    ARRAY_COMMA_ELEM_COUNT(szFunctionName),
                    szSymbolName
                    ));
        }
        else
        {
            IFC(StringCchCopyA(
                    ARRAY_COMMA_ELEM_COUNT(szFunctionName),
                    szDelimiterPosition + 1
                    ));

            *szDelimiterPosition = 0;

            IFC(StringCchCopyA(
                    szModuleName,
                    ceModuleName,
                    szSymbolName
                    ));
        }
    }
    
    if (_stricmp(szFunctionName, "MilUnexpectedError") == 0)
    {
        //
        // MilUnexpectedError on stack.
        //

        eResult = MILEventType::UnexpectedError;
    }
    else if (_stricmp(szFunctionName, "MilInstrumentationBreak") == 0)
    {
        //
        // MilInstrumentationBreak on stack.
        //
        
        eResult = MILEventType::InstrumentationBreak;
    }

    *peResult = eResult;

Cleanup:
    RRETURN(hr);
}


//+----------------------------------------------------------------------------
//
//  Function:  MILAnalyzeImpl
//
//  Synopsis:  Dumps the current call stack, collating it to the last
//             stack capture if necessary.
//
//-----------------------------------------------------------------------------

HRESULT MILAnalyzeImpl(
    __in_ecount(1) OutputControl* pOutCtl,
    __in_ecount(1) IDebugDataSpaces* pIData,
    __in_ecount(1) IDebugSymbols3* pISymbols,
    __in_ecount(1) IDebugControl* pIControl,
    __in_ecount(1) IDebugSystemObjects4* pISystemObjects
    )
{
    MILX_TRACE_ENTRY;

    HRESULT hr = S_OK;

    DEBUG_STACK_FRAME StackFrames[MAX_STACK_FRAMES];
    char szModuleName[256], szCurrentFunctionName[256];
    ULONG uFrames = 0;

    ULONG uFirstInterestingFrame = 0;

    bool fHitMilUnexpectedError = false;
    bool fPrintFollowup = false;

    DEBUG_VALUE ThreadIdFilter;
    ThreadIdFilter.I32 = 0;  // Filter based on last capture's thread
    ThreadIdFilter.Type = DEBUG_VALUE_INT32;

    DEBUG_VALUE HRESULTFilter;
    HRESULTFilter.Type = DEBUG_VALUE_INVALID;

    // Check for the .lines setting
    // Future Consideration:   Check into filtering OutputStackTrace to avoid .line check
    ULONG uSymbolOptions = SYMOPT_LOAD_LINES;
    IGNORE_HR(pISymbols->GetSymbolOptions(&uSymbolOptions));

    DWORD StackOutputFlags = DEBUG_STACK_FRAME_ADDRESSES;

    if (uSymbolOptions & SYMOPT_LOAD_LINES)
    {
        StackOutputFlags |= DEBUG_STACK_SOURCE_LINE;
    }

    // NOTICE-2005/10/03-JasonHa  Don't reset scope because that would loose .cxr etc.
    // Go to the last event's scope
    //IFC(pISymbols->ResetScope());

    // Get the call stack
    IFC(pIControl->GetStackTrace(
            /* FrameOffset */ 0, 
            /* StackOffset */ 0, 
            /* InstructionOffset */ 0, 
            ARRAY_COMMA_ELEM_COUNT(StackFrames),
            &uFrames
            ));

    for (ULONG i = 0; i < uFrames; i++)
    {
        MILEventType::Enum eMILEventType;

        IFC(ClassifyMILEventType(
                pOutCtl,
                pISymbols,
                StackFrames[i].InstructionOffset,
                ARRAY_COMMA_ELEM_COUNT(szModuleName),
                &eMILEventType
                ));

        if (eMILEventType == MILEventType::UnexpectedError)
        {
            //
            // Grab the name of the function where MilUnexpectedError occured.
            //

            assert(i + 1 < uFrames);

            IFC(GetNameByOffset(
                    pISymbols,
                    StackFrames[i + 1].InstructionOffset,
                    sizeof(szCurrentFunctionName),
                    szCurrentFunctionName,
                    NULL,
                    pOutCtl
                    ));

            // First parameter to MilUnexpectedError should be HRESULT
            // triggering the call.
            HRESULT FirstParamAsHRESULT = static_cast<HRESULT>(StackFrames[i].Params[0]);
            // If first parameter looks like a failure code then filter stack
            // captures fo it.
            if (FAILED(FirstParamAsHRESULT))
            {
                HRESULTFilter.I32 = FirstParamAsHRESULT;
                HRESULTFilter.Type = DEBUG_VALUE_INT32;
            }

            //
            // Dump the last capture followed by the rest of the call stack.
            //

            uFirstInterestingFrame = i + 1;
            fHitMilUnexpectedError = true;
            fPrintFollowup = true;
            break;
        }
        else if (eMILEventType == MILEventType::InstrumentationBreak)
        {
            //
            // Dump the whole call stack. Do not dump the last capture.
            // 

            uFirstInterestingFrame = 0;
            fHitMilUnexpectedError = false;
            break;
        }
    }

    // Dump the last capture, if requested. Try to match the call stack.
    if (fHitMilUnexpectedError)
    {
        StackCaptureFrame LastCapturedFrame;

        IFC(DumpCaptureImpl(
                pOutCtl,
                pIData,
                pISymbols,
                pISystemObjects,
                StackOutputFlags,
                ThreadIdFilter,
                HRESULTFilter,
                szModuleName,
                /* uNumberOfCaptureCollections */ 1,
                &LastCapturedFrame
                ));

        //
        // Compare last captures stack to current stack looking for some correlation
        //

        bool fFoundCorrelation = false;

        for (UINT uCapturedOffset = 1;
             (   (uCapturedOffset < ARRAYSIZE(LastCapturedFrame.rgCapturedFrame))
              && (LastCapturedFrame.rgCapturedFrame[uCapturedOffset] != 0));
             uCapturedOffset++)
        {
            ULONG64 CapturedOffset = LastCapturedFrame.rgCapturedFrame[uCapturedOffset];

            for (UINT uInterestingFrame = uFirstInterestingFrame;
                 uInterestingFrame < uFrames;
                 uInterestingFrame++)
            {
                DEBUG_STACK_FRAME const &StackFrame = StackFrames[uInterestingFrame];

                if (StackFrame.ReturnOffset == CapturedOffset)
                {
                    uFirstInterestingFrame = uInterestingFrame + 1;
                    fFoundCorrelation = true;
                    break;
                }
            }

            if (fFoundCorrelation)
            {
                break;
            }

            //
            // Output this captured offset that has not been correlated.
            //

            IFC(DumpStackCaptureFrame(
                pOutCtl,
                pISymbols,
                StackOutputFlags,
                CapturedOffset,
                0
                ));
        }

        if (!fFoundCorrelation)
        {
            IGNORE_HR(pOutCtl->Output(
                "Failed to match end of stack capture to current call stack.\n"
                " This may indicate that the capture is for a different error.\n"
                ));
        }
    }

    if (uFrames > uFirstInterestingFrame)
    {
        // Dump the interesting part of the call stack
        pOutCtl->OutputStackTrace(
            &StackFrames[uFirstInterestingFrame],
            uFrames - uFirstInterestingFrame,
            StackOutputFlags
            );
    }

    // Print out comments
    if (fHitMilUnexpectedError)
    {
        IFC(pOutCtl->Output("\n"));
        IFC(pOutCtl->Output("Note: the stack above combines potential stack capture and %d frames from the current call stack.\n",
                            uFrames - uFirstInterestingFrame
                            ));
        IFC(pOutCtl->Output("\n"));
        IFC(pOutCtl->Output("Summary: MilUnexpectedError in %s.\n",
                            szCurrentFunctionName
                            ));             
    }

    if (fPrintFollowup)
    {
        IFC(pOutCtl->Output("Followup: milstrs\n"));
    }

Cleanup:
    RRETURN(hr);
}


//+----------------------------------------------------------------------------
//
//  Function:  MILAnalyze
//
//  Synopsis:  Debugger extension for analyzing milcore.dll crashes.
//
//-----------------------------------------------------------------------------

DECLARE_API(milanalyze)
{
    HRESULT hr = S_OK;

    BEGIN_API(MILAnalyze);
    UNUSED_PARAMETER(args);

    OutputControl OutCtl(Client);           
    OutputControl* pOutCtl = &OutCtl;

    //
    // Variables for output prefix
    //

    bool PopPrefix = false;
    ULONG64 hPrefixPopHandle = 0;
    PDEBUG_CLIENT5 pIClient5 = NULL;

    IDebugDataSpaces* pIData = NULL;
    IDebugSymbols3* pISymbols = NULL;
    IDebugControl* pIControl = NULL;
    IDebugSystemObjects4 *pISystemObjects = NULL;

    //
    // Process options
    //

    bool BadSwitch = false;
    bool PrefixOutput = false;
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
            case 'P': PrefixOutput = true; break;
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
            "Usage: !milanalyze [-?P]\n"
            "\n"
            "  P    - Prefix all output with ["TARGETNAME_STR"] for toolability.\n"
            ));
    }
    else
    {
        if (PrefixOutput)
        {
            //
            // Prefix all output lines with a marker
            //

            IGNORE_HR(OutCtl.SetOutputLinePrefix("["TARGETNAME_STR"] "));

            if (SUCCEEDED(Client->QueryInterface(__uuidof(IDebugClient5), (void **)&pIClient5)))
            {
                PopPrefix = SUCCEEDED(
                    pIClient5->PushOutputLinePrefix("["TARGETNAME_STR"] ", &hPrefixPopHandle));
            }
        }


        MILX_TRACE_ENTRY;


        // Obtain debug library interfaces for looking up symbols, etc.    
        IFC(Client->QueryInterface(__uuidof(IDebugDataSpaces), (void **)&pIData));
        IFC(Client->QueryInterface(__uuidof(IDebugSymbols3), (void **)&pISymbols));
        IFC(Client->QueryInterface(__uuidof(IDebugControl), (void **)&pIControl));
        IFC(Client->QueryInterface(__uuidof(IDebugSystemObjects4), (void **)&pISystemObjects));

        // Dump the current call stack, combined with the stack capture if necessary.
        IFC(MILAnalyzeImpl(
                pOutCtl, 
                pIData,
                pISymbols, 
                pIControl,
                pISystemObjects
                ));
    }

Cleanup:
    IGNORE_HR(OutCtl.Output("\n"));

    if (FAILED(hr))
    {
        OutCtl.OutErr("MILAnalyze failed because of HR: %x\n\n", hr);        

        if (IsOutOfMemory(hr))
        {
            OutCtl.OutErr("Memory is low: try unloading unnecessary modules and re-run the extension.\n");
        }
    }

    if (PopPrefix)
    {
        IGNORE_HR(pIClient5->PopOutputLinePrefix(hPrefixPopHandle));
    }

    ReleaseInterface(pIData);
    ReleaseInterface(pISymbols);
    ReleaseInterface(pIControl);
    ReleaseInterface(pISystemObjects);
    ReleaseInterface(pIClient5);

    RRETURN(hr);
}


