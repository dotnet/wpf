// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                       
    Abstract:

        The file contains the implementation for the managed classes that 
        hold the pointers to the PRINTER_INFO_ unmanaged structures and know how 
        to retrieve a property based on it's name. 
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __INTERNALPRINTSYSTEMEXCEPTION_HPP__
#include <InternalPrintSystemException.hpp>
#endif



using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForPrintQueue;

/*++

Routine Name:   

    RegisterAttributeMaps

Routine Description:

    Static method that registers the methods to be called when a property is 
    retrieved.

Arguments:

    None
    
Return Value:

    N\A

--*/
void
PrinterInfoOne:: 
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("Flags",                gcnew GetValue(&GetFlags));
    getAttributeMap->Add("Description",          gcnew GetValue(&GetDescription));
    getAttributeMap->Add("Comment",              gcnew GetValue(&GetComment));        
}

/*++

Routine Name:   

    PrinterInfoOne

Routine Description:

    Constructor 

Arguments:

    unmanagedPrinterInfo    -   pointer to unmanaged buffer that contains an array of PRINTER_INFO_1 structures.
    ownsMemory              -   if true, this object is responsible with freeing the unmanaged memory
    count                   -   number of structures in the unmanaged buffer
Return Value:

    N\A

--*/
PrinterInfoOne:: 
PrinterInfoOne(
    SafeMemoryHandle^   safeHandle,
    UInt32              count
    ) : printersCount(count)
{
    printerInfoOneSafeHandle = safeHandle;
}

/*++

Routine Name:   

    PrinterInfoOne

Routine Description:

    Constructor 

Arguments:

    None

Return Value:

    N\A

--*/
PrinterInfoOne::
PrinterInfoOne(
    void
    ) : printersCount(1)
{
    printerInfoOneSafeHandle = gcnew PrinterInfoOneSafeMemoryHandle();
}

/*++

Routine Name:   

    ~PrinterInfoOne

Routine Description:

    Destructor 

Arguments:

    None

Return Value:

    N\A

--*/
void PrinterInfoOne::
Release(
    void
    )
{
    delete printerInfoOneSafeHandle;
    printerInfoOneSafeHandle = nullptr;
}

/*++

Routine Name:   

    get_Win32SafeHandle

Routine Description:

    Property for the Win32 IntPtr unmanaged buffer.

Arguments:

    None

Return Value:

    N\A

--*/
SafeMemoryHandle^
PrinterInfoOne::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoOneSafeHandle;
}

/*++

Routine Name:   

    GetComment

Routine Description:

    Returns PRINTER_INFO_1W->pComment as a managed String.

Arguments:

    unmanagedPrinterInfo - Pointer to unmanaged memory
    
Return Value:

    N\A

--*/
Object^
PrinterInfoOne:: 
GetComment(
    PRINTER_INFO_1W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pComment);
}

/*++

Routine Name:   

    GetDescription

Routine Description:

    Returns PRINTER_INFO_1W->pDescription as a managed String.

Arguments:

    unmanagedPrinterInfo - Pointer to unmanaged memory
    
Return Value:

    N\A

--*/
Object^
PrinterInfoOne:: 
GetDescription(
    PRINTER_INFO_1W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pDescription);
}

/*++

Routine Name:   

    GetFlags

Routine Description:

    Returns PRINTER_INFO_1W->Flags as a Int32 boxed object.

Arguments:

    unmanagedPrinterInfo - Pointer to unmanaged memory
    
Return Value:

    N\A

--*/
Object^
PrinterInfoOne:: 
GetFlags(
    PRINTER_INFO_1W* unmanagedPrinterInfo
    )
{
    Int32^    flags = static_cast<Int32>(unmanagedPrinterInfo->Flags);

    return flags;
}

/*++

Routine Name:   

    GetValueFromName

Routine Description:

    Returns a property as a managed object.

Arguments:

    name    - property name
    index   - index in the PRINTER_INFO_1W array
    
Return Value:

    Pointer to an namaged object that holds a copy of the unmanaged property data.

--*/
Object^
PrinterInfoOne:: 
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_1W* win32PrinterInfoOneArray = reinterpret_cast<PRINTER_INFO_1W*>(handle->DangerousGetHandle().ToPointer());
        
        return getValueDelegate->Invoke(&win32PrinterInfoOneArray[index]);
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }
}

/*++

Routine Name:   

    SetValueFromName

Routine Description:

    Not Supported

Arguments:

    name    - property name
    value   - value
    
Return Value:

    False

--*/
bool
PrinterInfoOne:: 
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}

/*++

Routine Name:   

    GetValueFromName

Routine Description:

    Returns a property as a managed object.

Arguments:

    name    - property name
    
Return Value:

    Pointer to an namaged object that holds a copy of the unmanaged property data.

--*/
Object^
PrinterInfoOne:: 
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

UInt32
PrinterInfoOne::Count::
get(
    void
    )
{
    return printersCount;
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoTwoGetter Implementation                               */
/*--------------------------------------------------------------------------------------*/

void
PrinterInfoTwoGetter:: 
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("HostingPrintServerName",    gcnew GetValue(&GetServerName));
    getAttributeMap->Add("Name",                      gcnew GetValue(&GetPrinterName));
    getAttributeMap->Add("ShareName",                 gcnew GetValue(&GetShareName));
    getAttributeMap->Add("QueuePortName",             gcnew GetValue(&GetPortName));
    getAttributeMap->Add("QueueDriverName",           gcnew GetValue(&GetDriverName));
    getAttributeMap->Add("Comment",                   gcnew GetValue(&GetComment));
    getAttributeMap->Add("Location",                  gcnew GetValue(&GetLocation));
    getAttributeMap->Add("SeparatorFile",             gcnew GetValue(&GetSeparatorFile));
    getAttributeMap->Add("QueuePrintProcessorName",   gcnew GetValue(&GetPrintProcessor));
    getAttributeMap->Add("PrintProcessorDatatype",    gcnew GetValue(&GetPrintProcessorDatatype));
    getAttributeMap->Add("PrintProcessorParameters",  gcnew GetValue(&GetPrintProcessorParameters));
    getAttributeMap->Add("SecurityDescriptor",        gcnew GetValue(&GetSecurityDescriptor));
    getAttributeMap->Add("Attributes",                gcnew GetValue(&GetAttributes));
    getAttributeMap->Add("Priority",                  gcnew GetValue(&GetPriority));
    getAttributeMap->Add("DefaultPriority",           gcnew GetValue(&GetDefaultPriority));
    getAttributeMap->Add("StartTimeOfDay",            gcnew GetValue(&GetStartTime));
    getAttributeMap->Add("UntilTimeOfDay",            gcnew GetValue(&GetUntilTime));
    getAttributeMap->Add("Status",                    gcnew GetValue(&GetStatus));
    getAttributeMap->Add("AveragePagesPerMinute",     gcnew GetValue(&GetAveragePPM));
    getAttributeMap->Add("NumberOfJobs",              gcnew GetValue(&GetJobs));
    getAttributeMap->Add("UserDevMode",               gcnew GetValue(&GetDeviceMode));
    getAttributeMap->Add("DefaultDevMode",            gcnew GetValue(&GetDeviceMode));
}

PrinterInfoTwoGetter::
PrinterInfoTwoGetter(
    SafeMemoryHandle^   safeHandle,
    UInt32              count
    ) : printersCount(count)
{
    printerInfoTwoSafeHandle = safeHandle;
}


void PrinterInfoTwoGetter::
Release(
    void
    )
{
    delete printerInfoTwoSafeHandle;
    printerInfoTwoSafeHandle = nullptr;
}

SafeMemoryHandle^
PrinterInfoTwoGetter::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoTwoSafeHandle;
}

UInt32
PrinterInfoTwoGetter::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoTwoGetter::
GetServerName(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return unmanagedPrinterInfo->pServerName ? 
                gcnew String(unmanagedPrinterInfo->pServerName) : 
                PrinterThunkHandler::GetLocalMachineName();
}

Object^
PrinterInfoTwoGetter::
GetPrinterName(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pPrinterName);
}

Object^
PrinterInfoTwoGetter::
GetShareName(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pShareName);
}

Object^
PrinterInfoTwoGetter::
GetPortName(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pPortName);
}

Object^
PrinterInfoTwoGetter::
GetDriverName(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pDriverName);
}

Object^
PrinterInfoTwoGetter::
GetComment(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pComment);
}

Object^
PrinterInfoTwoGetter::
GetLocation(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pLocation);
}

Object^
PrinterInfoTwoGetter::
GetDeviceMode(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    DeviceMode^ devmode = gcnew DeviceMode(unmanagedPrinterInfo->pDevMode);

    return devmode->Data;
}

Object^
PrinterInfoTwoGetter::
GetSeparatorFile(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pSepFile);
}

Object^ 
PrinterInfoTwoGetter::
GetPrintProcessor(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pPrintProcessor);       
}

Object^
PrinterInfoTwoGetter::
GetPrintProcessorDatatype(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pDatatype);
}

Object^ 
PrinterInfoTwoGetter::
GetPrintProcessorParameters(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pDatatype);
}

Object^
PrinterInfoTwoGetter::
GetSecurityDescriptor(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    return IntPtr(unmanagedPrinterInfo->pSecurityDescriptor);
}

Object^
PrinterInfoTwoGetter::
GetAttributes(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    attributes = static_cast<Int32>(unmanagedPrinterInfo->Attributes);

    return attributes;
}

Object^
PrinterInfoTwoGetter::
GetPriority(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    priority = static_cast<Int32>(unmanagedPrinterInfo->Priority);

    return priority;
}

Object^
PrinterInfoTwoGetter::
GetDefaultPriority(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    defaultPriority = static_cast<Int32>(unmanagedPrinterInfo->DefaultPriority);

    return defaultPriority;
}

Object^
PrinterInfoTwoGetter::
GetStartTime(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    startTime = static_cast<Int32>(unmanagedPrinterInfo->StartTime);
    return startTime;
}

Object^
PrinterInfoTwoGetter::
GetUntilTime(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    untilTime = static_cast<Int32>(unmanagedPrinterInfo->UntilTime);
    return untilTime;
}

Object^
PrinterInfoTwoGetter::
GetStatus(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    status = static_cast<Int32>(unmanagedPrinterInfo->Status);
    return status;      
}

Object^
PrinterInfoTwoGetter::
GetAveragePPM(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32^    averagePPM = static_cast<Int32>(unmanagedPrinterInfo->AveragePPM);

    return averagePPM;
}


Object^
PrinterInfoTwoGetter::
GetJobs(
    PRINTER_INFO_2W* unmanagedPrinterInfo
    )
{
    Int32 jobs = unmanagedPrinterInfo->cJobs;

    Int32^    boxed_jobs = jobs;

    return boxed_jobs;
}


Object^
PrinterInfoTwoGetter::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_2W* win32PrinterInfoTwoArray = reinterpret_cast<PRINTER_INFO_2W*>(handle->DangerousGetHandle().ToPointer());
        
        return getValueDelegate->Invoke(&win32PrinterInfoTwoArray[index]);
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }
}

Object^
PrinterInfoTwoGetter::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

bool
PrinterInfoTwoGetter::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}


/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoTwoSetter Implementation                               */
/*--------------------------------------------------------------------------------------*/
void
PrinterInfoTwoSetter:: 
RegisterAttributeMaps(
    void
    )
{
    setAttributeMap->Add("HostingPrintServerName",    gcnew SetValue(&SetServerName));
    setAttributeMap->Add("Name",                      gcnew SetValue(&SetPrinterName));
    setAttributeMap->Add("ShareName",                 gcnew SetValue(&SetShareName));
    setAttributeMap->Add("QueuePortName",             gcnew SetValue(&SetPortName));
    setAttributeMap->Add("QueueDriverName",           gcnew SetValue(&SetDriverName));
    setAttributeMap->Add("Comment",                   gcnew SetValue(&SetComment));
    setAttributeMap->Add("Location",                  gcnew SetValue(&SetLocation));
    setAttributeMap->Add("SeparatorFile",             gcnew SetValue(&SetSeparatorFile));
    setAttributeMap->Add("QueuePrintProcessorName",   gcnew SetValue(&SetPrintProcessor));
    setAttributeMap->Add("PrintProcessorDatatype",    gcnew SetValue(&SetPrintProcessorDatatype));
    setAttributeMap->Add("PrintProcessorParameters",  gcnew SetValue(&SetPrintProcessorParameters));
    setAttributeMap->Add("SecurityDescriptor",        gcnew SetValue(&SetSecurityDescriptor));
    setAttributeMap->Add("Attributes",                gcnew SetValue(&SetAttributes));
    setAttributeMap->Add("Priority",                  gcnew SetValue(&SetPriority));
    setAttributeMap->Add("DefaultPriority",           gcnew SetValue(&SetDefaultPriority));
    setAttributeMap->Add("StartTimeOfDay",            gcnew SetValue(&SetStartTime));
    setAttributeMap->Add("UntilTimeOfDay",            gcnew SetValue(&SetUntilTime));
    setAttributeMap->Add("Status",                    gcnew SetValue(&SetStatus));
    setAttributeMap->Add("AveragePagesPerMinute",     gcnew SetValue(&SetAveragePPM));
    setAttributeMap->Add("NumberOfJobs",              gcnew SetValue(&SetJobs));           

}

PrinterInfoTwoSetter::
PrinterInfoTwoSetter(
    PrinterThunkHandler^ printThunkHandler
    )
{
    internalMembersList = gcnew array<SafeMemoryHandle^>(setAttributeMap->Count);

    IPrinterInfo^ printerInfo = printThunkHandler->ThunkGetPrinter(2);

    win32PrinterInfoSafeHandle = printerInfo->Win32SafeHandle;

    Boolean mustRelease = false;
    win32PrinterInfoSafeHandle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_2W* win32PrinterInfoTwo = reinterpret_cast<PRINTER_INFO_2W*>(win32PrinterInfoSafeHandle->DangerousGetHandle().ToPointer());

        win32PrinterInfoTwo->pSecurityDescriptor = NULL;    
    }
    finally
    {
        if(mustRelease)
        {
            win32PrinterInfoSafeHandle->DangerousRelease();
        }
    }
}

PrinterInfoTwoSetter::
PrinterInfoTwoSetter(
    void
    ) 
{
    internalMembersList = gcnew array<SafeMemoryHandle^>(setAttributeMap->Count);

    win32PrinterInfoSafeHandle = gcnew SafeMemoryHandle(UnmanagedPrinterInfoLevelBuilder::BuildEmptyUnmanagedPrinterInfoTwo());
}

void PrinterInfoTwoSetter::
Release(
    void
    )
{
    if(win32PrinterInfoSafeHandle != nullptr)
    {
        if (!win32PrinterInfoSafeHandle->IsInvalid)
        {
            for (int index = 0; index < internalMembersIndex; index++)
            {
                delete internalMembersList[index];
                internalMembersList[index] = nullptr;
            }
        }    

        delete win32PrinterInfoSafeHandle;
        win32PrinterInfoSafeHandle = nullptr;
    }
}

SafeMemoryHandle^
PrinterInfoTwoSetter::Win32SafeHandle::
get(
    void
    )
{
    return win32PrinterInfoSafeHandle;
}

UInt32
PrinterInfoTwoSetter::Count::
get(
    void
    )
{
    return 1;
}


Object^
PrinterInfoTwoSetter::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}

bool
PrinterInfoTwoSetter::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    bool    returnValue = false;

    if(value)
    {
        SetValue^ setValueDelegate = (SetValue^)setAttributeMap[name];
        if(setValueDelegate)
        {
            Boolean mustRelease = false;
            SafeHandle^ handle = Win32SafeHandle;
            handle->DangerousAddRef(mustRelease);
            try 
            {        
                internalMembersList[internalMembersIndex] = gcnew SafeMemoryHandle(
                    setValueDelegate->Invoke(handle->DangerousGetHandle(), value)
                    );
            }
            finally
            {
                if(mustRelease)
                {
                    handle->DangerousRelease();
                }
            }
            
            internalMembersIndex++;        
        }
    }
    return returnValue;
}

IntPtr
PrinterInfoTwoSetter::
SetServerName(
    IntPtr,
    Object^
    )
{
    return IntPtr::Zero;
}

IntPtr
PrinterInfoTwoSetter::
SetPrinterName(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ printerName = (String^)value;

    return 
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        printerName,
                                                                        offsetof(PRINTER_INFO_2W, pPrinterName));
    
}

IntPtr
PrinterInfoTwoSetter::
SetShareName(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ shareName = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        shareName,
                                                                        offsetof(PRINTER_INFO_2W, pShareName));
    
}

IntPtr
PrinterInfoTwoSetter::
SetPortName(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ portName = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        portName,
                                                                        offsetof(PRINTER_INFO_2W, pPortName));
}

IntPtr
PrinterInfoTwoSetter::
SetDriverName(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ driverName = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        driverName,
                                                                        offsetof(PRINTER_INFO_2W, pDriverName));
}

IntPtr
PrinterInfoTwoSetter::
SetComment(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ comment = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        comment,
                                                                        offsetof(PRINTER_INFO_2W, pComment));
}

IntPtr
PrinterInfoTwoSetter::
SetLocation(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ location = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        location,
                                                                        offsetof(PRINTER_INFO_2W, pLocation));
}

IntPtr
PrinterInfoTwoSetter::
SetSeparatorFile(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ separatorFile = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        separatorFile,
                                                                        offsetof(PRINTER_INFO_2W, pSepFile));
}

IntPtr
PrinterInfoTwoSetter::
SetPrintProcessor(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ printProcessor = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        printProcessor,
                                                                        offsetof(PRINTER_INFO_2W, pPrintProcessor));
}

IntPtr
PrinterInfoTwoSetter::
SetPrintProcessorDatatype(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ printProcessorDataType = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        printProcessorDataType,
                                                                        offsetof(PRINTER_INFO_2W, pDatatype));
}

IntPtr
PrinterInfoTwoSetter::
SetPrintProcessorParameters(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    String^ printProcessorParameters = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        printProcessorParameters,
                                                                        offsetof(PRINTER_INFO_2W, pParameters));

}

IntPtr
PrinterInfoTwoSetter::
SetSecurityDescriptor(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    IntPtr nullPointer = IntPtr::Zero;

    IntPtr securityDescriptor;

    UnmanagedPrinterInfoLevelBuilder::WriteIntPtrInUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                        securityDescriptor,
                                                                        offsetof(PRINTER_INFO_2W, pSecurityDescriptor));

    return nullPointer;
}

IntPtr
PrinterInfoTwoSetter::
SetAttributes(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    IntPtr nullPointer = IntPtr::Zero;

    Int32 attributes = *((Int32^)value);

    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                       attributes,
                                                                       offsetof(PRINTER_INFO_2W, Attributes));
    return nullPointer;
}

IntPtr
PrinterInfoTwoSetter::
SetPriority(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    IntPtr nullPointer = IntPtr::Zero;

    Int32 priority = *((Int32^)value);

    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                       priority,
                                                                       offsetof(PRINTER_INFO_2W, Priority));
    return nullPointer;
}

IntPtr
PrinterInfoTwoSetter::
SetDefaultPriority(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    IntPtr nullPointer = IntPtr::Zero;

    Int32 defaultPriority = *((Int32^)value);

    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                       defaultPriority,
                                                                       offsetof(PRINTER_INFO_2W, DefaultPriority));
    return nullPointer;
}

IntPtr
PrinterInfoTwoSetter::
SetStartTime(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    IntPtr nullPointer = IntPtr::Zero;

    Int32 startTime = *((Int32^)value);

    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                       startTime,
                                                                       offsetof(PRINTER_INFO_2W, StartTime));
    return nullPointer;
}

IntPtr
PrinterInfoTwoSetter::
SetUntilTime(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    IntPtr nullPointer = IntPtr::Zero;

    Int32 untilTime = *((Int32^)value);

    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoTwoBuffer, 
                                                                       untilTime,
                                                                       offsetof(PRINTER_INFO_2W, UntilTime));
    return nullPointer;
}

IntPtr
PrinterInfoTwoSetter::
SetStatus(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    return IntPtr::Zero;
}

IntPtr
PrinterInfoTwoSetter::
SetAveragePPM(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    return IntPtr::Zero;
}

IntPtr
PrinterInfoTwoSetter::
SetJobs(
    IntPtr  printerInfoTwoBuffer,
    Object^ value
    )
{     
    return IntPtr::Zero;
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoThree Implementation                                   */
/*--------------------------------------------------------------------------------------*/
PrinterInfoThree::
PrinterInfoThree(
    SafeMemoryHandle^       safeHandle,
    UInt32                  count
    ) : printersCount(count)
        
{
    printerInfoThreeSafeHandle = safeHandle;    
}

PrinterInfoThree::
PrinterInfoThree(
    void
    ) : printersCount(1)
{
    printerInfoThreeSafeHandle = gcnew PrinterInfoThreeSafeMemoryHandle();
}

void
PrinterInfoThree::
Release(
    void
    )
{
    delete printerInfoThreeSafeHandle;
    printerInfoThreeSafeHandle = nullptr;
}


SafeMemoryHandle^
PrinterInfoThree::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoThreeSafeHandle;
}
UInt32
PrinterInfoThree::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoThree::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_3* win32PrinterInfoThreeArray = reinterpret_cast<PRINTER_INFO_3*>(handle->DangerousGetHandle().ToPointer());
        
        return IntPtr((&win32PrinterInfoThreeArray[index])->pSecurityDescriptor);
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }
}

Object^
PrinterInfoThree::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

bool
PrinterInfoThree:: 
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    return false;
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoFourGetter Implementation                              */
/*--------------------------------------------------------------------------------------*/

void
PrinterInfoFourGetter::
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("HostingPrintServerName",    gcnew GetValue(&GetServerName));
    getAttributeMap->Add("Name",                      gcnew GetValue(&GetPrinterName));
    getAttributeMap->Add("Attributes",                gcnew GetValue(&GetAttributes));        
}

PrinterInfoFourGetter::
PrinterInfoFourGetter(
    SafeMemoryHandle^   safeHandle,
    UInt32              count
    ) : printersCount(count)
{
    printerInfoFourSafeHandle = safeHandle;    
}

void PrinterInfoFourGetter::
Release(
    void
    )
{
    delete printerInfoFourSafeHandle;
    printerInfoFourSafeHandle = nullptr;
}


SafeMemoryHandle^
PrinterInfoFourGetter::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoFourSafeHandle;
} 

UInt32
PrinterInfoFourGetter::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoFourGetter::
GetAttributes(
    PRINTER_INFO_4W* unmanagedPrinterInfo
    )
{
    Int32^    attributes = static_cast<Int32>(unmanagedPrinterInfo->Attributes);

    return attributes;
}

Object^
PrinterInfoFourGetter::
GetServerName(
    PRINTER_INFO_4W* unmanagedPrinterInfo
    )
{
    return unmanagedPrinterInfo->pServerName ? 
                gcnew String(unmanagedPrinterInfo->pServerName) : 
                PrinterThunkHandler::GetLocalMachineName();
}

Object^
PrinterInfoFourGetter::
GetPrinterName(
    PRINTER_INFO_4W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pPrinterName);
}

Object^
PrinterInfoFourGetter::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];

    Boolean mustRelease = false;
    SafeHandle^ handle = printerInfoFourSafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_4W* win32PrinterInfoFourArray = reinterpret_cast<PRINTER_INFO_4W*>(handle->DangerousGetHandle().ToPointer());
        
        return getValueDelegate->Invoke(&win32PrinterInfoFourArray[index]);
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }
}

Object^
PrinterInfoFourGetter::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

bool
PrinterInfoFourGetter::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}


/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoFourSetter Implementation                              */
/*--------------------------------------------------------------------------------------*/
void
PrinterInfoFourSetter::
RegisterAttributeMaps(
    void
    )
{
    setAttributeMap->Add("HostingPrintServerName",    gcnew SetValue(&SetServerName));
    setAttributeMap->Add("Name",                      gcnew SetValue(&SetPrinterName));
    setAttributeMap->Add("Attributes",                gcnew SetValue(&SetAttributes));
}

PrinterInfoFourSetter::
PrinterInfoFourSetter(
    PrinterThunkHandler^ printThunkHandler
    )
{
    internalMembersList = gcnew array<SafeMemoryHandle^>(setAttributeMap->Count);

    printerInfo = printThunkHandler->ThunkGetPrinter(4);    
}

void
PrinterInfoFourSetter::
Release(
    void
    )
{
    if (nullptr != printerInfo)
    {
        for (int index = 0; index < internalMembersIndex; index++)
        {
            delete internalMembersList[index];
            internalMembersList[index] = nullptr;
        }

        delete printerInfo;
        printerInfo = nullptr;
    }                
}


SafeMemoryHandle^
PrinterInfoFourSetter::Win32SafeHandle::
get(
    void
    )
{
    return printerInfo->Win32SafeHandle;
} 

UInt32
PrinterInfoFourSetter::Count::
get(
    void
    )
{
    return 1;
}

Object^
PrinterInfoFourSetter::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}

bool
PrinterInfoFourSetter::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    bool    returnValue = false;

    SetValue^ setValueDelegate = (SetValue^)setAttributeMap[name];
    
    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        internalMembersList[internalMembersIndex] = gcnew SafeMemoryHandle(
            setValueDelegate->Invoke(handle->DangerousGetHandle(), value)
            );
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }
    
    internalMembersIndex++;        

    return returnValue;
}

IntPtr
PrinterInfoFourSetter::
SetAttributes(
    IntPtr  printerInfoFourBuffer,
    Object^ value
    )
{     
    Int32 attributes = *((Int32^)value);

    return (IntPtr)
    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoFourBuffer, 
                                                                       attributes,
                                                                       offsetof(PRINTER_INFO_4W, Attributes));

}

IntPtr
PrinterInfoFourSetter::
SetServerName(
    IntPtr,
    Object^
    )
{
    return IntPtr::Zero;
}

IntPtr
PrinterInfoFourSetter::
SetPrinterName(
    IntPtr  printerInfoFourBuffer,
    Object^ value
    )
{     
    String^ printerName = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoFourBuffer, 
                                                                        printerName,
                                                                        offsetof(PRINTER_INFO_4W, pPrinterName));
    
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoFiveGetter Implementation                              */
/*--------------------------------------------------------------------------------------*/

void
PrinterInfoFiveGetter::
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("Name",                      gcnew GetValue(&GetPrinterName));
    getAttributeMap->Add("QueuePortName",             gcnew GetValue(&GetPortName));
    getAttributeMap->Add("Attributes",                gcnew GetValue(&GetAttributes));        
    getAttributeMap->Add("TransmissionRetryTimeout",  gcnew GetValue(&GetTransmissionRetryTimeout));        
    getAttributeMap->Add("DeviceNotSelectedTimeout",  gcnew GetValue(&GetDeviceNotSelectedTimeout));        
}

PrinterInfoFiveGetter::
PrinterInfoFiveGetter(
    SafeMemoryHandle^   safeHandle,
    UInt32              count
    ) : printersCount(count)
{
    printerInfoFiveSafeHandle = safeHandle;    
}

void PrinterInfoFiveGetter::
Release(
    void
    )
{
    delete printerInfoFiveSafeHandle;
    printerInfoFiveSafeHandle = nullptr;    
}


SafeMemoryHandle^
PrinterInfoFiveGetter::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoFiveSafeHandle;
} 

UInt32
PrinterInfoFiveGetter::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoFiveGetter::
GetAttributes(
    PRINTER_INFO_5W* unmanagedPrinterInfo
    )
{
    Int32^    attributes = static_cast<Int32>(unmanagedPrinterInfo->Attributes);

    return attributes;
}

Object^
PrinterInfoFiveGetter::
GetPortName(
    PRINTER_INFO_5W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pPortName);
}

Object^
PrinterInfoFiveGetter::
GetPrinterName(
    PRINTER_INFO_5W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pPrinterName);
}

Object^
PrinterInfoFiveGetter::
GetDeviceNotSelectedTimeout(
    PRINTER_INFO_5W* unmanagedPrinterInfo
    )
{
    Int32^    deviceNotSelectedTimeout = static_cast<Int32>(unmanagedPrinterInfo->DeviceNotSelectedTimeout);

    return deviceNotSelectedTimeout;
}

Object^
PrinterInfoFiveGetter::
GetTransmissionRetryTimeout(
    PRINTER_INFO_5W* unmanagedPrinterInfo
    )
{
    Int32^    transmissionRetryTimeout = static_cast<Int32>(unmanagedPrinterInfo->TransmissionRetryTimeout);

    return transmissionRetryTimeout;
}

Object^
PrinterInfoFiveGetter::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];
    
    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_5W* win32PrinterInfoFiveArray = reinterpret_cast<PRINTER_INFO_5W*>(handle->DangerousGetHandle().ToPointer());
        
        return getValueDelegate->Invoke(&win32PrinterInfoFiveArray[index]);   
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }
}

bool
PrinterInfoFiveGetter:: 
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}

/*--------------------------------------------------------------------------------------*/
/*                    PrinterInfoFiveSetter Implementation                              */
/*--------------------------------------------------------------------------------------*/
void
PrinterInfoFiveSetter::
RegisterAttributeMaps(
    void
    )
{
    setAttributeMap->Add("Name",                      gcnew SetValue(&SetPrinterName));
    setAttributeMap->Add("QueuePortName",             gcnew SetValue(&SetPortName));
    setAttributeMap->Add("Attributes",                gcnew SetValue(&SetAttributes));        
    setAttributeMap->Add("TransmissionRetryTimeout",  gcnew SetValue(&SetTransmissionRetryTimeout));        
    setAttributeMap->Add("DeviceNotSelectedTimeout",  gcnew SetValue(&SetDeviceNotSelectedTimeout));        
}

PrinterInfoFiveSetter::
PrinterInfoFiveSetter(
    PrinterThunkHandler^ printThunkHandler
    ) 
{
    internalMembersList = gcnew array<SafeMemoryHandle^>(setAttributeMap->Count);

    printerInfo = printThunkHandler->ThunkGetPrinter(5);    
}

void PrinterInfoFiveSetter::
Release(
    void
    )
{
    if (printerInfo != nullptr)
    {
        for (int index = 0; index < internalMembersIndex; index++)
        {
            delete internalMembersList[index];
            internalMembersList[index] = nullptr;
        }

        delete printerInfo;
        printerInfo = nullptr;
    }                
}

SafeMemoryHandle^
PrinterInfoFiveSetter::Win32SafeHandle::
get(
    void
    )
{
    return printerInfo->Win32SafeHandle;
} 

UInt32
PrinterInfoFiveSetter::Count::
get(
    void
    )
{
    return 1;
}

Object^
PrinterInfoFiveSetter::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}

bool
PrinterInfoFiveSetter::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    bool    returnValue = false;

    SetValue^ setValueDelegate = (SetValue^)setAttributeMap[name];
    
    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        internalMembersList[internalMembersIndex] = gcnew SafeMemoryHandle(
                                                            setValueDelegate->Invoke(handle->DangerousGetHandle(), value)
                                                            );
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }    
    
    internalMembersIndex++;        

    return returnValue;
}

IntPtr
PrinterInfoFiveSetter::
SetPortName(
    IntPtr  printerInfoFiveBuffer,
    Object^ value
    )
{     
    String^ portName = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoFiveBuffer, 
                                                                        portName,
                                                                        offsetof(PRINTER_INFO_5W, pPortName));
    
}


IntPtr
PrinterInfoFiveSetter::
SetAttributes(
    IntPtr  printerInfoFiveBuffer,
    Object^ value
    )
{     
    Int32 attributes = *((Int32^)value);

    return (IntPtr)
    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoFiveBuffer, 
                                                                       attributes,
                                                                       offsetof(PRINTER_INFO_5W, Attributes));
}

IntPtr
PrinterInfoFiveSetter::
SetPrinterName(
    IntPtr  printerInfoFiveBuffer,
    Object^ value
    )
{     
    String^ printerName = (String^)value;

    return
    UnmanagedPrinterInfoLevelBuilder::WriteStringInUnmanagedPrinterInfo(printerInfoFiveBuffer, 
                                                                        printerName,
                                                                        offsetof(PRINTER_INFO_5W, pPrinterName));
    
}

IntPtr
PrinterInfoFiveSetter::
SetDeviceNotSelectedTimeout(
    IntPtr  printerInfoFiveBuffer,
    Object^ value
    )
{     
    Int32 deviceNotSelectedTimeout = *((Int32^)value);

    return (IntPtr)
    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoFiveBuffer, 
                                                                       deviceNotSelectedTimeout,
                                                                       offsetof(PRINTER_INFO_5W, DeviceNotSelectedTimeout));
}

IntPtr
PrinterInfoFiveSetter::
SetTransmissionRetryTimeout(
    IntPtr  printerInfoFiveBuffer,
    Object^ value
    )
{     
    Int32 transmissionRetryTimeout = *((Int32^)value);

    return (IntPtr)
    UnmanagedPrinterInfoLevelBuilder::WriteInt32InUnmanagedPrinterInfo(printerInfoFiveBuffer, 
                                                                       transmissionRetryTimeout,
                                                                       offsetof(PRINTER_INFO_5W, TransmissionRetryTimeout));
}


/*--------------------------------------------------------------------------------------*/
/*                       PrinterInfoSix Implementation                                  */
/*--------------------------------------------------------------------------------------*/

PrinterInfoSix::
PrinterInfoSix(
    SafeMemoryHandle^     safeHandle,
    UInt32  count
    ) : printersCount(count)
{
    printerInfoSixSafeHandle = safeHandle;    
}

PrinterInfoSix::
PrinterInfoSix(
    void
    ) : printersCount(1)
{
    printerInfoSixSafeHandle = gcnew PrinterInfoSixSafeMemoryHandle();
}

void PrinterInfoSix::
Release(
    void
    )
{
    delete printerInfoSixSafeHandle;
    printerInfoSixSafeHandle = nullptr;
}


SafeMemoryHandle^
PrinterInfoSix::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoSixSafeHandle;
}

UInt32
PrinterInfoSix::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoSix::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_6* win32PrinterInfoSixArray = reinterpret_cast<PRINTER_INFO_6*>(handle->DangerousGetHandle().ToPointer());
        
        return static_cast<Int32>((&win32PrinterInfoSixArray[index])->dwStatus);
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }                    
}

Object^
PrinterInfoSix::
GetValueFromName(
    String^ name
    )
{    
    return GetValueFromName(name, 0);
}

bool
PrinterInfoSix:: 
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    throw gcnew InternalPrintSystemException(ERROR_NOT_SUPPORTED);
}


/*--------------------------------------------------------------------------------------*/
/*                       PrinterInfoSeven Implementation                                */
/*--------------------------------------------------------------------------------------*/
void
PrinterInfoSeven::
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("ObjectGUID",      gcnew GetValue(&GetObjectGUID));
    getAttributeMap->Add("Action",          gcnew GetValue(&GetAction));

    setAttributeMap->Add("ObjectGUID",      gcnew SetValue(&SetObjectGUID));
    setAttributeMap->Add("Action",          gcnew SetValue(&SetAction));
    
}

PrinterInfoSeven::
PrinterInfoSeven(
    SafeMemoryHandle^   safeHandle,
    UInt32              count
    ) : printersCount(count),
        objectOwnsInternalUnmanagedMembers(false)
{
    printerInfoSevenSafeHandle = safeHandle;    
}

PrinterInfoSeven::
PrinterInfoSeven(
    void
    ) : printersCount(1),
        objectOwnsInternalUnmanagedMembers(true)
{
    printerInfoSevenSafeHandle = gcnew PrinterInfoSevenSafeMemoryHandle();
}

void PrinterInfoSeven::
Release(
    void
    )
{
    delete printerInfoSevenSafeHandle;
    printerInfoSevenSafeHandle = nullptr;
}


SafeMemoryHandle^
PrinterInfoSeven::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoSevenSafeHandle;
}

UInt32
PrinterInfoSeven::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoSeven::
GetObjectGUID(
    PRINTER_INFO_7W* unmanagedPrinterInfo
    )
{
    return gcnew String(unmanagedPrinterInfo->pszObjectGUID);
}

Object^
PrinterInfoSeven::
GetAction(
    PRINTER_INFO_7W* unmanagedPrinterInfo
    )
{
    Int32^    action = static_cast<Int32>(unmanagedPrinterInfo->dwAction);

    return action;
}

Object^
PrinterInfoSeven::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

Object^
PrinterInfoSeven::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_7W* win32PrinterInfoSevenArray = reinterpret_cast<PRINTER_INFO_7W*>(handle->DangerousGetHandle().ToPointer());
        
        return getValueDelegate->Invoke(&win32PrinterInfoSevenArray[index]);
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }                          
}

bool
PrinterInfoSeven::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    bool    returnValue = false;

    if (objectOwnsInternalUnmanagedMembers)
    {
        SetValue^ getValueDelegate = (SetValue^)setAttributeMap[name];
        
        Boolean mustRelease = false;

        SafeHandle^ handle = Win32SafeHandle;
        handle->DangerousAddRef(mustRelease);
        try 
        {        
            returnValue = getValueDelegate->Invoke(handle->DangerousGetHandle(), value);
        }
        finally
        {
            if(mustRelease)
            {
                handle->DangerousRelease();
            }
        }
    }

    return returnValue;
}

bool
PrinterInfoSeven::
SetObjectGUID(
    IntPtr  printerInfoSevenBuffer,
    Object^ value
    )
{
    return false;
}


bool
PrinterInfoSeven::
SetAction(
    IntPtr  printerInfoSevenBuffer,
    Object^ value
    )
{
    return false;
}

/*--------------------------------------------------------------------------------------*/
/*                       PrinterInfoEight Implementation                                */
/*--------------------------------------------------------------------------------------*/
PrinterInfoEight::
PrinterInfoEight(
    SafeMemoryHandle^     safeHandle,
    UInt32                count
    ): printersCount(count),
       objectOwnsInternalUnmanagedMembers(false)
{
    printerInfoEightSafeHandle = safeHandle;    
}

PrinterInfoEight::
PrinterInfoEight(
    void
    ):  printersCount(1),
        objectOwnsInternalUnmanagedMembers(true)
{
    printerInfoEightSafeHandle = gcnew PrinterInfoEightSafeMemoryHandle();
}

void PrinterInfoEight::
Release(
    void
    )
{
    delete printerInfoEightSafeHandle;
    printerInfoEightSafeHandle = nullptr;
}


SafeMemoryHandle^
PrinterInfoEight::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoEightSafeHandle;
}

UInt32
PrinterInfoEight::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoEight::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);               
}

Object^
PrinterInfoEight::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_8W* win32PrinterInfoEightArray = reinterpret_cast<PRINTER_INFO_8W*>(handle->DangerousGetHandle().ToPointer());
        
        DeviceMode^ devmode = gcnew DeviceMode((&(win32PrinterInfoEightArray[index]))->pDevMode);             

        return devmode->Data;
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }    
}

bool
PrinterInfoEight::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    bool returnValue = false;

    if (objectOwnsInternalUnmanagedMembers)
    {
        array<Byte>^ data  = (array<Byte>^)value;
        DeviceMode^ devMode = gcnew DeviceMode(data);

        IntPtr   devModeUnmanaged = Marshal::AllocHGlobal(devMode->Data->Length);
        Marshal::Copy((array<Byte>^)devMode->Data, 0, devModeUnmanaged, devMode->Data->Length);        

        Boolean mustRelease = false;
        SafeHandle^ handle = Win32SafeHandle;
        handle->DangerousAddRef(mustRelease);
        try 
        {            
            UnmanagedPrinterInfoLevelBuilder::WriteDevModeInUnmanagedPrinterInfoEight(handle->DangerousGetHandle(), 
                                                                                  devModeUnmanaged);
        }
        finally
        {
            if(mustRelease)
            {
                handle->DangerousRelease();
            }
        }                
    }

    return returnValue;
}

/*--------------------------------------------------------------------------------------*/
/*                       PrinterInfoNine Implementation                                 */
/*--------------------------------------------------------------------------------------*/

PrinterInfoNine::
PrinterInfoNine(
    SafeMemoryHandle^   printerInfoSevenSafeHandle,
    UInt32              count
    ) : printersCount(count),
        objectOwnsInternalUnmanagedMembers(false)
{
    printerInfoNineSafeHandle = printerInfoSevenSafeHandle;    
}

PrinterInfoNine::
PrinterInfoNine(
    void
    ):  printersCount(1),
        objectOwnsInternalUnmanagedMembers(true)
{
    printerInfoNineSafeHandle = gcnew PrinterInfoNineSafeMemoryHandle();
}

void 
PrinterInfoNine::
Release(
    void
    )
{
    delete printerInfoNineSafeHandle;
    printerInfoNineSafeHandle = nullptr;
}

SafeMemoryHandle^
PrinterInfoNine::Win32SafeHandle::
get(
    void
    )
{
    return printerInfoNineSafeHandle;
}

UInt32
PrinterInfoNine::Count::
get(
    void
    )
{
    return printersCount;
}

Object^
PrinterInfoNine::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= printersCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {        
        PRINTER_INFO_9W* win32PrinterInfoNineArray = reinterpret_cast<PRINTER_INFO_9W*>(handle->DangerousGetHandle().ToPointer());
        
        DeviceMode^ devmode = gcnew DeviceMode((&(win32PrinterInfoNineArray[index]))->pDevMode);              

        return devmode->Data;
    }
    finally
    {
        if(mustRelease)
        {
            handle->DangerousRelease();
        }
    }    
}

Object^
PrinterInfoNine::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

bool
PrinterInfoNine::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    bool returnValue = false;

    if (objectOwnsInternalUnmanagedMembers)
    {
        array<Byte>^ data  = (array<Byte>^)value;
        DeviceMode^ devMode = gcnew DeviceMode(data);

        IntPtr   devModeUnmanaged = Marshal::AllocHGlobal(devMode->Data->Length);
        Marshal::Copy((array<Byte>^)devMode->Data, 0, devModeUnmanaged, devMode->Data->Length);        

        Boolean mustRelease = false;
        SafeHandle^ handle = Win32SafeHandle;
        handle->DangerousAddRef(mustRelease);
        try 
        {        
            UnmanagedPrinterInfoLevelBuilder::WriteDevModeInUnmanagedPrinterInfoNine(handle->DangerousGetHandle(), devModeUnmanaged);            
        }
        finally
        {
            if(mustRelease)
            {
                handle->DangerousRelease();
            }
        }    
    }

    return returnValue;
}
