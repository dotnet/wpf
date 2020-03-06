// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        Win32JobThunk - This is object that does the Win32 thunking for a Job
        based on the level specified in the constructor. The object has the knowledge of calling
        the thunked GetJob, EnumJobs APIs. 
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 

#ifndef __GENERICJOBINFOLEVELTHUNK_HPP__
#include "GenericJobLevelThunk.hpp"
#endif 



using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::JobThunk;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForJob;

/*++

Routine Name:   

    Win32JobThunk

Routine Description:

    Constructor 

Arguments:

    infoLevel       - Win32 level
    infoLevelMask   - mnask associated with the level
    
Return Value:

    N\A

--*/
Win32JobThunk::
Win32JobThunk(
    UInt32              infoLevel,
    InfoLevelMask       infoCoverageMask
    ) : InfoLevelThunk(infoLevel, 
                       infoCoverageMask)
{
}

/*++

Routine Name:   

    CallWin32ApiToGetPrintInfoData

Routine Description:

    This method calls the GetPrinterDriver API via the PrinterThunkHandler
    object.

Arguments:

    printThunkHandler - wrapper object around a Win32 printer handle
    
Return Value:

    void

--*/
void
Win32JobThunk::
CallWin32ApiToGetPrintInfoData(
    PrinterThunkHandler^    printThunkHandler,
    Object^                 cookie
    )
{
    Int32   jobId = *((Int32^)cookie);

    if (!PrintInfoData)
    {
        PrintInfoData = printThunkHandler->ThunkGetJob(Level, jobId);
    }
}

/*++

Routine Name:   

    CallWin32ApiToEnumeratePrintInfoData

Routine Description:

    This method calls the EnumPrinterDrivers API via the PrinterThunkHandler
    object.

Arguments:

    serverName - server name
    flags      - enumeration flags 
    
Return Value:

    void

--*/
UInt32
Win32JobThunk::
CallWin32ApiToEnumeratePrintInfoData(
    PrinterThunkHandler^    printThunkHandler,
    UInt32                  firstJobId,
    UInt32                  numberOfJobs
    )
{
    UInt32               jobCount = 0;
    
    PrintInfoData = printThunkHandler->ThunkEnumJobs(Level, firstJobId, numberOfJobs);

    if(PrintInfoData)
    {
        jobCount = PrintInfoData->Count;
    }

    return jobCount;
}

/*++

Routine Name:   

    BeginCallWin32ApiToSetPrintInfoData
    

Routine Description:

    Not supported
    
Arguments:

    printThunkHandler - wrapper object around a Win32 printer handle
    
Return Value:

    void

--*/
void
Win32JobThunk::
BeginCallWin32ApiToSetPrintInfoData(
    PrinterThunkHandler^ printThunkHandler
    )
{   
    //throw "Win32JobThunk::BeginCallWin32ApiToSetPrintInfoData";
}


/*++

Routine Name:   

    EndCallWin32ApiToSetPrintInfoData
    

Routine Description:

    Not supported
    
Arguments:

    printThunkHandler - wrapper object around a Win32 printer handle
    
Return Value:

    void

--*/
void
Win32JobThunk::
EndCallWin32ApiToSetPrintInfoData(
    PrinterThunkHandler^ printThunkHandler
    )
{
    //throw "Win32JobThunk::EndCallWin32ApiToSetPrintInfoData";
}


/*++

Routine Name:   

    SetValueFromAttributeValue    

Routine Description:

    Overrides InfoLevelThunk::SetValueFromAttributeValue.
    The Driver doesn't support set operations.
    
Arguments:

    name  - name of the attribute.
    value - managed value to be set in the unmanaged buffer.    
    
Return Value:

    false

--*/
bool
Win32JobThunk::
SetValueFromAttributeValue(
    String^ name,
    Object^ value
    )
{
    return false;
}
