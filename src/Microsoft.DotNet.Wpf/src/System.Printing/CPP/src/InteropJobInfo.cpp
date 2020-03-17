// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                        
    Abstract:

        The file contains the definition for the managed classes that 
        hold the pointers to the JOB_INFO_ unmanaged structures and 
        know how to retrieve a property based on it's name. 
        
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif



using namespace MS::Internal::PrintWin32Thunk::DirectInteropForJob;
using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;

JobInfoOne::
JobInfoOne(
    SafeMemoryHandle^       unmanagedJobInfoSafeHandle,
    UInt32                  count
    ) : jobsCount(count)
{
    jobInfoOneSafeHandle = unmanagedJobInfoSafeHandle;    
}

void JobInfoOne::
Release(
    void
    )
{
    delete jobInfoOneSafeHandle;
    jobInfoOneSafeHandle = nullptr;
}

SafeMemoryHandle^
JobInfoOne::Win32SafeHandle::
get(
    void
    )
{
    return jobInfoOneSafeHandle;
} 

void
JobInfoOne::
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("Name",                       gcnew GetValue(&GetDocumentName));
    getAttributeMap->Add("JobIdentifier",              gcnew GetValue(&GetJobId));
    getAttributeMap->Add("PrintServer",                gcnew GetValue(&GetServerName));
    getAttributeMap->Add("PrintQueue",                 gcnew GetValue(&GetPrinterName));
    getAttributeMap->Add("Submitter",                  gcnew GetValue(&GetUserName));
    getAttributeMap->Add("Document",                   gcnew GetValue(&GetDocumentName));
    getAttributeMap->Add("PrintProcessorDatatype",     gcnew GetValue(&GetDatatype)); 
    getAttributeMap->Add("Status",                     gcnew GetValue(&GetStatus)); 
    getAttributeMap->Add("StatusDescription",          gcnew GetValue(&GetStatusString)); 
    getAttributeMap->Add("JobPriority",                gcnew GetValue(&GetPriority)); 
    getAttributeMap->Add("PositionInQueue",            gcnew GetValue(&GetPosition)); 
    getAttributeMap->Add("NumberOfPages",              gcnew GetValue(&GetTotalPages)); 
    getAttributeMap->Add("NumberOfPagesPrinted",       gcnew GetValue(&GetPagesPrinted)); 
    getAttributeMap->Add("TimeJobSubmitted",           gcnew GetValue(&GetTimeSubmitted)); 
    
}

Object^
JobInfoOne::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= jobsCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {
        JOB_INFO_1W* win32JobInfoOneArray = reinterpret_cast<JOB_INFO_1W*>(handle->DangerousGetHandle().ToPointer());
        
        return  getValueDelegate->Invoke(&win32JobInfoOneArray[index]);
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
JobInfoOne::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

UInt32
JobInfoOne::Count::
get(
    void
    )
{
    return jobsCount;
}

bool
JobInfoOne::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    return false;
}

Object^
JobInfoOne::
GetPrinterName(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pPrinterName);
}

Object^
JobInfoOne::
GetServerName(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pMachineName);
}

Object^
JobInfoOne::
GetUserName(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pUserName);
}

Object^
JobInfoOne::
GetDocumentName(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pDocument);
}

Object^
JobInfoOne::
GetDatatype(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pDatatype);
}

Object^
JobInfoOne::
GetStatusString(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pStatus);
}

Object^
JobInfoOne::
GetStatus(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->Status);
}

Object^
JobInfoOne::
GetJobId(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->JobId);
}

Object^
JobInfoOne::
GetPriority(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->Priority);
}

Object^
JobInfoOne::
GetPosition(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->Position);
}

Object^
JobInfoOne::
GetTotalPages(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->TotalPages);
}

Object^
JobInfoOne::
GetPagesPrinted(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->PagesPrinted);
}

Object^
JobInfoOne::
GetTimeSubmitted(
    JOB_INFO_1W* unmanagedJobInfo
    )
{
    DateTime    date = DateTime(unmanagedJobInfo->Submitted.wYear,
                                unmanagedJobInfo->Submitted.wMonth,
                                unmanagedJobInfo->Submitted.wDay,
                                unmanagedJobInfo->Submitted.wHour,
                                unmanagedJobInfo->Submitted.wMinute,
                                unmanagedJobInfo->Submitted.wSecond);

    return date;
}


/*--------------------------------------------------------------------------------------*/
/*                    JobInfoTwo Implementation                                         */
/*--------------------------------------------------------------------------------------*/

JobInfoTwo::
JobInfoTwo(
    SafeMemoryHandle^       unmanagedJobInfoSafeHandle,
    UInt32                  count
    ) : jobsCount(count)
{
    jobInfoTwoSafeHandle = unmanagedJobInfoSafeHandle;
}

void JobInfoTwo::
Release(
    void
    )
{
    delete jobInfoTwoSafeHandle;
    jobInfoTwoSafeHandle = nullptr;
}

void
JobInfoTwo::
RegisterAttributeMaps(
    void
    )
{
    getAttributeMap->Add("Name",                       gcnew GetValue(&GetDocumentName));
    getAttributeMap->Add("JobIdentifier",              gcnew GetValue(&GetJobId));
    getAttributeMap->Add("PrintServer",                gcnew GetValue(&GetServerName));
    getAttributeMap->Add("PrintQueue",                 gcnew GetValue(&GetPrinterName));
    getAttributeMap->Add("Submitter",                  gcnew GetValue(&GetUserName));
    getAttributeMap->Add("NotifyName",                 gcnew GetValue(&GetNotifyName));
    getAttributeMap->Add("Document",                   gcnew GetValue(&GetDocumentName));
    getAttributeMap->Add("QueueDriverName",            gcnew GetValue(&GetQueueDriverName)); 
    getAttributeMap->Add("PrintProcessor",             gcnew GetValue(&GetPrintProcessor));
    getAttributeMap->Add("PrintProcessorDatatype",     gcnew GetValue(&GetDatatype)); 
    getAttributeMap->Add("PrintProcessorParameters",   gcnew GetValue(&GetPrintProcessorParameters)); 
    getAttributeMap->Add("DevMode",                    gcnew GetValue(&GetDevMode)); 
    getAttributeMap->Add("Status",                     gcnew GetValue(&GetStatus)); 
    getAttributeMap->Add("StatusDescription",          gcnew GetValue(&GetStatusString)); 
    getAttributeMap->Add("JobPriority",                gcnew GetValue(&GetPriority)); 
    getAttributeMap->Add("PositionInQueue",            gcnew GetValue(&GetPosition)); 
    getAttributeMap->Add("NumberOfPages",              gcnew GetValue(&GetTotalPages)); 
    getAttributeMap->Add("NumberOfPagesPrinted",       gcnew GetValue(&GetPagesPrinted)); 
    getAttributeMap->Add("SecurityDescriptor",         gcnew GetValue(&GetSecurityDescriptor)); 
    getAttributeMap->Add("StartTimeOfDay",             gcnew GetValue(&GetStartTime)); 
    getAttributeMap->Add("UntilTimeOfDay",             gcnew GetValue(&GetUntilTime)); 
    getAttributeMap->Add("TimeJobSubmitted",           gcnew GetValue(&GetTimeSubmitted)); 
    getAttributeMap->Add("TimeSinceStartedPrinting",   gcnew GetValue(&GetTimeSinceSubmitted)); 
    getAttributeMap->Add("JobSize",                    gcnew GetValue(&GetSize)); 
    
}

SafeMemoryHandle^
JobInfoTwo::Win32SafeHandle::
get(
    void
    )
{
    return jobInfoTwoSafeHandle;
} 

Object^
JobInfoTwo::
GetValueFromName(
    String^ name,
    UInt32  index
    )
{
    if(index >= jobsCount)
    {
        throw gcnew ArgumentOutOfRangeException("index");
    }

    GetValue^ getValueDelegate = (GetValue^)getAttributeMap[name];

    Boolean mustRelease = false;
    SafeHandle^ handle = Win32SafeHandle;
    handle->DangerousAddRef(mustRelease);
    try 
    {
        JOB_INFO_2W* win32JobInfoOneArray = reinterpret_cast<JOB_INFO_2W*>(handle->DangerousGetHandle().ToPointer());
        
        return getValueDelegate->Invoke(&win32JobInfoOneArray[index]);
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
JobInfoTwo::
GetValueFromName(
    String^ name
    )
{
    return GetValueFromName(name, 0);
}

UInt32
JobInfoTwo::Count::
get(
    void
    )
{
    return jobsCount;
}

bool
JobInfoTwo::
SetValueFromName(
    String^ name,
    Object^ value
    )
{
    return false;
}

Object^
JobInfoTwo::
GetPrinterName(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pPrinterName);
}

Object^
JobInfoTwo::
GetServerName(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pMachineName);
}

Object^
JobInfoTwo::
GetQueueDriverName(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pDriverName);
}

Object^
JobInfoTwo::
GetUserName(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pUserName);
}

Object^
JobInfoTwo::
GetNotifyName(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pUserName);
}

Object^
JobInfoTwo::
GetDocumentName(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pDocument);
}

Object^
JobInfoTwo::
GetDatatype(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pDatatype);
}

Object^
JobInfoTwo::
GetPrintProcessor(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pPrintProcessor);
}

Object^
JobInfoTwo::
GetPrintProcessorParameters(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pParameters);
}

Object^
JobInfoTwo::
GetDevMode(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    DeviceMode^ devmode = gcnew DeviceMode(unmanagedJobInfo->pDevMode);

    return devmode->Data;
}

Object^
JobInfoTwo::
GetStatusString(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return gcnew String(unmanagedJobInfo->pStatus);
}

Object^
JobInfoTwo::
GetStatus(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    Int32   unmanagedStatus = static_cast<Int32>(unmanagedJobInfo->Status);

    PrintJobStatus  jobStatus = *(PrintJobStatus^)Enum::Parse(PrintJobStatus::typeid, 
                                                              unmanagedStatus.ToString(System::Globalization::CultureInfo::CurrentCulture));

    return static_cast<Int32>(jobStatus);
}

Object^
JobInfoTwo::
GetJobId(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->JobId);
}

Object^
JobInfoTwo::
GetPriority(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    Int32               unmanagedPriority = static_cast<Int32>(unmanagedJobInfo->Priority);

    PrintJobPriority jobPriority =  (unmanagedPriority >= static_cast<Int32>(PrintJobPriority::Maximum)) ?
                                            PrintJobPriority::Maximum :
                                            PrintJobPriority::Minimum;

    return static_cast<Int32>(jobPriority);
}

Object^
JobInfoTwo::
GetPosition(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->Position);
}

Object^
JobInfoTwo::
GetTotalPages(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->TotalPages);
}

Object^
JobInfoTwo::
GetPagesPrinted(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->PagesPrinted);
}

Object^
JobInfoTwo::
GetStartTime(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->StartTime);
}

Object^
JobInfoTwo::
GetUntilTime(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->UntilTime);
}

Object^
JobInfoTwo::
GetTimeSinceSubmitted(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->Time);
}

Object^
JobInfoTwo::
GetSize(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return static_cast<Int32>(unmanagedJobInfo->Size);
}

Object^
JobInfoTwo::
GetSecurityDescriptor(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    return nullptr;
}

Object^
JobInfoTwo::
GetTimeSubmitted(
    JOB_INFO_2W* unmanagedJobInfo
    )
{
    DateTime    date = DateTime(unmanagedJobInfo->Submitted.wYear,
                                unmanagedJobInfo->Submitted.wMonth,
                                unmanagedJobInfo->Submitted.wDay,
                                unmanagedJobInfo->Submitted.wHour,
                                unmanagedJobInfo->Submitted.wMinute,
                                unmanagedJobInfo->Submitted.wSecond);

    return date;
    
}

