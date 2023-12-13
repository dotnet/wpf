// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++        
    Abstract:

        Win32PrinterThunk - This is object that does the Win32 thunking for a PrintQueue
        based on the level specified in the constructor. The object has the knowledge of calling
        the thunked GetPrinter, SetPrinter and EnumPrinters APIs. 
        
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 

#ifndef __GENERICPRINTERINFOLEVELTHUNK_HPP__
#include "GenericPrinterLevelThunk.hpp"
#endif 



using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::PrintQueueThunk;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForPrintQueue;

/*++

Routine Name:   

    Win32PrinterThunk

Routine Description:

    Constructor 

Arguments:

    infoLevel       - Win32 level
    infoLevelMask   - mnask associated with the level
    
Return Value:

    N\A

--*/
Win32PrinterThunk::
Win32PrinterThunk(
    UInt32          infoLevel,
    InfoLevelMask   infoCoverageMask
    ) : InfoLevelThunk(infoLevel, 
                       infoCoverageMask)
{   
}

/*++

Routine Name:   

    CallWin32ApiToGetPrintInfoData

Routine Description:

    This method calls the GetPrinter API via the PrinterThunkHandler
    object.

Arguments:

    printThunkHandler - wrapper object around a Win32 printer handle
    
Return Value:

    void

--*/
void
Win32PrinterThunk::
CallWin32ApiToGetPrintInfoData(
    PrinterThunkHandler^    printThunkHandler,
    Object^                 cookie
    )
{
    if (!PrintInfoData)
    {
        PrintInfoData = printThunkHandler->ThunkGetPrinter(Level);
    }

    if (PrintInfoData)
    {
        Succeeded = true;
    }
}

/*++

Routine Name:   

    CallWin32ApiToEnumeratePrintInfoData

Routine Description:

    This method calls the EnumPrinters API via the PrinterThunkHandler
    object.

Arguments:

    serverName - server name
    flags      - enumeration flags 
    
Return Value:

    void

--*/
UInt32
Win32PrinterThunk::
CallWin32ApiToEnumeratePrintInfoData(
    String^     serverName,
    UInt32      flags
    )
{
    UInt32    printerCount = 0;

    PrintInfoData = PrinterThunkHandler::ThunkEnumPrinters(serverName, Level, flags);
    printerCount = (PrintInfoData != nullptr) ? PrintInfoData->Count : 0;

    return printerCount;
}

/*++

Routine Name:   

    BeginCallWin32ApiToSetPrintInfoData
    

Routine Description:

    The Win32 print APIs model for Sets require a Get operation to be called 
    to get the buffer containing all properties in a level, 
    apply the changed data in the buffer then call Set with the altered buffer.

    This method creates the managed "PrinterInfo^" wrapper for the level specified in the constructor.
    The "PrinterInfo^" object calls the GetPrinter API on the constructor.
    
Arguments:

    printThunkHandler - wrapper object around a Win32 printer handle
    
Return Value:

    void

--*/
void
Win32PrinterThunk::
BeginCallWin32ApiToSetPrintInfoData(
    PrinterThunkHandler^ printThunkHandler
    )
{
    switch (Level)
    {
        case 1:
        {
            PrintInfoData = gcnew PrinterInfoOne();            
            break;
        }
        case 2:
        {
            PrintInfoData = gcnew PrinterInfoTwoSetter(printThunkHandler);            
            break;
        }
        case 3:
        {
            PrintInfoData = gcnew PrinterInfoThree();                    
            break;
        }
        case 4:
        {
            PrintInfoData = gcnew PrinterInfoFourSetter(printThunkHandler);   
            break;
        }
        case 5:
        {
            PrintInfoData = gcnew PrinterInfoFiveSetter(printThunkHandler);  
            break;
        }
        case 6:
        {
            PrintInfoData = gcnew PrinterInfoSix();
            break;
        }
        case 7:
        {
            PrintInfoData = gcnew PrinterInfoSeven();
            break;
        }
        case 8:
        {
            PrintInfoData = gcnew PrinterInfoEight();
            break;
        }
        case 9:
        {
            PrintInfoData = gcnew PrinterInfoNine();
            break;
        }            
        default:
        {
            break;
        }
    }
}

/*++

Routine Name:   

    EndCallWin32ApiToSetPrintInfoData

Routine Description:

    This method calls the SetPrinter API via the PrinterThunkHandler
    object. the unmanged buffer was previously built by calling 
    BeginCallWin32ApiToSetPrintInfoData.

Arguments:

    printThunkHandler - wrapper object around a Win32 printer handle
    
Return Value:

    void

--*/
void
Win32PrinterThunk::
EndCallWin32ApiToSetPrintInfoData(
    PrinterThunkHandler^ printThunkHandler
    )
{
    SafeMemoryHandle^ win32Buffer = ((IPrinterInfo^)PrintInfoData)->Win32SafeHandle;
    printThunkHandler->ThunkSetPrinter(Level, win32Buffer);
}
