// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
    Abstract:

        Win32DriverThunk - This is object that does the Win32 thunking for a Driver
        based on the level specified in the constructor. The object has the knowledge of calling
        the thunked GetDriver, EnumPrinterDrivers APIs. 
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 

#ifndef __GENERICDRIVERINFOLEVELTHUNK_HPP__
#include "GenericDriverLevelThunk.hpp"
#endif 



using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::DriverThunk;

/*++

Routine Name:   

    Win32DriverThunk

Routine Description:

    Constructor 

Arguments:

    infoLevel       - Win32 level
    infoLevelMask   - mnask associated with the level
    
Return Value:

    N\A

--*/
Win32DriverThunk::
Win32DriverThunk(
    UInt32              level,
    InfoLevelMask       levelMask
    ) : InfoLevelThunk(level, 
                       levelMask)
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
Win32DriverThunk::
CallWin32ApiToGetPrintInfoData(
    PrinterThunkHandler^    printThunkHandler,
    Object^                 cookie
    )
{
    if (!PrintInfoData)
    {
            PrintInfoData = printThunkHandler->ThunkGetDriver(Level, nullptr);
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
Win32DriverThunk::
CallWin32ApiToEnumeratePrintInfoData(
    String^     serverName,
    UInt32      flags
    )
{
    UInt32               driverCount = 0;
    PrinterThunkHandler^ printThunkHandler = nullptr;

    try
    {
        printThunkHandler = gcnew PrinterThunkHandler(serverName);
        PrintInfoData = printThunkHandler->ThunkEnumDrivers(Level, nullptr);
        driverCount = PrintInfoData->Count;
        
    }
    __finally
    {
        //printThunkHandler->Dispose();
        delete printThunkHandler;
    }

    return driverCount;
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
Win32DriverThunk::
BeginCallWin32ApiToSetPrintInfoData(
    PrinterThunkHandler^ printThunkHandler
    )
{   
    //throw "Win32DriverThunk::BeginCallWin32ApiToSetPrintInfoData";
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
Win32DriverThunk::
EndCallWin32ApiToSetPrintInfoData(
    PrinterThunkHandler^ printThunkHandler
    )
{
    //throw "Win32DriverThunk::EndCallWin32ApiToSetPrintInfoData";
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
Win32DriverThunk::
SetValueFromAttributeValue(
    String^ name,
    Object^ value
    )
{
    return false;
}
