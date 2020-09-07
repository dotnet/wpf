// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:
        This file inlcudes the implementation of the PrintSystemJobInfo
        which is an encapsulation of spooler related operation and properties
        on a Print Job.
--*/

#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;
using namespace System::Xml;
using namespace System::Xml::XPath;

using namespace System::Printing::Interop;
using namespace System::Windows::Xps::Packaging;

using namespace System::Windows::Documents;


#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;


#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif

#ifndef  __OBJECTSATTRIBUTESVALUESFACTORY_HPP__
#include <ObjectsAttributesValuesFactory.hpp>
#endif

using namespace System::Printing::Activation;

#ifndef  __GETDATATHUNKOBJECT_HPP__
#include <GetDataThunkObject.hpp>
#endif

#ifndef  __ENUMDATATHUNKOBJECT_HPP__
#include <EnumDataThunkObject.hpp>
#endif

PrintSystemJobInfo::
PrintSystemJobInfo(
    PrintQueue^     printQueue,
    PrintTicket^    printTicket
    ) : hostingPrintQueue(printQueue),
        jobName(defaultJobName),
        printStream(nullptr),
        accessVerifier(nullptr)
{
    if (!hostingPrintQueue)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                       gcnew ArgumentNullException("printQueue"));
    }
    try
    {
        //
        // Initilizing the Document instance
        //
        Initialize();

        printStream = gcnew PrintQueueStream(hostingPrintQueue, jobName, false, printTicket);

        JobIdentifier = printStream->JobIdentifier;

        PopulateJobProperties(refreshPropertiesFilter);

    }
    catch (SystemException^ internalException)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                    internalException);
    }
}

PrintSystemJobInfo::
PrintSystemJobInfo(
    PrintQueue^     printQueue,
    String^         userJobName,
    PrintTicket^    printTicket
    ) :
    hostingPrintQueue(printQueue),
    jobName(userJobName ? userJobName : defaultJobName),
    accessVerifier(nullptr)
{
    if (!hostingPrintQueue)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                       gcnew ArgumentNullException("printQueue"));
    }
    try
    {
        //
        // Initializing the Document instance
        //
        Initialize();

        printStream = gcnew PrintQueueStream(hostingPrintQueue, jobName, false, printTicket);

        JobIdentifier = printStream->JobIdentifier;

        PopulateJobProperties(refreshPropertiesFilter);

    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintJobException(internalException->HResult,
                                      "PrintSystemException.PrintSystemJobInfo.Create");
    }
}

PrintSystemJobInfo::
PrintSystemJobInfo(
    PrintQueue^         printQueue,
    String^             userJobName,
    String^             documentPath,
    Boolean             fastCopy,
    PrintTicket^        printTicket
    ) :
    hostingPrintQueue(printQueue),
    jobName(userJobName ? userJobName : defaultJobName),
    printStream(nullptr),
    accessVerifier(nullptr)
{
    Int32   printJobIdentifier = 0;

    if (!hostingPrintQueue)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                          gcnew ArgumentNullException("printQueue"));
    }
    try
    {
        //
        // Initializing the Document instance
        //
        Initialize();

        //
        // Fast Copy: this will copy the container file as it is, chunk by chunk.
        // This is the fastest method and assured the printed job is identical with the original container.
        // There are no notifications sent to Print Spooler and the total page count cannot be calculated.
        //
        if (fastCopy)
        {
            if (hostingPrintQueue->IsXpsDevice)
            {
                PrintQueueStream^ printQueueStream = gcnew PrintQueueStream(hostingPrintQueue, jobName, false, printTicket, fastCopy);

                CopyFileStreamToPrinter(documentPath, printQueueStream);

                JobIdentifier = printQueueStream->JobIdentifier;

                PopulateJobProperties(refreshPropertiesFilter);

                delete printQueueStream;
            }
            else
            {
                throw gcnew NotSupportedException;
            }
        }
        else
        {
            hostingPrintQueue->CurrentJobSettings->Description = jobName;
            //
            // This is another way of sending the container file to be printed,
            // opening is as a XpsDocument and then simulate a SaveAs.
            // This is slow, but the job progress of having the document content
            // saved is propagated to the Print Spooler.
            //
            #pragma warning ( disable:4691 )
            XpsDocument^        xpsDocument = gcnew XpsDocument(documentPath, FileAccess::Read);
            XpsDocumentWriter^  writer      = gcnew XpsDocumentWriter(hostingPrintQueue);
            #pragma warning ( default:4691 )

            FixedDocumentSequence^  documentSequence  = xpsDocument->GetFixedDocumentSequence();

            writer->BeginPrintFixedDocumentSequence(documentSequence, printTicket, /* by ref */ printJobIdentifier);

            JobIdentifier = printJobIdentifier;

            //
            // This method will populate the print job properties only if the job id is known.
            // This is the reason for which XpsDocumentWriter::Write has to return a jobIdentifier.
            //
            try
            {

                PopulateJobProperties(refreshPropertiesFilter);
            }
            catch (InternalPrintSystemException^ internalException)
            {
                if (internalException->HResult != HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER))
                {
                    throw CreatePrintJobException(internalException->HResult,
                                                  "PrintSystemException.PrintSystemJobInfo.Create");
                }
            }

            writer->EndPrintFixedDocumentSequence();

            xpsDocument->Close();
        }
    }
    catch (SystemException^ internalException)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                      internalException);
    }
}

PrintSystemJobInfo::
PrintSystemJobInfo(
    PrintQueue^     printQueue,
    Int32           jobId
    ):
    hostingPrintQueue(printQueue),
    jobName(defaultJobName),
    jobIdentifier(jobId),
    accessVerifier(nullptr)
{
    if (!hostingPrintQueue)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                      gcnew ArgumentNullException("printQueue"));
    }

    //
    // Initilizing the Document instance
    //
    Initialize();

    try
    {
        PopulateJobProperties(refreshPropertiesFilter);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintJobException(internalException->HResult,
                                      "PrintSystemException.PrintSystemJobInfo.Create");
    }
}



void
PrintSystemJobInfo::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if(!IsDisposed)
                {
                    if(disposing)
                    {
                        if (printStream)
                        {
                            delete printStream;
                            printStream = nullptr;
                        }

                        if (hostingPrintServer)
                        {
                            delete hostingPrintServer;
                            hostingPrintServer = nullptr;
                        }

                        if (thunkPropertiesCollection)
                        {
                            delete thunkPropertiesCollection;
                            thunkPropertiesCollection = nullptr;
                        }
                    }
                }
            }
            __finally
            {
                __try
                {
                    PrintSystemObject::InternalDispose(disposing);
                }
                __finally
                {
                    this->IsDisposed = true;
                    System::Threading::Monitor::Exit(this);
                }
            }
        }
    }

}

PrintSystemJobInfo^
PrintSystemJobInfo::
Add(
    PrintQueue^     printQueue,
    PrintTicket^    printTicket
    )
{
    return Add(printQueue, defaultJobName, printTicket);
}

PrintSystemJobInfo^
PrintSystemJobInfo::
Add(
    PrintQueue^     printQueue,
    String^         jobName,
    PrintTicket^    printTicket
    )
{
    return gcnew PrintSystemJobInfo(printQueue, jobName, printTicket);
}


PrintSystemJobInfo^
PrintSystemJobInfo::
Add(
    PrintQueue^         printQueue,
    String^             jobName,
    String^             document,
    Boolean             fastCopy,
    PrintTicket^        printTicket
    )
{
    return gcnew PrintSystemJobInfo(printQueue, jobName, document, fastCopy, printTicket);
}

void
PrintSystemJobInfo::
PopulateJobProperties(
    array<String^>^ propertiesAsStrings
    )
{
    GetDataThunkObject^ dataThunkObject = nullptr;

    try
    {
        dataThunkObject = gcnew GetDataThunkObject(this->GetType());

        dataThunkObject->Cookie = jobIdentifier;

        dataThunkObject->PopulatePrintSystemObject(hostingPrintQueue->PrinterThunkHandler,
                                                   this,
                                                   propertiesAsStrings);
    }
    __finally
    {
        delete dataThunkObject;
    }
}


PrintSystemJobInfo^
PrintSystemJobInfo::
Get(
    PrintQueue^     printQueue,
    Int32           jobID
    )
{
    PrintSystemJobInfo^ jobInfo = gcnew PrintSystemJobInfo(printQueue, jobID);
    return jobInfo;
}

void
PrintSystemJobInfo::
Pause(
    void
    )
{
    VerifyAccess();

    if (!isDeleted)
    {
        try
        {
            hostingPrintQueue->PrinterThunkHandler->ThunkSetJob(jobIdentifier, JOB_CONTROL_PAUSE);

            get_InternalPropertiesCollection("Status")->GetProperty("Status")->IsInternallyInitialized = true;
            this->JobStatusSecondary = static_cast<Int32>(jobStatus | PrintJobStatus::Paused);
        }
        catch (InternalPrintSystemException^ internalException)
        {
            throw CreatePrintJobException(internalException->HResult,
                                          "PrintSystemException.PrintSystemJobInfo.Generic");
        }
    }
    else
    {
       throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Deleted");
    }
}


void
PrintSystemJobInfo::
Resume(
    void
    )
{
    VerifyAccess();

    if (!isDeleted)
    {
        try
        {
            hostingPrintQueue->PrinterThunkHandler->ThunkSetJob(jobIdentifier, JOB_CONTROL_RESUME);

            get_InternalPropertiesCollection("Status")->GetProperty("Status")->IsInternallyInitialized = true;
            this->JobStatusSecondary = static_cast<Int32>(jobStatus & (~PrintJobStatus::Paused));
        }
        catch (InternalPrintSystemException^ internalException)
        {
            throw CreatePrintJobException(internalException->HResult,
                                     "PrintSystemException.PrintSystemJobInfo.Generic");
        }
    }
    else
    {
       throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Deleted");
    }
}

void
PrintSystemJobInfo::
Cancel(
    void
    )
{
    VerifyAccess();

    if (!isDeleted)
    {
        try
        {
            hostingPrintQueue->PrinterThunkHandler->ThunkSetJob(jobIdentifier, JOB_CONTROL_DELETE);

            get_InternalPropertiesCollection("Status")->GetProperty("Status")->IsInternallyInitialized = true;
            this->JobStatusSecondary = static_cast<Int32>(PrintJobStatus::Deleted);
        }
        catch (InternalPrintSystemException^ internalException)
        {
            throw CreatePrintJobException(internalException->HResult,
                                        "PrintSystemException.PrintSystemJobInfo.Generic");
        }
    }
    else
    {
       throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Deleted");
    }
}

void
PrintSystemJobInfo::
Restart(
    void
    )
{
    VerifyAccess();

    if (!isDeleted)
    {
        try
        {
            hostingPrintQueue->PrinterThunkHandler->ThunkSetJob(jobIdentifier, JOB_CONTROL_RESTART);

            get_InternalPropertiesCollection("Status")->GetProperty("Status")->IsInternallyInitialized = true;
            this->JobStatusSecondary = static_cast<Int32>(jobStatus | PrintJobStatus::Restarted);
        }
        catch (InternalPrintSystemException^ internalException)
        {
            throw CreatePrintJobException(internalException->HResult,
                                          "PrintSystemException.PrintSystemJobInfo.Generic");
        }
    }
    else
    {
       throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Deleted");
    }
}

Stream^
PrintSystemJobInfo::JobStream::
get(
    void
    )
{
    VerifyAccess();

    return printStream;
}


Int32
PrintSystemJobInfo::JobIdentifier::
get(
    void
    )
{
    VerifyAccess();

    return jobIdentifier;
}

void
PrintSystemJobInfo::JobIdentifier::
set(
    Int32   internalJobID
    )
{
    VerifyAccess();

    jobIdentifier = internalJobID;
    PropertiesCollection->GetProperty("JobIdentifier")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("JobIdentifier")->Value = jobIdentifier;
}

String^
PrintSystemJobInfo::Submitter::
get(
    void
    )
{
    VerifyAccess();

    return submitter;
}

void
PrintSystemJobInfo::Submitter::
set(
    String^     jobSubmitter
    )
{
    VerifyAccess();

    submitter = jobSubmitter;
    PropertiesCollection->GetProperty("Submitter")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("Submitter")->Value = submitter;
}

PrintJobPriority
PrintSystemJobInfo::Priority::
get(
    void
    )
{
    VerifyAccess();

    return priority;
}

void
PrintSystemJobInfo::Priority::
set(
    PrintJobPriority jobPriority
    )
{
    VerifyAccess();

    priority = jobPriority;
    PropertiesCollection->GetProperty("Priority")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("Priority")->Value = priority;
}

void
PrintSystemJobInfo::PrioritySecondary::
set(
    Int32 jobPriority
    )
{
    priority = static_cast<PrintJobPriority>(jobPriority);

    if(get_InternalPropertiesCollection("JobPriority")->GetProperty("JobPriority")->IsInternallyInitialized)
    {
        PropertiesCollection->GetProperty("Priority")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("Priority")->Value = priority;
    }
}

Int32
PrintSystemJobInfo::PositionInPrintQueue::
get(
    void
    )
{
    VerifyAccess();

    return positionInPrintQueue;
}

void
PrintSystemJobInfo::PositionInPrintQueue::
set(
    Int32 positionInQueue
    )
{
    this->positionInPrintQueue = positionInQueue;
    PropertiesCollection->GetProperty("PositionInQueue")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("PositionInQueue")->Value = positionInPrintQueue;
}

Int32
PrintSystemJobInfo::StartTimeOfDay::
get(
    void
    )
{
    VerifyAccess();

    return startTime;
}

void
PrintSystemJobInfo::StartTimeOfDay::
set(
    Int32 newStartTime
    )
{
    VerifyAccess();

    startTime = newStartTime;
    PropertiesCollection->GetProperty("StartTimeOfDay")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("StartTimeOfDay")->Value = startTime;
}

Int32
PrintSystemJobInfo::UntilTimeOfDay::
get(
    void
    )
{
    VerifyAccess();

    return untilTime;
}

void
PrintSystemJobInfo::UntilTimeOfDay::
set(
    Int32 newUntilTime
    )
{
    VerifyAccess();

    untilTime = newUntilTime;
    PropertiesCollection->GetProperty("UntilTimeOfDay")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("UntilTimeOfDay")->Value = untilTime;
}

Int32
PrintSystemJobInfo::NumberOfPages::
get(
    void
    )
{
    VerifyAccess();

    return numberOfPages;
}

void
PrintSystemJobInfo::NumberOfPages::
set(
    Int32   newNumberOfPages
    )
{
    VerifyAccess();

    numberOfPages = newNumberOfPages;
    PropertiesCollection->GetProperty("NumberOfPages")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("NumberOfPages")->Value = numberOfPages;
}

void
PrintSystemJobInfo::NumberOfPagesPrinted::
set(
    Int32   newNumberOfPagesPrinted
    )
{
    VerifyAccess();

    numberOfPagesPrinted = newNumberOfPagesPrinted;
    PropertiesCollection->GetProperty("NumberOfPagesPrinted")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("NumberOfPagesPrinted")->Value = numberOfPagesPrinted;
}

Int32
PrintSystemJobInfo::NumberOfPagesPrinted::
get(
    void
    )
{
    VerifyAccess();

    return numberOfPagesPrinted;
}

Int32
PrintSystemJobInfo::JobSize::
get(
    void
    )
{
    VerifyAccess();

    return jobSize;
}

void
PrintSystemJobInfo::JobSize::
set(
    Int32   newJobSize
    )
{
    VerifyAccess();

    jobSize = newJobSize;
    PropertiesCollection->GetProperty("JobSize")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("JobSize")->Value = jobSize;
}

DateTime
PrintSystemJobInfo::TimeJobSubmitted::
get(
    void
    )
{
    VerifyAccess();

    return timeJobSubmitted;
}

void
PrintSystemJobInfo::TimeJobSubmitted::
set(
    DateTime    newTimeJobSubmitted
    )
{
    VerifyAccess();

    timeJobSubmitted = newTimeJobSubmitted;
    PropertiesCollection->GetProperty("TimeJobSubmitted")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("TimeJobSubmitted")->Value = timeJobSubmitted;
}

Int32
PrintSystemJobInfo::TimeSinceStartedPrinting::
get(
    void
    )
{
    VerifyAccess();

    return timeSinceStartedPrinting;
}

PrintJobStatus
PrintSystemJobInfo::JobStatus::
get(
    void
    )
{
    VerifyAccess();

    return jobStatus;
}

void
PrintSystemJobInfo::JobStatus::
set(
    PrintJobStatus  status
    )
{
    VerifyAccess();
}

void
PrintSystemJobInfo::JobStatusSecondary::
set(
    Int32  status
    )
{
    jobStatus = static_cast<PrintJobStatus>(status);

    if(get_InternalPropertiesCollection("Status")->GetProperty("Status")->IsInternallyInitialized)
    {
        PropertiesCollection->GetProperty("JobStatus")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("JobStatus")->Value = jobStatus;

        isCompleted = (status & (static_cast<Int32>(PrintJobStatus::Completed))) ? true : false;
        isDeleting = (status & (static_cast<Int32>(PrintJobStatus::Deleting))) ? true : false;
        isPaused = (status & (static_cast<Int32>(PrintJobStatus::Paused))) ? true : false;
        isPrinted = (status & (static_cast<Int32>(PrintJobStatus::Printed))) ? true : false;
        isRestarted = (status & (static_cast<Int32>(PrintJobStatus::Restarted))) ? true : false;
        isSpooling = (status & (static_cast<Int32>(PrintJobStatus::Spooling))) ? true : false;
        isPrinting = (status & (static_cast<Int32>(PrintJobStatus::Printing))) ? true : false;
        isInError = (status & (static_cast<Int32>(PrintJobStatus::Error))) ? true : false;
        isOffline = (status & (static_cast<Int32>(PrintJobStatus::Offline))) ? true : false;
        isPaperOut = (status & (static_cast<Int32>(PrintJobStatus::PaperOut))) ? true : false;
        isDeleted = (status & (static_cast<Int32>(PrintJobStatus::Deleted))) ? true : false;
        isBlocked = (status & (static_cast<Int32>(PrintJobStatus::Blocked))) ? true : false;
        isUserInterventionRequired = (status & (static_cast<Int32>(PrintJobStatus::UserIntervention))) ? true : false;
        isRetained = (status & (static_cast<Int32>(PrintJobStatus::Retained))) ? true : false;
    }
}

Boolean
PrintSystemJobInfo::IsCompleted::
get(
    void
    )
{
    VerifyAccess();

    return isCompleted;
}

Boolean
PrintSystemJobInfo::IsDeleting::
get(
    void
    )
{
    VerifyAccess();

    return isDeleting;
}

Boolean
PrintSystemJobInfo::IsPaused::
get(
    void
    )
{
    VerifyAccess();

    return isPaused;
}

Boolean
PrintSystemJobInfo::IsPrinted::
get(
    void
    )
{
    VerifyAccess();

    return isPrinted;
}

Boolean
PrintSystemJobInfo::IsRestarted::
get(
    void
    )
{
    VerifyAccess();

    return isRestarted;
}

Boolean
PrintSystemJobInfo::IsSpooling::
get(
    void
    )
{
    VerifyAccess();

    return isSpooling;
}

Boolean
PrintSystemJobInfo::IsInError::
get(
    void
    )
{
    VerifyAccess();

    return isInError;
}

Boolean
PrintSystemJobInfo::IsPrinting::
get(
    void
    )
{
    VerifyAccess();

    return isPrinting;
}

Boolean
PrintSystemJobInfo::IsOffline::
get(
    void
    )
{
    VerifyAccess();

    return isOffline;
}

Boolean
PrintSystemJobInfo::IsPaperOut::
get(
    void
    )
{
    VerifyAccess();

    return isPaperOut;
}

Boolean
PrintSystemJobInfo::IsDeleted::
get(
    void
    )
{
    VerifyAccess();

    return isDeleted;
}

Boolean
PrintSystemJobInfo::IsBlocked::
get(
    void
    )
{
    VerifyAccess();

    return isBlocked;
}

Boolean
PrintSystemJobInfo::IsUserInterventionRequired::
get(
    void
    )
{
    VerifyAccess();

    return isUserInterventionRequired;
}

Boolean
PrintSystemJobInfo::IsRetained::
get(
    void
    )
{
    VerifyAccess();

    return isRetained;
}

String^
PrintSystemJobInfo::JobName::
get(
    void
    )
{
    VerifyAccess();

    return jobName;
}

void
PrintSystemJobInfo::JobName::
set(
    String^ newJobName
    )
{
    VerifyAccess();

    jobName = newJobName;
}

PrintQueue^
PrintSystemJobInfo::HostingPrintQueue::
get(
    void
    )
{
    VerifyAccess();

    return hostingPrintQueue;
}

PrintServer^
PrintSystemJobInfo::HostingPrintServer::
get(
    void
    )
{
    VerifyAccess();

    return hostingPrintQueue->HostingPrintServer;
}

void
PrintSystemJobInfo::HostingPrintQueue::
set(
    PrintQueue^ printQueue
    )
{
    VerifyAccess();
}

void
PrintSystemJobInfo::HostingPrintServer::
set(
    PrintServer^ printServer
    )
{
    VerifyAccess();
}

Boolean
PrintSystemJobInfo::DownLevelSystem::
get(
    )
{
    return isDownLevelSystem;
}

void
PrintSystemJobInfo::DownLevelSystem::
set(
    Boolean         value
    )
{
    isDownLevelSystem = value;
}

void
PrintSystemJobInfo::
Commit(
    void
    )
{
    VerifyAccess();

    throw gcnew NotSupportedException;

}

void
PrintSystemJobInfo::
Refresh(
    void
    )
{
    VerifyAccess();

    try
    {
        PopulateJobProperties(refreshPropertiesFilter);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        if (IsErrorInvalidParameter(internalException->HResult))
        {
            get_InternalPropertiesCollection("Status")->GetProperty("Status")->IsInternallyInitialized = true;
            this->JobStatusSecondary = static_cast<Int32>(PrintJobStatus::Deleted);
        }
        else
        {
            throw CreatePrintJobException(internalException->HResult,
                                          "PrintSystemException.PrintSystemJobInfo.Refresh");
        }
    }
}

__declspec(noinline)
bool
PrintSystemJobInfo::
IsErrorInvalidParameter(
    int hResult
    )
{
    return PrinterHResult::HResultCode(hResult) == ERROR_INVALID_PARAMETER;
}


PrintProperty^
PrintSystemJobInfo::
CreateAttributeNoValue(
    String^ attributeName
    )
{
    Type^ type = (Type^)PrintSystemJobInfo::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName);
}

PrintProperty^
PrintSystemJobInfo::
CreateAttributeValue(
    String^ attributeName,
    Object^ attributeValue
    )
{
    Type^ type = (Type^)PrintSystemJobInfo::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue);
}

PrintProperty^
PrintSystemJobInfo::
CreateAttributeNoValueLinked(
    String^             attributeName,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintSystemJobInfo::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,delegate);
}

PrintProperty^
PrintSystemJobInfo::
CreateAttributeValueLinked(
    String^             attributeName,
    Object^             attributeValue,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintSystemJobInfo::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue,delegate);
}

void
PrintSystemJobInfo::
RegisterAttributesNamesTypes(
    void
    )
{
    //
    // Register the attributes of the base class first
    //
    PrintSystemObject::RegisterAttributesNamesTypes(PrintSystemJobInfo::attributeNameTypes);
    //
    // Register the attributes of the current class
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemJobInfo::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintSystemJobInfo::primaryAttributeNames[numOfAttributes],
                                PrintSystemJobInfo::primaryAttributeTypes[numOfAttributes]);
    }

    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemJobInfo::secondaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintSystemJobInfo::secondaryAttributeNames[numOfAttributes],
                                PrintSystemJobInfo::secondaryAttributeTypes[numOfAttributes]);
    }
}

array<String^>^
PrintSystemJobInfo::
GetAllPropertiesFilter(
    void
    )
{
    //
    // Properties = Base Class Properties + Inherited Class Properties
    //
    array<String^>^ allPropertiesFilter = gcnew array<String^>(PrintSystemObject::BaseAttributeNames()->Length +
                                                               PrintSystemJobInfo::primaryAttributeNames->Length);

    //
    // First Add the Base Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemObject::BaseAttributeNames()->Length;
        numOfAttributes++)
    {
        allPropertiesFilter[numOfAttributes] = PrintSystemObject::BaseAttributeNames()[numOfAttributes];
    }

    //
    // Then Add the Inherited Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemJobInfo::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        String^ upLevelAttribute = nullptr;

        if(String^ downLevelAttribute = (String^)upLevelToDownLevelMapping[upLevelAttribute = PrintSystemJobInfo::primaryAttributeNames[numOfAttributes]])
        {
            allPropertiesFilter[PrintSystemObject::BaseAttributeNames()->Length + numOfAttributes] = downLevelAttribute;
        }
        else
        {
            allPropertiesFilter[PrintSystemObject::BaseAttributeNames()->Length + numOfAttributes] = upLevelAttribute;
        }
    }

    return allPropertiesFilter;
}

/*++
    Function Name:
        Instantiate

    Description:
        Due to the way the APIs are implemented and to apply
        generic patterns to some of the methods instantiated
        and to make it easier in applying single patterns on
        simillar paradiagms, I used Factories in some internal
        instantiation models. This method is the one called by
        such factories to intantiate an instance of the
        PrintSystemJobInfo.

    Parameters:
        PrintQueue:         Print Queue that is hosting the Print Job
        PropertiesFilter:   The set of properties required to be visible on
                            that Print Job

    Return Value
        PrintSystemObject:  An instance of a PrintQueue
--*/
PrintSystemObject^
PrintSystemJobInfo::
Instantiate(
    Object^             printQueue,
    array<String^>^     propertiesFilter
    )
{
    return gcnew PrintSystemJobInfo((PrintQueue^)printQueue,
                                    propertiesFilter);
}

void
PrintSystemJobInfo::
InitializeInternalCollections(
    void
    )
{
    collectionsTable          = gcnew Hashtable();
    thunkPropertiesCollection = gcnew PrintPropertyDictionary();

    //
    // Add the attributes from the base class to the appropriate collection
    //
    for(Int32 numOfBaseAttributes=0;
        numOfBaseAttributes < PrintSystemObject::BaseAttributeNames()->Length;
        numOfBaseAttributes++)
    {
        collectionsTable->Add(PrintSystemObject::BaseAttributeNames()[numOfBaseAttributes],PropertiesCollection);
    }

    //
    // Override the set_Name property in the base class
    //
    ((PrintStringProperty^)PropertiesCollection->GetProperty("Name"))->
    ChangeHandler = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintSystemJobInfo::Name::set);

    array<MulticastDelegate^>^ propertiesDelegates = CreatePropertiesDelegates();

    //
    // Perparing the primary (purely managed) attributes
    //
    Int32 numOfPrimaryAttributes=0;

    for(numOfPrimaryAttributes=0;
        numOfPrimaryAttributes < PrintSystemJobInfo::primaryAttributeNames->Length;
        numOfPrimaryAttributes++)
    {
        PrintProperty^ printSystemAttributeValue = nullptr;

        printSystemAttributeValue =
        ObjectsAttributesValuesFactory::Value->Create(this->GetType(),
                                                      primaryAttributeNames[numOfPrimaryAttributes],
                                                      propertiesDelegates[numOfPrimaryAttributes]);

        PrintSystemObject::PropertiesCollection->Add(printSystemAttributeValue);
        //
        // The following links an attribute name to a collection
        //
        collectionsTable->Add(PrintSystemJobInfo::primaryAttributeNames[numOfPrimaryAttributes],PropertiesCollection);
    }

    //
    // Perparing the secondary (used for downlevel -unamanged- thunking) attributes
    //
    for(Int32 numOfSecondaryAttributes=0;
        numOfSecondaryAttributes < PrintSystemJobInfo::secondaryAttributeNames->Length;
        numOfSecondaryAttributes++)
    {
        PrintProperty^ printSystemAttributeValue = nullptr;

        printSystemAttributeValue =
        ObjectsAttributesValuesFactory::Value->Create(this->GetType(),
                                                      secondaryAttributeNames[numOfSecondaryAttributes],
                                                      propertiesDelegates[numOfPrimaryAttributes + numOfSecondaryAttributes]);

        thunkPropertiesCollection->Add(printSystemAttributeValue);
        //
        // The following links an attribute name to a collection
        //
        collectionsTable->Add(PrintSystemJobInfo::secondaryAttributeNames[numOfSecondaryAttributes],thunkPropertiesCollection);
    }

}

void
PrintSystemJobInfo::
Initialize(
    void
    )
{
    accessVerifier = gcnew PrintSystemDispatcherObject();

    InitializeInternalCollections();

    PropertiesCollection->GetProperty("Name")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("Name")->Value                   = jobName;
    //
    // Gather out the name of all the properties within the object
    //
    refreshPropertiesFilter = PrintSystemJobInfo::GetAllPropertiesFilter();
}

array<MulticastDelegate^>^
PrintSystemJobInfo::
CreatePropertiesDelegates(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = gcnew array<MulticastDelegate^>(primaryAttributeNames->Length +
                                                                                     secondaryAttributeNames->Length);

    propertiesDelegates[0] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::JobIdentifier::set);
    //propertiesDelegates[1] = nullptr;
    propertiesDelegates[1] = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintSystemJobInfo::Submitter::set);
    propertiesDelegates[2] = nullptr;
    propertiesDelegates[3] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::PositionInPrintQueue::set);
    propertiesDelegates[4] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::StartTimeOfDay::set);
    propertiesDelegates[5] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::UntilTimeOfDay::set);
    propertiesDelegates[6] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::NumberOfPages::set);
    propertiesDelegates[7] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::NumberOfPagesPrinted::set);
    propertiesDelegates[8] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::JobSize::set);
    propertiesDelegates[9] = gcnew PrintSystemDelegates::SystemDateTimeValueChanged(this,&PrintSystemJobInfo::TimeJobSubmitted::set);
    propertiesDelegates[10] = nullptr;//gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::set_TimeSinceStartedPrinting);
    propertiesDelegates[11] = nullptr;
    propertiesDelegates[12] = nullptr;
    propertiesDelegates[13] = nullptr;

    //
    // Secondary
    //
    propertiesDelegates[14] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::PrioritySecondary::set);
    propertiesDelegates[15] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintSystemJobInfo::JobStatusSecondary::set);
    propertiesDelegates[16] = nullptr;
    propertiesDelegates[17] = nullptr;

    return propertiesDelegates;
}


PrintPropertyDictionary^
PrintSystemJobInfo::
get_InternalPropertiesCollection(
    String^ attributeName
    )
{
    return (PrintPropertyDictionary^)collectionsTable[attributeName];
}

void
PrintSystemJobInfo::
CopyFileStreamToPrinter(
    String^  xpsFileName,
    Stream^  printQueueStream
    )
{

    array<Byte>^ buffer        = gcnew array<Byte>(0x10000);
    FileStream^  xpsFileStream = gcnew FileStream(xpsFileName, FileMode::Open, FileAccess::Read, FileShare::Read);
    int          bufferSize    = 0;

    try
    {
        do
        {
            bufferSize = xpsFileStream->Read(buffer, 0, 0x10000);
            printQueueStream->Write(buffer, 0, bufferSize);
        }
        while (bufferSize > 0);
    }
    __finally
    {
        delete xpsFileStream;
    }

}

/*++
    Function Name:
        PrintSystemJobInfo

    Description:
        Constructor of class instance for a browsable PrintJob
        used during enumerations

    Parameters:
        printQueue:         Hosting Print Queue.
        propertiesFilter:   Properties we want to see for this Job.

    Return Value
        None
--*/
PrintSystemJobInfo::
PrintSystemJobInfo(
    PrintQueue^         printQueue,
    array<String^>^     propertiesFilter
    ):
    hostingPrintQueue(printQueue),
    refreshPropertiesFilter(nullptr),
    jobName(defaultJobName),
    accessVerifier(nullptr)
{
    if (!hostingPrintQueue)
    {
        throw CreatePrintJobException("PrintSystemException.PrintSystemJobInfo.Create",
                                      gcnew ArgumentNullException("printQueue"));
    }

    Initialize();

    refreshPropertiesFilter = propertiesFilter;
}

void
PrintSystemJobInfo::
VerifyAccess(
    void
    )
{
    if(accessVerifier==nullptr)
    {
        accessVerifier = gcnew PrintSystemDispatcherObject();
    }

    accessVerifier->VerifyThreadLocality();
}

__declspec(noinline)
Exception^
PrintSystemJobInfo::CreatePrintJobException(
    String^ messageId
    )
{
    return gcnew PrintJobException(messageId);
}

__declspec(noinline)
Exception^
PrintSystemJobInfo::CreatePrintJobException(
    int hresult,
    String^ messageId
    )
{
    return gcnew PrintJobException(hresult, messageId);
}

__declspec(noinline)
Exception^
PrintSystemJobInfo::CreatePrintJobException(
    String^ messageId,
    Exception^ innerException
    )
{
    return gcnew PrintJobException(messageId, innerException);
}

//////////////////////////////////////////////////////////////////////////////////////////////////////
PrintJobInfoCollection::
PrintJobInfoCollection(
    PrintQueue^     printQueue,
    array<String^>^ propertyFilter
) : hostingPrintQueue(printQueue)
{
    jobInfoCollection = gcnew System::Collections::Generic::Queue<PrintSystemJobInfo^>();

    EnumDataThunkObject^ enumDataThunkObject = nullptr;

    accessVerifier = gcnew PrintSystemDispatcherObject();

    try
    {
        enumDataThunkObject = gcnew EnumDataThunkObject(System::Printing::PrintSystemJobInfo::typeid);

        enumDataThunkObject->GetPrintSystemValuesPerPrintJobs(printQueue,
                                                              jobInfoCollection,
                                                              propertyFilter,
                                                              0,
                                                              printQueue->NumberOfJobs);
    }
    __finally
    {
        delete enumDataThunkObject;
    }
}


PrintJobInfoCollection::
~PrintJobInfoCollection(
    )
{
    VerifyAccess();

    jobInfoCollection = nullptr;
}

void
PrintJobInfoCollection::
Add(
    PrintSystemJobInfo^ jobInfo
    )
{
    VerifyAccess();
    jobInfoCollection->Enqueue(jobInfo);
}

System::
Collections::
Generic::
IEnumerator<PrintSystemJobInfo ^>^
PrintJobInfoCollection::
GetEnumerator(
    void
    )
{
    VerifyAccess();

    return jobInfoCollection->GetEnumerator();
}


System::
Collections::
IEnumerator^
PrintJobInfoCollection::
GetNonGenericEnumerator(
    void
    )
{
    VerifyAccess();

    return jobInfoCollection->GetEnumerator();
}

void
PrintJobInfoCollection::
VerifyAccess(
    void
    )
{
    if(accessVerifier==nullptr)
    {
        accessVerifier = gcnew PrintSystemDispatcherObject();
    }

    accessVerifier->VerifyThreadLocality();

}
