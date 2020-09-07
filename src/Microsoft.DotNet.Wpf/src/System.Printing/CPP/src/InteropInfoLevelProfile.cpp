// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                        
    Abstract:

        InfoLevelThunk - Abstract base class for the object that it's being created 
        for each level that is being thunked to unmanaged code.
--*/
#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 

#ifndef  __INTERNALPRINTSYSTEMEXCEPTION_HPP__
#include <InternalPrintSystemException.hpp>
#endif



using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;

/*++

Routine Name:   

    InfoLevelThunk

Routine Description:

    Constructor 

Arguments:

    None.
    
Return Value:

    N\A

--*/
InfoLevelThunk::
InfoLevelThunk(
    void
    ) : levelMask(InfoLevelMask::NoLevel),
        printInfoData(nullptr)
{
}

/*++

Routine Name:   

    InfoLevelThunk

Routine Description:

    Constructor 

Arguments:

    infoLevel       - Win32 level
    infoLevelMask   - mnask associated with the level
    
Return Value:

    N\A

--*/
InfoLevelThunk::
InfoLevelThunk(
    UInt32          infoLevel,
    InfoLevelMask   infoLevelMask
    ) : level(infoLevel),
        levelMask(infoLevelMask),
        printInfoData(nullptr)
{   
}

/*++

Routine Name:   

    InternalDispose

Routine Description:

    Internal Dispose method.     

Arguments:

    disposing - true if called on destructor
    
Return Value:

    N\A

--*/
void
InfoLevelThunk::
Release(
    )
{
    if(!isDisposed)
    {
        if (printInfoData)
        {
            printInfoData->Release();
        }

        isDisposed = true;
    }
}

/*++

Routine Name:   

    get_Level

Routine Description:

    property

Arguments:

    N\A
    
Return Value:

    returns the level member

--*/
UInt32
InfoLevelThunk::Level::
get(
    )
{
    return level;
}
/*++

Routine Name:   

    get_Succeeded

Routine Description:

    property

Arguments:

    N\A
    
Return Value:

    true if the thunking operation succeeded.

--*/
bool
InfoLevelThunk::Succeeded::
get(
    void
    )
{
    return succeeded;
}

/*++

Routine Name:   

    set_Succeeded

Routine Description:

    property

Arguments:

    N\A
    
Return Value:

    true if the thunking operation succeeded.

--*/
void
InfoLevelThunk::Succeeded::
set(
    bool thunkingSucceeded
    )
{
    succeeded = thunkingSucceeded;
}


/*++

Routine Name:   

    get_PrintInfoData

Routine Description:

    property

Arguments:

    N\A
    
Return Value:

    returns the printInfoData member that wraps the unmanaged data.

--*/
IPrinterInfo^
InfoLevelThunk::PrintInfoData::
get(
    )
{
    return printInfoData;
}

/*++

Routine Name:   

    set_PrintInfoData

Routine Description:

    property

Arguments:

    pInfo - this is the object that wraps the unmanaged data.
            The type of the unmanaged data must be the same as 
            of level member inside this object.
    
Return Value:

    N/A

--*/
void
InfoLevelThunk::PrintInfoData::
set(
    IPrinterInfo^    printerInfo
    )
{
    printInfoData = printerInfo;
}

/*++

Routine Name:   

    get_LevelMask

Routine Description:

    property

Arguments:

    N\A
    
Return Value:

    returns the levelMask member

--*/
InfoLevelMask
InfoLevelThunk::LevelMask::
get(
    void
    )
{
    return levelMask;
}

/*++

Routine Name:   

    GetValueFromInfoData

Routine Description:

    Extracts the value of a given attribute out of the unmanaged buffer.
    The unmanaged buffer is assumed to contain only one structure. This applies for 
    Get operations.

Arguments:

    name - name of the attribute.
    
Return Value:

    Managed copy of the unmanaged data exposed as an object.

--*/
Object^
InfoLevelThunk::
GetValueFromInfoData(
    String^ name
    )
{
    Object^ value = nullptr;

    if (PrintInfoData->Count == 1)
    {
        value = PrintInfoData->GetValueFromName(name, 0);
    }

    return value;
}

/*++

Routine Name:   

    GetValueFromInfoData

Routine Description:

    Extracts the value of a given attribute out of the unmanaged buffer.
    The unmanaged buffer is assumed to contain more than one structure. 
    This applies for Enum operations.

Arguments:

    name  - name of the attribute.
    index - index of the structure inside the unmanaged buffer 
    
Return Value:

    Managed copy of the unmanaged data exposed as an object.

--*/
Object^
InfoLevelThunk::
GetValueFromInfoData(
    String^ valueName,
    UInt32  index
    )
{
    Object^ value = nullptr;

    if (PrintInfoData->Count)
    {
        value = PrintInfoData->GetValueFromName(valueName, index);
    }    

    return value;
}

/*++

Routine Name:   

    SetValueFromAttributeValue

Routine Description:

    Sets the value of a given attribute inside the unmanaged buffer.
    The unmanaged buffer is assumed to contain one structure. 
    This applies for Set operations.

Arguments:

    name  - name of the attribute.
    value - managed value to be set in the unmanaged buffer.
    
Return Value:

    true if succeeded.

--*/
bool
InfoLevelThunk::
SetValueFromAttributeValue(
    String^ valueName,
    Object^ value
    )
{
    bool    returnValue = false;

    returnValue = PrintInfoData->SetValueFromName(valueName, value);

    return returnValue;
}
