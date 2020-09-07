// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        PrintServer object implementation.
--*/
#include "win32inc.hpp"

#define HRESULT LONG

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;
using namespace System::Collections::ObjectModel;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Text;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif


#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif

using namespace System::Printing;

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __OBJECTSATTRIBUTESVALUESFACTORY_HPP__
#include <ObjectsAttributesValuesFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif


#ifndef  __GETDATATHUNKOBJECT_HPP__
#include <GetDataThunkObject.hpp>
#endif

#ifndef  __SETDATATHUNKOBJECT_HPP__
#include <SetDataThunkObject.hpp>
#endif


using namespace MS::Internal;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForPrintQueue;

using namespace System::Printing::Activation;

/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor. It binds the object to the print server.

Arguments:

    None

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    void
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(true),
accessVerifier(nullptr)
{
    Initialize(nullptr, (array<String^>^)(nullptr), nullptr);
}

/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor.

Arguments:

    path - print server path identifier. nullptr for local print server.
           Could be in any format recognized by a print provider on the
           print server.

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^     path
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(true),
accessVerifier(nullptr)
{
    Initialize(path, (array<String^>^)(nullptr), nullptr);
}

/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor

Arguments:

    path    - print server path identifier. nullptr for local print server.
              Could be in any format recognized by a print provider on the
              print server.
    type    - ignored for now. This is the constructor used when enumerating the
              PrintQueues.

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^         path,
    PrintServerType type
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(true),
accessVerifier(nullptr)
{
    InitializeInternalCollections();

    this->IsInternallyInitialized = true;
    this->Name = path ? path : PrinterThunkHandler::GetLocalMachineName();
}


/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor

Arguments:

    path                - print server path identifier. nullptr for local print server.
                          Could be in any format recognized by a print provider on the
                          print server.
    propertiesFilter    - server properties to be initialized while building the object.

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^                             path,
    array<PrintServerIndexedProperty>^  propertiesFilter
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(false),
accessVerifier(nullptr)
{
    Initialize(path, PrintServer::ConvertPropertyFilterToString(propertiesFilter), nullptr);
}


/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor

Arguments:

    path                - print server path identifier. nullptr for local print server.
                          Could be in any format recognized by a print provider on the
                          print server.
    propertiesFilter    - server properties to be initialized while building the object.

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^             path,
    array<String^>^     propertiesFilter
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(false),
accessVerifier(nullptr)
{
    Initialize(path, propertiesFilter, nullptr);
}


/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor. Binds the object to the local print server and populates
    all properties.

Arguments:

    desiredAccess   -   requested permissions

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    PrintSystemDesiredAccess    desiredAccess
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(true),
accessVerifier(nullptr)
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(nullptr, (array<String^>^)(nullptr), printerDefaults);
}


/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor. Binds the object to the local print server and populates all
    properties.

Arguments:

    path            - print server path identifier. nullptr for local print server.
                      Could be in any format recognized by a print provider on the
                      print server.
    desiredAccess   - requested permissions

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^                     path,
    PrintSystemDesiredAccess    desiredAccess
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(true),
accessVerifier(nullptr)
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(path, (array<String^>^)(nullptr), printerDefaults);
}


/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor. Binds the object to the local print server.

Arguments:

    path             - print server path identifier. nullptr for local print server.
                       Could be in any format recognized by a print provider on the
                       print server.
    propertiesFilter - server properties to be initialized while building the object.
    desiredAccess    - requested permissions

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^                            path,
    array<PrintServerIndexedProperty>^ propertiesFilter,
    PrintSystemDesiredAccess           desiredAccess
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(false),
accessVerifier(nullptr)
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(path, PrintServer::ConvertPropertyFilterToString(propertiesFilter), printerDefaults);
}

/*++

Routine Name:

    PrintServer

Routine Description:

    Constructor. Binds the object to the local print server.

Arguments:

    path             - print server path identifier. nullptr for local print server.
                       Could be in any format recognized by a print provider on the
                       print server.
    propertiesFilter - server properties to be initialized while building the object.
    desiredAccess    - requested permissions

Return Value:

    N\A

--*/
PrintServer::
PrintServer(
    String^                     path,
    array<String^>^             propertiesFilter,
    PrintSystemDesiredAccess    desiredAccess
    ):
serverThunkHandler(nullptr),
refreshPropertiesFilter(nullptr),
isDelayInitialized(false),
accessVerifier(nullptr)
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(path, propertiesFilter, printerDefaults);
}

void
PrintServer::
Initialize(
    String^                     path,
    array<String^>^             propertiesFilter,
    PrinterDefaults^            printerDefaults
    )
{
    bool disposeServerHandle = false;

    accessVerifier = gcnew PrintSystemDispatcherObject();
   
    InitializeInternalCollections();

    try
    {
        //
        // Validate the path. If invalid, PrinterThunkHandler will throw.
        //
        serverThunkHandler = gcnew PrintWin32Thunk::PrinterThunkHandler(path, printerDefaults);

        if (serverThunkHandler)
        {
            this->IsInternallyInitialized = true;

            this->Name = path ? path : PrinterThunkHandler::GetLocalMachineName();

            refreshPropertiesFilter = PrintServer::GetAllPropertiesFilter(propertiesFilter);

            if (!isDelayInitialized)
            {
                GetUnInitializedData(refreshPropertiesFilter);
            }
        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        disposeServerHandle = true;

        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
    __finally
    {
        if (disposeServerHandle &&
            serverThunkHandler)
        {
            delete serverThunkHandler;
            serverThunkHandler = nullptr;
        }
    }
}

/*++

Routine Name:

    InitializeInternalCollections

Routine Description:

    It initializes the object's internal collections and tables.
    It builds the attribute value collection by creating PrintProperty
    corresponding to the base object properties and adding them to the collection
    and also creates the PrintProperty objects for this object's properties
    PrintSErver object doesn't have secondary attributes.

Arguments:

    N/A

Return Value:

    N\A

--*/
void
PrintServer::
InitializeInternalCollections(
    void
    )
{
    collectionsTable          = gcnew Hashtable();

    //
    // Add the attributes from the base class to the appropriate collection
    //
    for(Int32 numOfBaseAttributes=0;
        numOfBaseAttributes < PrintSystemObject::BaseAttributeNames()->Length;
        numOfBaseAttributes++)
    {
        collectionsTable->Add(PrintSystemObject::BaseAttributeNames()[numOfBaseAttributes],PropertiesCollection);
    }

    array<MulticastDelegate^>^ propertiesDelegates = CreatePropertiesDelegates();

    for(Int32 numOfPrimaryAttributes=0;
        numOfPrimaryAttributes < PrintServer::primaryAttributeNames->Length;
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
        collectionsTable->Add(PrintServer::primaryAttributeNames[numOfPrimaryAttributes],PropertiesCollection);
    }
}


/*++

Routine Name:

    InstallPrintQueue

Routine Description:

    Installs a print queue on the print server represented by the current object.

Arguments:

    printQueueName          - print queue name
    driverName              - driver name
    portNames[]             - array of port names
    printProcessorName      - print processor name
    printQueueAttributes    - attributes

Return Value:

    PrintQueue object representing the just installed printer.

--*/
PrintQueue^
PrintServer::
InstallPrintQueue(
    String^                     printQueueName,
    String^                     driverName,
    array<String^>^             portNames,
    String^                     printProcessorName,
    PrintQueueAttributes        printQueueAttributes
    )
{
    VerifyAccess();

    return PrintQueue::Install(this,
                               printQueueName,
                               driverName,
                               portNames,
                               printProcessorName,
                               printQueueAttributes);
}

/*++

Routine Name:

    InstallPrintQueue

Routine Description:

    Installs a print queue on the print server represented by the current object.

Arguments:

    printQueueName              - print queue name
    driverName                  - driver name
    portNames[]                 - array of port names
    printProcessorName          - print processor name
    printQueueAttributes        - attributes
    requiredPrintQueueProperty  - either comment, sharename or location
    requiredPriority            - print queue priority
    requiredDefaultPriority     - print queue default priority

Return Value:

    PrintQueue object representing the just installed printer.

--*/
PrintQueue^
PrintServer::
InstallPrintQueue(
    String^                      printQueueName,
    String^                      driverName,
    array<String^>^              portNames,
    String^                      printProcessorName,
    PrintQueueAttributes         printQueueAttributes,
    PrintQueueStringProperty^    requiredPrintQueueProperty,
    Int32                        requiredPriority,
    Int32                        requiredDefaultPriority
    )
{
    VerifyAccess();

    return PrintQueue::Install(this,
                               printQueueName,
                               driverName,
                               portNames,
                               printProcessorName,
                               printQueueAttributes,
                               requiredPrintQueueProperty,
                               requiredPriority,
                               requiredDefaultPriority);
}

/*++

Routine Name:

    InstallPrintQueue

Routine Description:

    Installs a print queue on the print server represented by the current object.

Arguments:

    printQueueName              - print queue name
    driverName                  - driver name
    portNames[]                 - array of port names
    printProcessorName          - print processor name
    printQueueAttributes        - attributes
    requiredShareName           - share name
    requiredComment             - comment
    requiredLocation            - location
    requiredSepFile             - separator file
    requiredPriority            - print queue priority
    requiredDefaultPriority     - print queue default priority

Return Value:

    PrintQueue object representing the just installed printer.

--*/
PrintQueue^
PrintServer::
InstallPrintQueue(
    String^                      printQueueName,
    String^                      driverName,
    array<String^>^              portNames,
    String^                      printProcessorName,
    PrintQueueAttributes         printQueueAttributes,
    String^                      requiredShareName,
    String^                      requiredComment,
    String^                      requiredLocation,
    String^                      requiredSeparatorFile,
    Int32                        requiredPriority,
    Int32                        requiredDefaultPriority
    )
{
    VerifyAccess();

    return PrintQueue::Install(this,
                               printQueueName,
                               driverName,
                               portNames,
                               printProcessorName,
                               printQueueAttributes,
                               requiredShareName,
                               requiredComment,
                               requiredLocation,
                               requiredSeparatorFile,
                               requiredPriority,
                               requiredDefaultPriority);
}

/*++

Routine Name:

    InstallPrintQueue

Routine Description:

    Installs a print queue on the print server represented by the current object.

Arguments:

    printQueueName              - print queue name
    driverName                  - driver name
    portNames[]                 - array of port names
    initParams                  - attribute value collection that specifies
                                  the rest of the properties


Return Value:

    PrintQueue object representing the just installed printer.

--*/
PrintQueue^
PrintServer::
InstallPrintQueue(
    String^                                 printQueueName,
    String^                                 driverName,
    array<String^>^                         portNames,
    String^                                 printProcessorName,
    PrintPropertyDictionary^                initParams
    )
{
    VerifyAccess();

    return PrintQueue::Install(this,
                               printQueueName,
                               driverName,
                               portNames,
                               printProcessorName,
                               initParams);
}

/*++

Routine Name:

    DeletePrintQueue

Routine Description:

    Deletes a print queue on the print server represented by the current object.

Arguments:

    printQueueName  - name of the print queue to be deleted

Return Value:

    true if succeeded

--*/
bool
PrintServer::
DeletePrintQueue(
    String^     printQueueName
    )
{
    return PrintQueue::Delete(printQueueName);
}


/*++
    Routine Name:

        DeletePrintQueue

    Routine Description:

        Deletes a print queue on the print server represented by the current object.

    Arguments:

        printQueueName  - name of the print queue to be deleted

    Return Value:

        true if succeeded

--*/
bool
PrintServer::
DeletePrintQueue(
    PrintQueue^     printQueue
    )
{
    return DeletePrintQueue(printQueue->FullName);
}

/*++

Routine Name:

    GetPrintQueue

Routine Description:

    Instantiates the PrintQueue object associated with the given printer name.
    All PrintQueue's object properties will be initialized.

Arguments:

    printQueueName  - name of the print queue

Return Value:

    PrintQueue object representing the print queue specified by printQueueName

--*/
PrintQueue^
PrintServer::
GetPrintQueue(
    String^ printQueueName
    )
{
    VerifyAccess();

    return gcnew PrintQueue(this, printQueueName);
}

/*++

Routine Name:

    GetPrintQueue

Routine Description:

    Instantiates the PrintQueue object associated with the given printer name.
    All PrintQueue's object properties will be initialized.

Arguments:

    printQueueName      - name of the print queue
    propertiesFilter    - array of strings that represent the names of the
                          properties to be initialized when the PrintQueue object is created.

Return Value:

    PrintQueue object representing the print queue specified by printQueueName

--*/
PrintQueue^
PrintServer::
GetPrintQueue(
    String^         printQueueName,
    array<String^>^ propertiesFilter
    )
{
    VerifyAccess();

    return gcnew PrintQueue(this,
                            printQueueName,
                            propertiesFilter);
}

/*++

Routine Name:

    GetPrintQueues

Routine Description:

    Instantiates the PrintQueueCollection object that holds the PrintQueue objects
    installed on the print server represented by this object. All properties of
    the PrintQueue objectswill be initialized.

Arguments:

    none

Return Value:

    PrintQueueCollection object representing the print queues on the print server indentified by this object.

--*/
PrintQueueCollection^
PrintServer::
GetPrintQueues(
    void
    )
{
    VerifyAccess();

    return gcnew PrintQueueCollection(this,PrintQueue::GetAllPropertiesFilter());
}

/*++

Routine Name:

    GetPrintQueues

Routine Description:

    Instantiates the PrintQueueCollection object that holds the PrintQueue objects
    installed on the print server represented by this object. All properties of
    the PrintQueue objectswill be initialized.

Arguments:

    enumerationFlag - enumeration flags

Return Value:

    PrintQueueCollection object representing the print queues on the print server indentified by this object.

--*/
PrintQueueCollection^
PrintServer::
GetPrintQueues(
    array<EnumeratedPrintQueueTypes>^    enumerationFlag
    )
{
    VerifyAccess();

    return gcnew PrintQueueCollection(this, PrintQueue::GetAllPropertiesFilter(), enumerationFlag);
}

/*++

Routine Name:

    GetPrintQueues

Routine Description:

    Instantiates the PrintQueueCollection object that holds the PrintQueue objects
    installed on the print server represented by this object. All properties of
    the PrintQueue objectswill be initialized.

Arguments:

    propertiesFilter    - array of strings that represent the names of the
                          properties to be initialized when the PrintQueue object is created.

    enumerationFlag     - enumeration flags

Return Value:

    PrintQueueCollection object representing the print queues on the print server indentified by this object.

--*/
PrintQueueCollection^
PrintServer::
GetPrintQueues(
    array<String^>^                      propertiesFilter,
    array<EnumeratedPrintQueueTypes>^    enumerationFlag
    )
{
    VerifyAccess();

    return gcnew PrintQueueCollection(this, propertiesFilter, enumerationFlag);
}

/*++

Routine Name:

    GetPrintQueues

Routine Description:

    Instantiates the PrintQueueCollection object that holds the PrintQueue objects
    installed on the print server represented by this object.

Arguments:

    propertiesFilter    - array of strings that represent the names of the
                          properties to be initialized when the PrintQueue object is created.

Return Value:

    PrintQueueCollection object representing the print queues on the print server indentified by this object.

--*/
PrintQueueCollection^
PrintServer::
GetPrintQueues(
    array<String^>^     propertiesFilter
    )
{
    VerifyAccess();

    return gcnew PrintQueueCollection(this,propertiesFilter);
}

/*++

Routine Name:

    GetPrintQueues

Routine Description:

    Instantiates the PrintQueueCollection object that holds the PrintQueue objects
    installed on the print server represented by this object.

Arguments:

    propertiesFilter    - array of strings that represent the names of the
                          properties to be initialized when the PrintQueue object is created.
    enumerationFlag     - enumeration flags

Return Value:

    PrintQueueCollection object representing the print queues on the print server indentified by this object.

--*/
PrintQueueCollection^
PrintServer::
GetPrintQueues(
    array<PrintQueueIndexedProperty>^              propertiesFilter,
    array<EnumeratedPrintQueueTypes>^              enumerationFlag
    )
{
    VerifyAccess();

    array<String^>^ propertiesFilterAsStrings = PrintQueue::ConvertPropertyFilterToString(propertiesFilter);

    return gcnew PrintQueueCollection(this, propertiesFilterAsStrings, enumerationFlag);
}

/*++

Routine Name:

    GetPrintQueues

Routine Description:

    Instantiates the PrintQueueCollection object that holds the PrintQueue objects
    installed on the print server represented by this object.

Arguments:

    propertiesFilter    - array of strings that represent the names of the
                          properties to be initialized when the PrintQueue object is created.

Return Value:

    PrintQueueCollection object representing the print queues on the print server indentified by this object.

--*/
PrintQueueCollection^
PrintServer::
GetPrintQueues(
    array<PrintQueueIndexedProperty>^     propertiesFilter
    )
{
    //
    // Convert the PropertyFilters to the corresponding Strings
    // We have to delegate the conversion to the PrintQueue and
    // the reason for that is:
    // In the old spooler we have names and not objects and so
    // for e.g. the PrintServer is represented by its name and
    // not an object and so when we come in with a required
    // HostingPrintServer Property, we delegate it to be converted
    // to a HostingPrintServerName Property and since those are all
    // properties of the PritnQueue, it is the best fit object to
    // hostto doing the conversions.
    //
    VerifyAccess();

    array<String^>^ propertiesFilterAsStrings = PrintQueue::ConvertPropertyFilterToString(propertiesFilter);

    return gcnew PrintQueueCollection(this,propertiesFilterAsStrings);
}

/*++

Routine Name:

    Commit

Routine Description:

    Commits the dirty attributes to server.

Arguments:

    None

Return Value:

    void

--*/
void
PrintServer::
Commit(
    void
    )
{
    VerifyAccess();

    try
    {
        ComitDirtyData(GetAlteredPropertiesFilter());
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Commit");
    }
}

/*++

Routine Name:

    Refresh

Routine Description:

    Refreshes the object attributes.

Arguments:

    None

Return Value:

    void

--*/
void
PrintServer::
Refresh(
    void
    )
{
    VerifyAccess();

    try
    {
        for(int numOfProperties = 0;
        numOfProperties < refreshPropertiesFilter->Length;
        numOfProperties++)
        {
            GetDataFromServer(refreshPropertiesFilter[numOfProperties], true);
        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Refresh");
    }
}

/*++

Routine Name:

    InternalDispose

Routine Description:

    Internal Dispose method.

Arguments:

    None

Return Value:

    void

--*/
void
PrintServer::
InternalDispose(
    bool    disposing
)
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(this);
        {
            __try
            {
                if (!IsDisposed)
                {
                    if (disposing)
                    {
                        if (serverThunkHandler)
                        {
                            delete serverThunkHandler;
                            serverThunkHandler = nullptr;
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

/*++

Routine Name:

    get_DefaultSpoolDirectory

Routine Description:

    Property

Arguments:

    None

Return Value:

    String representing the default spool directory for the
    print server represented by this object.

--*/
String^
PrintServer::DefaultSpoolDirectory::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("DefaultSpoolDirectory", false);
        }
    }
    else
    {
        GetDataFromServer("DefaultSpoolDirectory", false);
    }

    return defaultSpoolDirectory;
}

/*++

Routine Name:

    set_DefaultSpoolDirectory

Routine Description:

    Property

Arguments:

    requiredDefaultSpoolDirectory - string representing the default spool directory
                                    for the print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::DefaultSpoolDirectory::
set(
    String^   requiredDefaultSpoolDirectory
    )
{
    VerifyAccess();

    if(defaultSpoolDirectory != requiredDefaultSpoolDirectory ||
       (requiredDefaultSpoolDirectory &&
        !requiredDefaultSpoolDirectory->Equals(defaultSpoolDirectory)))
    {
        defaultSpoolDirectory = requiredDefaultSpoolDirectory;

        PropertiesCollection->GetProperty("DefaultSpoolDirectory")->Value = defaultSpoolDirectory;
    }
}

/*++

Routine Name:

    get_PortThreadPriority

Routine Description:

    Property

Arguments:

    None

Return Value:

    Port thread priority for the
    print server represented by this object.

--*/
System::Threading::ThreadPriority
PrintServer::PortThreadPriority::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("PortThreadPriority", false);
        }
    }
    else
    {
        GetDataFromServer("PortThreadPriority", false);
    }

    return portThreadPriority;
}

/*++

Routine Name:

    set_PortThreadPriority

Routine Description:

    Property

Arguments:

    requiredPortThreadPriority - port thread priority for the
                                 print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::PortThreadPriority::
set(
    System::Threading::ThreadPriority requiredPortThreadPriority
    )
{
    VerifyAccess();

    if (portThreadPriority != requiredPortThreadPriority)
    {
        portThreadPriority = requiredPortThreadPriority;

        PropertiesCollection->GetProperty("PortThreadPriority")->Value = portThreadPriority;
    }
}

/*++

Routine Name:

    get_DefaultPortThreadPriority

Routine Description:

    Property

Arguments:

    None

Return Value:

    Default port thread priority for the
    print server represented by this object.

--*/
System::Threading::ThreadPriority
PrintServer::DefaultPortThreadPriority::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("DefaultPortThreadPriority", false);
        }
    }
    else
    {
        GetDataFromServer("DefaultPortThreadPriority", false);
    }

    return defaultPortThreadPriority;
}

/*++

Routine Name:

    set_DefaultPortThreadPriority

Routine Description:

    Property

Arguments:

    requiredPortThreadPriority - default port thread priority for the
                                 print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::DefaultPortThreadPriority::
set(
    System::Threading::ThreadPriority  requiredDefaultPortThreadPriority
    )
{
    VerifyAccess();

    if (defaultPortThreadPriority != requiredDefaultPortThreadPriority)
    {
        defaultPortThreadPriority = requiredDefaultPortThreadPriority;

        PropertiesCollection->GetProperty("DefaultPortThreadPriority")->Value = defaultPortThreadPriority;
    }
}

/*++

Routine Name:

    get_SchedulerPriority

Routine Description:

    Property

Arguments:

    None

Return Value:

    The Scheduler thread priority for the
    print server represented by this object.

--*/
System::Threading::ThreadPriority
PrintServer::SchedulerPriority::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("SchedulerPriority", false);
        }
    }
    else
    {
        GetDataFromServer("SchedulerPriority", false);
    }

    return schedulerPriority;
}

/*++

Routine Name:

    set_SchedulerPriority

Routine Description:

    Property

Arguments:

    requiredSchedulerPriority - scheduler thread priority for the
                                print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::SchedulerPriority::
set(
    System::Threading::ThreadPriority  requiredSchedulerPriority
    )
{
    VerifyAccess();

    if (schedulerPriority != requiredSchedulerPriority)
    {
        schedulerPriority = requiredSchedulerPriority;

        PropertiesCollection->GetProperty("SchedulerPriority")->Value = schedulerPriority;
    }
}

/*++

Routine Name:

    get_DefaultSchedulerPriority

Routine Description:

    Property

Arguments:

    None

Return Value:

    The default scheduler thread priority for the
    print server represented by this object.

--*/
System::Threading::ThreadPriority
PrintServer::DefaultSchedulerPriority::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("DefaultSchedulerPriority", false);
        }
    }
    else
    {
        GetDataFromServer("DefaultSchedulerPriority", false);
    }

    return defaultSchedulerPriority;
}

/*++

Routine Name:

    set_DefaultSchedulerPriority

Routine Description:

    Property

Arguments:

    requiredDefaultSchedulerPriority - default scheduler thread priority for the
                                       print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::DefaultSchedulerPriority::
set(
    System::Threading::ThreadPriority  requiredDefaultSchedulerPriority
    )
{
    VerifyAccess();

    if (defaultSchedulerPriority != requiredDefaultSchedulerPriority)
    {
        defaultSchedulerPriority = requiredDefaultSchedulerPriority;

        PropertiesCollection->GetProperty("DefaultSchedulerPriority")->Value = defaultSchedulerPriority;
    }
}

/*++

Routine Name:

    get_BeepEnabled

Routine Description:

    Property

Arguments:

    None

Return Value:

    Boolean value representing the beep setting for the print server
    represented by this object.

--*/
Boolean
PrintServer::BeepEnabled::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("BeepEnabled", false);
        }
    }
    else
    {
        GetDataFromServer("BeepEnabled", false);
    }

    return beepEnabled;
}

/*++

Routine Name:

    set_BeepEnabled

Routine Description:

    Property

Arguments:

    requiredBeepEnabled - beep setting for the print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::BeepEnabled::
set(
    Boolean requiredBeepEnabled
    )
{
    VerifyAccess();

    if (beepEnabled != requiredBeepEnabled)
    {
        beepEnabled = requiredBeepEnabled;

        PropertiesCollection->GetProperty("BeepEnabled")->Value = beepEnabled;
    }
}

/*++

Routine Name:

    get_NetPopup

Routine Description:

    Property

Arguments:

    None

Return Value:

    Boolean value representing the Net Popup setting for the print server
    represented by this object.

--*/
Boolean
PrintServer::NetPopup::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("NetPopup", false);
        }
    }
    else
    {
        GetDataFromServer("NetPopup", false);
    }

    return netPopup;
}

/*++

Routine Name:

    set_NetPopup

Routine Description:

    Property

Arguments:

    requiredNetPopup - Net Popup setting for the print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::NetPopup::
set(
    Boolean  requiredNetPopup
    )
{
    VerifyAccess();

    if (netPopup != requiredNetPopup)
    {
        netPopup = requiredNetPopup;

        PropertiesCollection->GetProperty("NetPopup")->Value = netPopup;
    }
}

/*++

Routine Name:

    get_EventLog

Routine Description:

    Property

Arguments:

    None

Return Value:

    Int32 representing the event log setting for the print server
    represented by this object.

--*/
PrintServerEventLoggingTypes
PrintServer::EventLog::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("EventLog", false);
        }
    }
    else
    {
        GetDataFromServer("EventLog", false);
    }
    return eventLog;
}

/*++

Routine Name:

    set_EventLog

Routine Description:

    Property

Arguments:

    requiredEventLog - Event log setting for the print server represented by this object.

Return Value:

    void

--*/
void
PrintServer::EventLog::
set(
    PrintServerEventLoggingTypes  requiredEventLog
)
{
    VerifyAccess();

    if (eventLog != requiredEventLog)
    {
        eventLog = requiredEventLog;

        PropertiesCollection->GetProperty("EventLog")->Value = eventLog;
    }
}

/*++

Routine Name:

    get_Name

Routine Description:

    Property

Arguments:

    None

Return Value:

    String representing the name of the print server
    represented by this object.

--*/
String^
PrintServer::Name::
get(
    void
    )
{
    return PrintSystemObject::Name::get();
}

/*++

Routine Name:

    get_MajorVersion

Routine Description:

    Property

Arguments:

    None

Return Value:

    Int32 representing the major version of the print server
    represented by this object.

--*/
Int32
PrintServer::MajorVersion::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("MajorVersion", false);
        }
    }
    else
    {
        GetDataFromServer("MajorVersion", false);
    }

    return majorVersion;
}

/*++

Routine Name:

    get_MinorVersion

Routine Description:

    Property

Arguments:

    None

Return Value:

    Int32 representing the minor version of the print server
    represented by this object.

--*/
Int32
PrintServer::MinorVersion::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("MinorVersion", false);
        }
    }
    else
    {
        GetDataFromServer("MinorVersion", false);
    }

    return minorVersion;
}

/*++

Routine Name:

    get_SubSystemVersion

Routine Description:

    Property

Arguments:

    None

Return Value:

    Byte representing the SubSystem version of the print server 
    represented by this object.

--*/
Byte
PrintServer::SubSystemVersion::
get(
void
)
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("SubSystemVersion", false);
        }
    }
    else
    {
        GetDataFromServer("SubSystemVersion", false);
    }

    return subSystemVersion;
}


/*++

Routine Name:

    set_MajorVersion

Routine Description:

    Property

Arguments:

    version - Byte representing the major version of the print server
              represented by this object.
              This method is called only by the thunking
              code when populates the object.

Return Value:

    None

--*/
void
PrintServer::MajorVersion::
set(
    Int32  version
    )
{
    VerifyAccess();

    if (PropertiesCollection->GetProperty("MajorVersion")->IsInternallyInitialized)
    {
        majorVersion = version;
    }
    else
    {
        throw CreatePrintServerException(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED),
                                         "PrintSystemException.PrintServer.MajorVersionCannotChange"
                                         );

    }
}

/*++

Routine Name:

    set_MinorVersion

Routine Description:

    Property

Arguments:

    version - Byte representing the minor version of the print server
              represented by this object.
              This method is called only by the thunking
              code when populates the object.

Return Value:

    None

--*/
void
PrintServer::MinorVersion::
set(
    Int32  version
    )
{
    VerifyAccess();

    if (PropertiesCollection->GetProperty("MinorVersion")->IsInternallyInitialized)
    {
        minorVersion = version;
    }
    else
    {
        throw CreatePrintServerException(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED),
                                         "PrintSystemException.PrintServer.MinorVersionCannotChange"
                                         );

    }
}

/*++

Routine Name:

    get_RestartJobOnPoolTimeout

Routine Description:

    Property

Arguments:

    None

Return Value:

    Int32 representing the restart job on print pool errors timeout setting,
    in seconds , of the print server represented by this object.

--*/
Int32
PrintServer::RestartJobOnPoolTimeout::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("RestartJobOnPoolTimeout", false);
        }
    }
    else
    {
        GetDataFromServer("RestartJobOnPoolTimeout", false);
    }

    return restartJobOnPoolTimeout;
}

/*++

Routine Name:

    set_RestartJobOnPoolTimeout

Routine Description:

    Property

Arguments:

    requiredRestartJobOnPoolTimeout - restart job on print pool errors timeout setting,
                                      in seconds.

Return Value:

    None

--*/
void
PrintServer::RestartJobOnPoolTimeout::
set(
    Int32 requiredRestartJobOnPoolTimeout
    )
{
    VerifyAccess();

    if (restartJobOnPoolTimeout != requiredRestartJobOnPoolTimeout)
    {
        restartJobOnPoolTimeout = requiredRestartJobOnPoolTimeout;

        PropertiesCollection->GetProperty("RestartJobOnPoolTimeout")->Value = restartJobOnPoolTimeout;
    }
}

/*++

Routine Name:

    get_RestartJobOnPoolEnabled

Routine Description:

    Property

Arguments:

    None

Return Value:

    If different than 0, the job restart on print pool errors is enabled
    on the print server represented by this object.

--*/
Boolean
PrintServer::RestartJobOnPoolEnabled::
get(
    void
    )
{
    VerifyAccess();

    if(isDelayInitialized)
    {
        if (!GetUnInitializedData(refreshPropertiesFilter))
        {
            GetDataFromServer("RestartJobOnPoolEnabled", false);
        }
    }
    else
    {
        GetDataFromServer("RestartJobOnPoolEnabled", false);
    }

    return restartJobOnPoolEnabled;
}

/*++

Routine Name:

    set_RestartJobOnPoolEnabled

Routine Description:

    Property

Arguments:

    requiredRestartJobOnPoolEnabled -   if different than 0, it enables the job restart on
                                        print pool errors timeout.

Return Value:

    None

--*/
void
PrintServer::RestartJobOnPoolEnabled::
set(
    Boolean  requiredRestartJobOnPoolEnabled
    )
{
    VerifyAccess();

    if (restartJobOnPoolEnabled != requiredRestartJobOnPoolEnabled)
    {
        restartJobOnPoolEnabled = requiredRestartJobOnPoolEnabled;

        PropertiesCollection->GetProperty("RestartJobOnPoolEnabled")->Value = restartJobOnPoolEnabled;
    }
}

void
PrintServer::Name::
set(
    String^ name
    )
{
    try
    {
        System::Threading::Monitor::Enter(SyncRoot);

        if (IsInternallyInitialized)
        {
            PropertiesCollection->GetProperty("Name")->IsInternallyInitialized = true;

            PrintSystemObject::Name::set(name);
            PropertiesCollection->GetProperty("Name")->Value = name;
        }
        else
        {
            System::Threading::Monitor::Exit(SyncRoot);
            throw CreatePrintServerException(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED),
                                             "PrintSystemException.PrintServer.NameCannotChange"
                                             );
        }
    }
    __finally
    {
        IsInternallyInitialized = false;
        System::Threading::Monitor::Exit(SyncRoot);
    }
}

/*++

Routine Name:

    CreatePropertiesDelegates

Routine Description:

    This method creates the delegates associated with each property
    of this object. The purpose to invoke one of these delegated is to keep
    the PrintServer's properties in sync with the attribute value collection.
    The delegates are associated with the attribute value created for each of the
    object's property. The attribute value collection is updated through the thunking code
    with data coming from the Spooler service.

Arguments:

    None

Return Value:

    None

--*/
array<MulticastDelegate^>^
PrintServer::
CreatePropertiesDelegates(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = gcnew array<MulticastDelegate^>(primaryAttributeNames->Length);

    propertiesDelegates[0]  = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintServer::DefaultSpoolDirectory::set);
    propertiesDelegates[1]  = gcnew PrintSystemDelegates::ThreadPriorityValueChanged(this,&PrintServer::PortThreadPriority::set);
    propertiesDelegates[2]  = gcnew PrintSystemDelegates::ThreadPriorityValueChanged(this,&PrintServer::DefaultPortThreadPriority::set);
    propertiesDelegates[3]  = gcnew PrintSystemDelegates::ThreadPriorityValueChanged(this,&PrintServer::SchedulerPriority::set);
    propertiesDelegates[4]  = gcnew PrintSystemDelegates::ThreadPriorityValueChanged(this,&PrintServer::DefaultSchedulerPriority::set);
    propertiesDelegates[5]  = gcnew PrintSystemDelegates::BooleanValueChanged(this,&PrintServer::BeepEnabled::set);
    propertiesDelegates[6]  = gcnew PrintSystemDelegates::BooleanValueChanged(this,&PrintServer::NetPopup::set);
    propertiesDelegates[7]  = gcnew PrintSystemDelegates::PrintServerEventLoggingValueChanged(this,&PrintServer::EventLog::set);
    propertiesDelegates[8]  = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintServer::MajorVersion::set);
    propertiesDelegates[9]  = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintServer::MinorVersion::set);
    propertiesDelegates[10] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintServer::RestartJobOnPoolTimeout::set);
    propertiesDelegates[11] = gcnew PrintSystemDelegates::BooleanValueChanged(this,&PrintServer::RestartJobOnPoolEnabled::set);

    return propertiesDelegates;
}

/*++

Routine Name:

    BuildInteropAttributesMap

Routine Description:

    This method creates the tables for delegates associated with each type of attributes
    supported by this object.

Arguments:

    None

Return Value:

    None

--*/
void
PrintServer::
BuildInteropAttributesMap(
    void
    )
{
    getAttributeInteropMap = gcnew Hashtable();
    setAttributeInteropMap = gcnew Hashtable();

    for (Int32 index = 0;
         index < attributeInteropTypes->Length;
         index++)
    {
        getAttributeInteropMap->Add(attributeInteropTypes[index],
                                    getAttributeInteropDelegates[index]);
        setAttributeInteropMap->Add(attributeInteropTypes[index],
                                    setAttributeInteropDelegates[index]);
    }
}

/*++

Routine Name:

    ConvertPropertyFilterToString

Routine Description:

    Converts an array of PrintServerProperties to a string array.

Arguments:

    propertiesFilter - array of PrintServerProperties values

Return Value:

    None

--*/
array<String^>^
PrintServer::
ConvertPropertyFilterToString(
    array<PrintServerIndexedProperty>^      propertiesFilter
    )
{
    array<String^>^ propertiesFilterAsStrings = gcnew array<String^>(propertiesFilter->Length);

    for(Int32 numOfProperties = 0;
        numOfProperties < propertiesFilter->Length;
        numOfProperties++)
    {
        String^ upLevelAttribute = nullptr;

        upLevelAttribute = propertiesFilter[numOfProperties].ToString();

        propertiesFilterAsStrings[numOfProperties] = upLevelAttribute;
    }

    return propertiesFilterAsStrings;
}

/*++

Routine Name:

    GetAllPropertiesFilter

Routine Description:

    Creates an array string representing of all properties of a PrintServer object.

Arguments:

    None

Return Value:

    None

--*/
array<String^>^
PrintServer::
GetAllPropertiesFilter(
    void
    )
{
    array<String^>^ allPropertiesFilter = gcnew array<String^>(PrintServer::primaryAttributeNames->Length);


    for(Int32 numOfAttributes = 0;
        numOfAttributes < primaryAttributeNames->Length;
        numOfAttributes++)
    {
        allPropertiesFilter[numOfAttributes] =
        PrintServer::primaryAttributeNames[numOfAttributes];
    }

    return allPropertiesFilter;
}

/*++

Routine Name:

    GetAllPropertiesFilter

Routine Description:

    Creates an array string representing of the properties of a PrintServer object.

Arguments:

    propertiesFilter - array of propert strings

Return Value:

    None

--*/
array<String^>^
PrintServer::
GetAllPropertiesFilter(
    array<String^>^ propertiesFilter
    )
{
    if (propertiesFilter != nullptr)
    {
        array<String^>^ allPropertiesFilter         = nullptr;
        Int32           numOfPrintServerProperties  = 0;

        for(Int32 index = 0; index < propertiesFilter->Length; index++)
        {
            if (PrintServer::attributeNameTypes->ContainsKey(propertiesFilter[index]))
            {
                numOfPrintServerProperties++;
            }
        }

        allPropertiesFilter = gcnew array<String^>(numOfPrintServerProperties);

        for(Int32 index = 0, numOfPrintServerProperties = 0; index < propertiesFilter->Length; index++)
        {
            if (PrintServer::attributeNameTypes->ContainsKey(propertiesFilter[index]))
            {
                allPropertiesFilter[numOfPrintServerProperties++] = propertiesFilter[index];
            }
        }
        return allPropertiesFilter;
    }
    else
    {
        return GetAllPropertiesFilter();
    }
}


/*++

Routine Name:

    GetAlteredPropertiesFilter

Routine Description:

    Creates an array string representing of the dirty properties of a PrintServer object.

Arguments:

    propertiesFilter - array of property strings

Return Value:

    None

--*/
array<String^>^
PrintServer::
GetAlteredPropertiesFilter(
    void
    )
{
    Int32   indexInAlteredProperties = 0;

    //
    // Typically Properties = Base Class Properties + Inherited Class Properties
    // In this case, we don't allow the base Class Properties to change, so we'll just skip them.
    //
    array<String^>^ probePropertiesFilter = gcnew array<String^>(PrintServer::primaryAttributeNames->Length);

    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintServer::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        if(PropertiesCollection->GetProperty(PrintServer::primaryAttributeNames[numOfAttributes])->IsDirty)
        {
            String^ upLevelAttribute = PrintServer::primaryAttributeNames[numOfAttributes];

            probePropertiesFilter[indexInAlteredProperties++] = upLevelAttribute;
        }
    }

    array<String^>^ alteredPropertiesFilter = nullptr;

    if(indexInAlteredProperties)
    {
        alteredPropertiesFilter = gcnew array<String^>(indexInAlteredProperties);

        for(Int32 indexOfMovedData = 0;
            indexOfMovedData < indexInAlteredProperties;
            indexOfMovedData++)
        {
            alteredPropertiesFilter[indexOfMovedData] = probePropertiesFilter[indexOfMovedData];
        }
    }

    return alteredPropertiesFilter;
}

/*++

Routine Name:

    GetDataFromServer

Routine Description:

    Initialize a given property with data from the Spooler service.

Arguments:

    property - property to be initialized

Return Value:

    None

--*/
void
PrintServer::
GetDataFromServer(
    String^     property,
    Boolean     forceRefresh
    )
{
    String^         propertyName   = nullptr;
    PrintProperty^  attributeValue = PropertiesCollection->GetProperty(property);

    if (attributeValue)
    {
        try
        {
            propertyName = (String^)internalAttributeNameMapping[attributeValue->Name];

            if (forceRefresh || !attributeValue->IsInitialized)
            {
                attributeValue->IsInternallyInitialized = true;

                ThunkGetPrinterData^ interopThunkGetPrinterData =
                (ThunkGetPrinterData^)getAttributeInteropMap[attributeValue->GetType()];

                attributeValue->Value = interopThunkGetPrinterData->Invoke(serverThunkHandler, propertyName);
            }
        }
        catch (InternalPrintSystemException^ internalException)
        {
            bool isNetPopupInvalidParameter = propertyName->Equals("NetPopup") && IsHResultWin32Error(internalException->HResult, ERROR_INVALID_PARAMETER);
            if (!isNetPopupInvalidParameter)
            {

                throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
            }
        }
        __finally
        {
            attributeValue->IsInternallyInitialized = false;
        }
    }
}

/*++

Routine Name:

    IsHResultWin32InvalidParameter

Routine Description:

    Tests to see if a COM HRESULT maps to a WIN32 error code

Arguments:

    hresult             COM HRESULT
    expectedWin32Error  Expected WIN32 error code

Return Value:

    True if hresult maps to the expected WIN32 in expectedWin32Error

--*/
__declspec(noinline)
bool
PrintServer::
IsHResultWin32Error(
    int hresult,
    int expectedWin32Error
    )
{
   return 
       PrinterHResult::HResultFacility(hresult) == PrinterHResult::Facility::Win32 
       && 
       PrinterHResult::HResultCode(hresult) == expectedWin32Error;
}

/*++

Routine Name:

    ComitDirtyData

Routine Description:

    Commits the dirty properties specified in the
    array of properties.

Arguments:

    properties - array of properties to be commited

Return Value:

    None

--*/
void
PrintServer::
ComitDirtyData(
    array<String^>^ properties
    )
{
    int  propertieIndex = 0;
    Collection<String^>^ commitedAttributes = gcnew Collection<String^>();

    try
    {
        if (properties != nullptr)
        {
            for(int propertieIndex = 0;
                propertieIndex < properties->Length;
                propertieIndex++)
            {
                PrintProperty^  attributeValue = PropertiesCollection->GetProperty(properties[propertieIndex]);
                String^         propertyName   = (String^)internalAttributeNameMapping[attributeValue->Name];

                ThunkSetPrinterData^ interopThunkSetPrinterData =
                (ThunkSetPrinterData^)setAttributeInteropMap[attributeValue->GetType()];

                interopThunkSetPrinterData->Invoke(serverThunkHandler, propertyName, attributeValue->Value);

                commitedAttributes->Add(attributeValue->Name);
                get_InternalPropertiesCollection(attributeValue->Name)->GetProperty(attributeValue->Name)->IsDirty = false;
            }
        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        Collection<String^>^ failedAttributes = gcnew Collection<String^>();

        for (; propertieIndex < properties->Length; propertieIndex++)
        {
            failedAttributes->Add(properties[propertieIndex]);
        }

        throw CreatePrintCommitAttributesException(internalException->HResult,
                                                   "PrintSystemException.PrintServer.Commit",
                                                   commitedAttributes,
                                                   failedAttributes
                                                   );
    }
}

void
PrintServer::
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



/*++

Routine Name:

    GetUnInitializedData

Routine Description:

    Initialize an array of properties with data from the Spooler service.

Arguments:

    properties - array of string representing the properties to be initialized

Return Value:

    None

--*/
Boolean
PrintServer::
GetUnInitializedData(
    array<String^>^     properties
    )
{
    Boolean returnValue = true;

    for(int numOfProperties = 0;
        numOfProperties < properties->Length;
        numOfProperties++)
    {
        try
        {
            GetDataFromServer(properties[numOfProperties], false);
        }
        catch (InternalPrintSystemException^)
        {
            returnValue = false;
        }
    }

    isDelayInitialized = false;

    return returnValue;
}

/*++

Routine Name:

    RegisterAttributesNamesTypes

Routine Description:

    Initializes the internal table that keeps the association between
    a property name and a attribute value type.

Arguments:

    None

Return Value:

    None

--*/
void
PrintServer::
RegisterAttributesNamesTypes(
    void
    )
{
    //
    // Register the attributes of the base class first
    //
    PrintSystemObject::RegisterAttributesNamesTypes(PrintServer::attributeNameTypes);
    //
    // Register the attributes of the current class
    //
    for(int numOfAttributes = 0;
        numOfAttributes < PrintServer::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintServer::primaryAttributeNames[numOfAttributes],
                                PrintServer::primaryAttributeTypes[numOfAttributes]);
    }
}


/*++

Routine Name:

    RegisterAttributesNamesTypes

Routine Description:

    Initializes the internal table that keeps the association between
    a property name and a attribute value type.

Arguments:

    attributeNameTypes  Hashtable supplied by the inherited class to fill in.

Return Value:

    None

--*/
void
PrintServer::
RegisterAttributesNamesTypes(
    Hashtable^ childAttributeNameTypes
    )
{
    //
    // Register the attributes of the base class first
    //
    PrintSystemObject::RegisterAttributesNamesTypes(childAttributeNameTypes);
    //
    // Register the attributes of the current class
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintServer::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        childAttributeNameTypes->Add(PrintServer::primaryAttributeNames[numOfAttributes],
                                     PrintServer::primaryAttributeTypes[numOfAttributes]);
    }
}


/*++

Routine Name:

    CreateAttributeNoValue

Routine Description:

    Creates an uninitialized PrintProperty object
    associated with a given property.

Arguments:

    attributeName   - name of the property

Return Value:

    None

--*/
PrintProperty^
PrintServer::
CreateAttributeNoValue(
    String^ attributeName
    )
{
    Type^ type = (Type^)PrintServer::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName);
}

/*++

Routine Name:

    CreateAttributeValue

Routine Description:

    Creates a PrintProperty object
    associated with a given property and initializes it with the given value.

Arguments:

    attributeName   - name of the property
    attributeValue  - value of the property

Return Value:

    None

--*/
PrintProperty^
PrintServer::
CreateAttributeValue(
    String^ attributeName,
    Object^ attributeValue
    )
{
    Type^ type = (Type^)PrintServer::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue);
}

/*++

Routine Name:

    CreateAttributeNoValueLinked

Routine Description:

    Creates a PrintProperty object
    associated with a given property and links it with a delegate
    that will keep the attribute value and the property in sync.

Arguments:

    attributeName   - name of the property
    delegate        - delegate to be linked to attribute value

Return Value:

    None

--*/
PrintProperty^
PrintServer::
CreateAttributeNoValueLinked(
    String^             attributeName,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintServer::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,delegate);
}

/*++

Routine Name:

    CreateAttributeValueLinked

Routine Description:

    Creates a PrintProperty object
    associated with a given property and links it with a delegate
    that will keep the attribute value and the property in sync.

Arguments:

    attributeName   - name of the property
    attributeValue  - value of the property
    delegate        - delegate to be linked to attribute value

Return Value:

    None

--*/
PrintProperty^
PrintServer::
CreateAttributeValueLinked(
    String^             attributeName,
    Object^             attributeValue,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintServer::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue,delegate);
}

/*++

Routine Name:

    get_InternalPropertiesCollection

Routine Description:

    Property

Arguments:

    attributeName   - name of the property

Return Value:

    None

--*/
PrintPropertyDictionary^
PrintServer::
get_InternalPropertiesCollection(
    String^ attributeName
    )
{
    return (PrintPropertyDictionary^)collectionsTable[attributeName];
}

/*++

Routine Name:

    get_IsInternallyInitialized

Routine Description:

    Property

Arguments:

    None

Return Value:

    boolean

--*/
bool
PrintServer::IsInternallyInitialized::
get(
    void
    )
{
    return isInternallyInitialized;
}

/*++

Routine Name:

    set_IsInternallyInitialized

Routine Description:

    Property

Arguments:

    isInternallyInitialized

Return Value:

    void

--*/
void
PrintServer::IsInternallyInitialized::
set(
    bool isInternallyInitialized
    )
{
    this->isInternallyInitialized = isInternallyInitialized;
}


/*++
    Function Name:
        get_IsDelayInitialized

    Description:
        We delay initialized the PrintServer properties for performance
        reasons. This is the method which tells us whether the switch
        for delay initialization is turned on or not.

    Parameters:
        None

    Return Value
        Boolean
--*/
Boolean
PrintServer::IsDelayInitialized::
get(
    void
    )
{
    return isDelayInitialized;
}

/*++
    Function Name:
        set_IsDelayInitialized

    Description:
        We delay initialized the PrintServer properties for performance
        reasons. Once the parameters are initialized we need to swich the
        parameter off.

    Parameters:
        None

    Return Value
        Boolean
--*/
void
PrintServer::IsDelayInitialized::
set(
    Boolean delayInitialized
    )
{
    isDelayInitialized = delayInitialized;
}

__declspec(noinline)
Exception^
PrintServer::CreatePrintServerException(
    int hresult,
    String^ messageId
    )
{
    return gcnew PrintServerException(hresult, messageId, Name);
}


__declspec(noinline)
Exception^
PrintServer::CreatePrintServerException(
    int hresult,
    String^ messageId,
    Exception^ innerException
    )
{
    return gcnew PrintServerException(hresult, messageId, Name, innerException);
}

__declspec(noinline)
Exception^
PrintServer::CreatePrintCommitAttributesException(
    int hresult,
    String^ messageId,
    System::Collections::ObjectModel::Collection<String^>^ commitedAttributes,
    System::Collections::ObjectModel::Collection<String^>^ failedAttributes
    )
{
    return gcnew PrintCommitAttributesException(hresult,
                                               messageId,
                                               commitedAttributes,
                                               failedAttributes,
                                               Name);
}
