// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:
        This file includes the implementation for a managed
        PrintQueue
--*/


#include "win32inc.hpp"



#define HRESULT LONG

using namespace MS::Internal::Telemetry::PresentationCore;

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Collections::Specialized;
using namespace System::Xml;
using namespace System::Xml::XPath;

using namespace System::Printing::Interop;

using namespace System::IO::Packaging;
using namespace System::Windows::Documents;
using namespace System::Windows::Xps::Serialization;
using namespace System::Windows::Xps::Packaging;

using namespace System::Drawing::Printing;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

#ifndef  __PRINTSYSTEMPATHRESOLVER_HPP__
#include <PrintSystemPathResolver.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

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

#ifndef  __SETDATATHUNKOBJECT_HPP__
#include <SetDataThunkObject.hpp>
#endif

#ifndef __PREMIUMPRINTSTREAM_HPP__
#include <PremiumPrintStream.hpp>
#endif


using namespace System::Threading;

[assembly:System::Runtime::InteropServices::ComVisibleAttribute(false)];

using namespace MS::Internal;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForPrintQueue;

using namespace System::Security;
using namespace System::Drawing::Printing;

/*--------------------------------------------------------------------------------------*/
/*                              PrintQueue Implementation                               */
/*--------------------------------------------------------------------------------------*/

/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:    Server on which object would be instantiated
                        Null == on this local print server
        String:         Name of the Print Queue targeted on that server

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^    printServer,
    String^         printQueueName
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    Initialize(printServer, printQueueName, (array<String^>^)(nullptr), nullptr);
}


/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:    Server on which object would be instantiated
                        Null == on this local print server
        String:         Name of the Print Queue targeted on that server
        Int32:          client schema version

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^    printServer,
    String^         printQueueName,
    Int32           printSchemaVersion
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(printSchemaVersion),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    Initialize(printServer, printQueueName, (array<String^>^)(nullptr), nullptr);
}


/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:    Server on which object would be instantiated
                        Null == on this local print server
        String:         Name of the Print Queue targeted on that server
        String[]:       Names of properties that the queue would be
                        initialized with. If someone is interested in a
                        subset of the the PrintQueue properties they
                        could require a parameter looking like
                        new String^[]={"Comment","Location"}

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^        printServer,
    String^             printQueueName,
    array<String^>^     propertiesFilter
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    Initialize(printServer, printQueueName, propertiesFilter, nullptr);
}


/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:                   Server on which object would be instantiated
                                       Null == on this local print server
        String:                        Name of the Print Queue targeted on that server
        PrintQueueIndexedProperty[]:   Enums of properties that the queue would be
                                       initialized with. If someone is interested in a
                                       subset of the the PrintQueue properties they
                                       could require a parameter looking like
                                       PrintQueueIndexedProperty[]={PrintQueueIndexedProperty::QueueDriver,
                                       PrintQueueIndexedProperty::QueueStatus}


    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^                       printServer,
    String^                            printQueueName,
    array<PrintQueueIndexedProperty>^  propertiesFilter
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    Initialize(printServer,
               printQueueName,
               PrintQueue::ConvertPropertyFilterToString(propertiesFilter),
               nullptr);
}


/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:                Server on which object would be instantiated
                                    Null == on this local print server
        String:                     Name of the Print Queue targeted on that server
        PrintSystemDesiredAccess:   Security role-based deired access.

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^                printServer,
    String^                     printQueueName,
    PrintSystemDesiredAccess    desiredAccess
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(printServer, printQueueName, (array<String^>^)(nullptr), printerDefaults);
}


/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:                Server on which object would be instantiated
                                    Null == on this local print server
        String:                     Name of the Print Queue targeted on that server
        Int32:                      Client Schema Version
        PrintSystemDesiredAccess:   Security role-based deired access.

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^                printServer,
    String^                     printQueueName,
    Int32                       printSchemaVersion,
    PrintSystemDesiredAccess    desiredAccess
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(printSchemaVersion),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(printServer, printQueueName, (array<String^>^)(nullptr), printerDefaults);
}


/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:                Server on which object would be instantiated
                                    Null == on this local print server
        String:                     Name of the Print Queue targeted on that server
        String[]:                   Names of properties that the queue would be
                                    initialized with. If someone is interested in a
                                    subset of the the PrintQueue properties they
                                    could require a parameter looking like
                                    new String^[]={"Comment","Location"}
        PrintSystemDesiredAccess:   Security role-based deired access.

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^                printServer,
    String^                     printQueueName,
    array<String^>^             propertiesFilter,
    PrintSystemDesiredAccess    desiredAccess
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(printServer, printQueueName, propertiesFilter, printerDefaults);

}
/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance

    Parameters:
        PrintServer:                    Server on which object would be instantiated
                                        Null == on this local print server
        String:                         Name of the Print Queue targeted on that server
        PrintQueueIndexedProperty[]:    Enums of properties that the queue would be
                                        initialized with. If someone is interested in a
                                        subset of the the PrintQueue properties they
                                        could require a parameter looking like
                                        PrintQueueIndexedProperty[]={PrintQueueIndexedProperty::QueueDriver,
                                                              PrintQueueIndexedProperty::QueueStatus}
        PrintSystemDesiredAccess:   Security role-based deired access.

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^                       printServer,
    String^                            printQueueName,
    array<PrintQueueIndexedProperty>^  propertiesFilter,
    PrintSystemDesiredAccess           desiredAccess
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                             nullptr,
                                                             desiredAccess);

    Initialize(printServer,
               printQueueName,
               PrintQueue::ConvertPropertyFilterToString(propertiesFilter),
               printerDefaults);
}

/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance for a browsable PritnQueue
        used during enumerations

    Parameters:
        PrintQueueIndexedProperty[]:      Enums of properties that the queue would be
                                          initialized with. If someone is interested in a
                                          subset of the the PrintQueue properties they
                                          could require a parameter looking like
                                          PrintQueueProperty[]={PrintQueueIndexedProperty::QueueDriver,
                                                                PrintQueueIndexedProperty::QueueStatus}

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    array<String^>^     propertiesFilter
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
isBrowsable(true),
refreshPropertiesFilter(nullptr),
hostingPrintServer(nullptr),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    try
    {
        InPartialTrust = false;
        InitializeInternalCollections();
        refreshPropertiesFilter = propertiesFilter;
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult, "PrintSystemException.PrintQueue.Generic");
    }
}



/*++
    Function Name:
        PrintQueue

    Description:
        Constructor of class instance for a browsable PritnQueue
        used during enumerations

    Parameters:
        PrintServer:                The Print Server hosting the print queue

        PrintQueueIndexedProperty[]:       Enums of properties that the queue would be
                                           initialized with. If someone is interested in a
                                           subset of the the PrintQueue properties they
                                           could require a parameter looking like
                                           PrintQueueProperty[]={PrintQueueIndexedProperty::QueueDriver,
                                                                 PrintQueueIndexedProperty::QueueStatus}

    Return Value
        None
--*/
PrintQueue::
PrintQueue(
    PrintServer^        printServer,
    array<String^>^     propertiesFilter
    ):
printerThunkHandler(nullptr),
printTicketManager(nullptr),
isBrowsable(true),
refreshPropertiesFilter(nullptr),
hostingPrintServer(printServer),
fullQueueName(nullptr),
clientPrintSchemaVersion(1),
printingIsCancelled(false),
accessVerifier(nullptr),
_lockObject(gcnew Object())
{
    try
    {
        InPartialTrust = false;
        InitializeInternalCollections();
        refreshPropertiesFilter = propertiesFilter;
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult, "PrintSystemException.PrintQueue.Generic");
    }
}
void
PrintQueue::
Initialize(
    PrintServer^                printServer,
    String^                     printQueueName,
    array<String^>^             propertiesFilter,
    PrinterDefaults^            printerDefaults
    )
{
    bool                                disposePrinterThunkHandler = false;
    GetDataThunkObject^                 dataThunkObject            = nullptr;

    _currentJobSettings = nullptr;

    InPartialTrust = false;

    try
    {
        isWriterAttached = false;
        writerStream     = nullptr;
        xpsDocument      = nullptr;

        InitializeInternalCollections();

        PropertiesCollection->GetProperty("Name")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("Name")->Value                   = printQueueName;
        //
        // We have to resolve the name of the Print Server and the
        // Print Queue to map to one entity to be usefull for dowlevel
        // thunking
        //
        fullQueueName = PrepareNameForDownLevelConnectivity(printServer->Name,Name);
        //
        // Call the thunk code to do the actual OpenPrinter
        //
        printerThunkHandler = gcnew PrintWin32Thunk::PrinterThunkHandler(fullQueueName,printerDefaults);
        //
        // Since no Filters were provided in the constructor, I would
        // instantiate an object with all possible properties populated
        //
        array<String^>^ propertiesAsStrings = PrintQueue::GetAllPropertiesFilter(propertiesFilter);
        //
        // Call the thunking code to populate the required properties of the
        // PrintQueue Object
        //
        dataThunkObject = gcnew GetDataThunkObject(this->GetType());
        dataThunkObject->PopulatePrintSystemObject(printerThunkHandler,
                                                   this,
                                                   propertiesAsStrings);
        //
        // When an object consumer asks for a refresh on the object,
        // I only refresh the properties that he already asked for and
        // those are maintained in the following array
        //
        refreshPropertiesFilter = propertiesAsStrings;
    }
    catch (InternalPrintSystemException^ internalException)
    {
        disposePrinterThunkHandler = true;

        throw CreatePrintQueueException(internalException->HResult, "PrintSystemException.PrintQueue.Populate");
    }
    __finally
    {
        if (disposePrinterThunkHandler &&
            printerThunkHandler)
        {
            delete printerThunkHandler;
            printerThunkHandler = nullptr;
        }

        if (dataThunkObject)
        {
            delete dataThunkObject;
            dataThunkObject = nullptr;
        }

        if (printerDefaults)
        {
            delete printerDefaults;
            printerDefaults = nullptr;
        }
    }
}

void
PrintQueue::
ActivateBrowsableQueue(
    void
    )
{
    bool  disposePrinterThunkHandler = false;

    try
    {
        PrinterDefaults^ printerDefaults = nullptr;

        fullQueueName = PrepareNameForDownLevelConnectivity(hostingPrintServer->Name,Name);

        printerThunkHandler = gcnew PrintWin32Thunk::PrinterThunkHandler(fullQueueName, printerDefaults);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        disposePrinterThunkHandler = true;

        throw CreatePrintQueueException(internalException->HResult, "PrintSystemException.PrintQueue.Populate");
    }
    __finally
    {
        if (disposePrinterThunkHandler &&
            printerThunkHandler)
        {
            delete printerThunkHandler;
            printerThunkHandler = nullptr;
        }
    }
}


/*++
    Routine Name:
        Install

    Routine Description:
        Installs a print queue on the print server

    Arguments:
        printServer             - Print Server Object
        printQueueName          - print queue name
        driverName              - driver name
        portNames[]             - array of port names
        printProcessorName      - print processor name
        printQueueAttributes    - attributes

    Return Value:
        PrintQueue object representing the just installed printer.
--*/
PrintQueue^
PrintQueue::
Install(
    PrintServer^                            printServer,
    String^                                 printQueueName,
    String^                                 driverName,
    array<String^>^                         portNames,
    String^                                 printProcessorName,
    PrintQueueAttributes                    printQueueAttributes
    )
{
    PrintWin32Thunk::PrinterThunkHandler^ printerHandle = nullptr;

    try
    {
        if (printQueueName && driverName && portNames)
        {
            printerHandle = PrintWin32Thunk::PrinterThunkHandler::ThunkAddPrinter(printServer->Name,
                                                                                  printQueueName,
                                                                                  driverName,
                                                                                  BuildPortNamesString(portNames),
                                                                                  printProcessorName,
                                                                                  nullptr,
                                                                                  nullptr,
                                                                                  nullptr,
                                                                                  nullptr,
                                                                                  static_cast<int>(printQueueAttributes),
                                                                                  0,
                                                                                  0);


        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        if (printerHandle)
        {
            delete printerHandle;
        }

        throw printServer->CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.AddPrinter");
    }
    __finally
    {
        if (printerHandle)
        {
            delete printerHandle;
        }
    }

    return gcnew PrintQueue(printServer, printQueueName);
}

/*++
    Routine Name:
        Install

    Routine Description:
        Installs a print queue on the print server

    Arguments:
        printServer                 - Print Server Object
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
PrintQueue::
Install(
    PrintServer^                            printServer,
    String^                                 printQueueName,
    String^                                 driverName,
    array<String^>^                         portNames,
    String^                                 printProcessorName,
    PrintQueueAttributes                    printQueueAttributes,
    PrintQueueStringProperty^               requiredPrintQueueProperty,
    Int32                                   requiredPriority,
    Int32                                   requiredDefaultPriority
    )
{
    PrintWin32Thunk::PrinterThunkHandler^ printerHandle = nullptr;

    try
    {
        if (printQueueName && driverName && portNames)
        {
            array<String^>^ commentLocationSharename = gcnew array<String^>(3);

            try
            {
                int i = (int)requiredPrintQueueProperty->Type;
                commentLocationSharename[i] = requiredPrintQueueProperty->Name;
            }
            catch (IndexOutOfRangeException^ e)
            {
                throw printServer->CreatePrintServerException(HRESULT_FROM_WIN32(ERROR_INVALID_PARAMETER),
                                                 "PrintSystemException.PrintServer.AddPrinter",
                                                 e);
            }

            printerHandle = PrintWin32Thunk::
                            PrinterThunkHandler::
                            ThunkAddPrinter(printServer->Name,
                                            printQueueName,
                                            driverName,
                                            PrintQueue::BuildPortNamesString(portNames),
                                            printProcessorName,
                                            commentLocationSharename[(int)PrintQueueStringPropertyType::Comment],
                                            commentLocationSharename[(int)PrintQueueStringPropertyType::Location],
                                            commentLocationSharename[(int)PrintQueueStringPropertyType::ShareName],
                                            nullptr,
                                            static_cast<int>(printQueueAttributes),
                                            requiredPriority,
                                            requiredDefaultPriority);
        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        if (printerHandle)
        {
            delete printerHandle;
        }
        throw printServer->CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.AddPrinter");
    }
    __finally
    {
        if (printerHandle)
        {
            delete printerHandle;
        }
    }

    return gcnew PrintQueue(printServer, printQueueName);
}


/*++
    Routine Name:
        Install

    Routine Description:
        Installs a print queue on the print server

    Arguments:
        printServer                 - Print Server Object
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
PrintQueue::
Install(
    PrintServer^                            printServer,
    String^                                 printQueueName,
    String^                                 driverName,
    array<String^>^                         portNames,
    String^                                 printProcessorName,
    PrintQueueAttributes                    printQueueAttributes,
    String^                                 requiredShareName,
    String^                                 requiredComment,
    String^                                 requiredLocation,
    String^                                 requiredSeparatorFile,
    Int32                                   requiredPriority,
    Int32                                   requiredDefaultPriority
    )
{
    PrintWin32Thunk::PrinterThunkHandler^ printerHandle = nullptr;

    try
    {
        if (printQueueName && driverName && portNames)
        {
            printerHandle = PrintWin32Thunk::PrinterThunkHandler::ThunkAddPrinter(printServer->Name,
                                                                                  printQueueName,
                                                                                  driverName,
                                                                                  BuildPortNamesString(portNames),
                                                                                  printProcessorName,
                                                                                  requiredComment,
                                                                                  requiredLocation,
                                                                                  requiredShareName,
                                                                                  requiredSeparatorFile,
                                                                                  static_cast<int>(printQueueAttributes),
                                                                                  requiredPriority,
                                                                                  requiredDefaultPriority);
        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        if (printerHandle)
        {
            delete printerHandle;
        }

        throw printServer->CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.AddPrinter");
    }
    __finally
    {
        if (printerHandle)
        {
            delete printerHandle;
        }
    }

    return gcnew PrintQueue(printServer, printQueueName);
}


/*++
    Routine Name:
        Install

    Routine Description:
        Installs a print queue on the print server

    Arguments:
        printServer                 - Print Server Object
        printQueueName              - print queue name
        driverName                  - driver name
        portNames[]                 - array of port names
        initParams                  - attribute value collection that specifies
                                      the rest of the properties

    Return Value:
        PrintQueue object representing the just installed printer.
--*/
PrintQueue^
PrintQueue::
Install(
    PrintServer^                            printServer,
    String^                                 printQueueName,
    String^                                 driverName,
    array<String^>^                         portNames,
    String^                                 printProcessorName,
    PrintPropertyDictionary^                initParams
    )
{
    PrintWin32Thunk::PrinterThunkHandler^ printerHandle         = nullptr;
    PrinterInfoTwoSetter^                 printInfoTwoLeveThunk = nullptr;
    PrintQueue^                           installedPrintQueue   = nullptr;

    try
    {
        if (printQueueName && driverName && portNames)
        {
            printInfoTwoLeveThunk = gcnew PrinterInfoTwoSetter;

            IEnumerator^ initParamsEnumerator = initParams->GetEnumerator();

            Hashtable^ setParameters = gcnew Hashtable;

            //
            // Set the attribute values in the printInfoTwoLeveThunk
            // Skip the attributes that are not settable and that are covered by different levels
            //
            for ( ; initParamsEnumerator->MoveNext() ;)
            {
                DictionaryEntry^     collectionEntry = (DictionaryEntry^)initParamsEnumerator->Current;
                PrintProperty^       attributeValue  = (PrintProperty^)collectionEntry->Value;

                if (attributeValue->Value)
                {
                    if (!attributeValue->Name->Equals("HostingPrintServer") &&
                        !attributeValue->Name->Equals("Name"))
                    {

                        if (attributeValue->Name->Equals("UserPrintTicket") ||
                            attributeValue->Name->Equals("DefaultPrintTicket"))
                        {
                            setParameters->Add(attributeValue->Name, attributeValue);
                        }
                        else
                        {
                            printInfoTwoLeveThunk->SetValueFromName(PrintQueue::GetAttributeNamePerPrintQueueObject(attributeValue),
                                                                    PrintQueue::GetAttributeValuePerPrintQueueObject(attributeValue));
                        }
                    }
                }
            }

            //
            // Overwrite the attributes with the values passed in as parameters
            //
            printInfoTwoLeveThunk->SetValueFromName("Name",                    printQueueName);
            printInfoTwoLeveThunk->SetValueFromName("QueueDriverName",         driverName);
            printInfoTwoLeveThunk->SetValueFromName("QueuePortName",           BuildPortNamesString(portNames));
            printInfoTwoLeveThunk->SetValueFromName("QueuePrintProcessorName", printProcessorName);

            printerHandle = PrintWin32Thunk::PrinterThunkHandler::ThunkAddPrinter(printServer->Name,
                                                                                  printInfoTwoLeveThunk);

            //
            // The printer was created. Set the rest of the attributes that weren't covered by level 2.
            // If anything throws after this point we should try and delete the printer.
            //
            installedPrintQueue = gcnew PrintQueue(printServer, printQueueName, PrintSystemDesiredAccess::AdministratePrinter);

            IEnumerator^ remainingParamsEnumerator = setParameters->GetEnumerator();

            for ( ; remainingParamsEnumerator->MoveNext() ;)
            {
                DictionaryEntry^    collectionEntry = (DictionaryEntry^)remainingParamsEnumerator->Current;
                PrintProperty^      attributeValue  = (PrintProperty^)collectionEntry->Value;

                installedPrintQueue->PropertiesCollection->GetProperty(attributeValue->Name)->Value = attributeValue->Value;
            }

            installedPrintQueue->Commit();

        }
    }
    catch (PrintSystemException^ printException)
    {
        if (printerHandle)
        {
            delete printerHandle;
        }

        if (printInfoTwoLeveThunk)
        {
            delete printInfoTwoLeveThunk;
        }

        if (installedPrintQueue)
        {
            //
            // printServer->DeletePrintQueue(printQueueName);
            //
        }

        throw printException;
    }
    catch (InternalPrintSystemException^ internalException)
    {
        if (printerHandle)
        {
            delete printerHandle;
        }

        if (printInfoTwoLeveThunk)
        {
            delete printInfoTwoLeveThunk;
        }

        if (installedPrintQueue)
        {
            //
            // printServer->DeletePrintQueue(printQueueName);
            //
        }

        throw printServer->CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.AddPrinter");
    }
    __finally
    {
        if (printerHandle)
        {
            delete printerHandle;
        }

        if (printInfoTwoLeveThunk)
        {
            delete printInfoTwoLeveThunk;
        }
    }


    return installedPrintQueue;
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
PrintQueue::
Delete(
    String^     printQueueName
    )
{
    bool                                  returnValue         = false;
    PrintWin32Thunk::PrinterThunkHandler^ printerThunkHandler = nullptr;

    try
    {
        PrinterDefaults^ printerDefaults = gcnew PrinterDefaults(nullptr,
                                                                 nullptr,
                                                                 PrintSystemDesiredAccess::AdministratePrinter);

        printerThunkHandler = gcnew PrintWin32Thunk::PrinterThunkHandler(printQueueName,
                                                                         printerDefaults);

        if (printerThunkHandler && !printerThunkHandler->IsInvalid)
        {
            returnValue = printerThunkHandler->ThunkDeletePrinter();
        }
    }
    __finally
    {
        if (printerThunkHandler)
        {
            delete printerThunkHandler;
        }
    }

    return returnValue;
}

void
PrintQueue::
InternalDispose(
    bool disposing
    )
{
    if(!this->IsDisposed)
    {
        System::Threading::Monitor::Enter(_lockObject);
        {
            try
            {
                if(!IsDisposed)
                {
                    if (disposing)
                    {
                        if (printerThunkHandler)
                        {
                            delete printerThunkHandler;
                            printerThunkHandler = nullptr;
                        }

                        if (printTicketManager)
                        {
                            delete printTicketManager;
                            printTicketManager = nullptr;
                        }
                    }
                }
            }
            __finally
            {
                try
                {
                    PrintSystemObject::InternalDispose(disposing);

                }
                __finally
                {
                    this->IsDisposed = true;
                    System::Threading::Monitor::Exit(_lockObject);
                }
            }
        }
    }
}

PrintCapabilities^
PrintQueue::
GetPrintCapabilities(
    PrintTicket^                 printTicket
    )
{
    VerifyAccess();

    if(printTicketManager == nullptr)
    {
        printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
    }

    PrintCapabilities^  capabilities = printTicketManager->GetPrintCapabilities(printTicket);

    return capabilities;
}


PrintCapabilities^
PrintQueue::
GetPrintCapabilities(
    void
    )
{
    VerifyAccess();

    if(printTicketManager == nullptr)
    {
        printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
    }

    return printTicketManager->GetPrintCapabilities(nullptr);
}

MemoryStream^
PrintQueue::
GetPrintCapabilitiesAsXml(
    PrintTicket^                 printTicket
    )
{
    VerifyAccess();

    if(printTicketManager == nullptr)
    {
        printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
    }

    return printTicketManager->GetPrintCapabilitiesAsXml(printTicket);
}

MemoryStream^
PrintQueue::
GetPrintCapabilitiesAsXml(
    void
    )
{
    VerifyAccess();

    if(printTicketManager == nullptr)
    {
        printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
    }

    return printTicketManager->GetPrintCapabilitiesAsXml(nullptr);
}

ValidationResult
PrintQueue::
MergeAndValidatePrintTicket(
    PrintTicket^   basePrintTicket,
    PrintTicket^   deltaPrintTicket
    )
{
    VerifyAccess();

    if(printTicketManager == nullptr)
    {
        printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
    }

    return printTicketManager->MergeAndValidatePrintTicket(basePrintTicket,
                                                           deltaPrintTicket);
}

ValidationResult
PrintQueue::
MergeAndValidatePrintTicket(
    PrintTicket^     basePrintTicket,
    PrintTicket^     deltaPrintTicket,
    PrintTicketScope scope
    )
{
    VerifyAccess();

    if(printTicketManager == nullptr)
    {
        printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
    }

    return printTicketManager->MergeAndValidatePrintTicket(basePrintTicket,
                                                           deltaPrintTicket,
                                                           scope);
}


/*++
    Function Name:
        Pause

    Description:
        Pause the Printer

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
Pause(
    void
    )
{
    VerifyAccess();

    try
    {
        printerThunkHandler->ThunkSetPrinter(PRINTER_CONTROL_PAUSE);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult,
                                        "PrintSystemException.PrintQueue.Pause");
    }
}


/*++
    Function Name:
        Purge

    Description:
        Deletes all the jobs in the printer

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
Purge(
    void
    )
{
    VerifyAccess();

    try
    {
        printerThunkHandler->ThunkSetPrinter(PRINTER_CONTROL_PURGE);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult,
                                        "PrintSystemException.PrintQueue.Purge");
    }
}


Boolean
PrintQueue::PrintingIsCancelled::
get(
    void
   )
{
    VerifyAccess();

    return printingIsCancelled;
}

void
PrintQueue::PrintingIsCancelled::
set(
    Boolean isCancelled
   )
{
    VerifyAccess();

    printingIsCancelled = isCancelled;
}

/*++
    Function Name:
        Resume

    Description:
        Resumes the pasued printer

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
Resume(
    void
    )
{
    VerifyAccess();

    try
    {
        printerThunkHandler->ThunkSetPrinter(PRINTER_CONTROL_RESUME);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult,
                                        "PrintSystemException.PrintQueue.Resume");
    }
}

PrintSystemJobInfo^
PrintQueue::
AddJob(
    void
    )
{
    VerifyAccess();

    // We need to pass down a print ticket so that the job ID will be available
    // immediately.  Since the caller did not specify a print ticket, we will use
    // the user/default print ticket for this print queue.
    PrintTicket^ printTicket = UserPrintTicket;
    if(printTicket == nullptr)
    {
        printTicket = DefaultPrintTicket;
    }


    PrintSystemJobInfo^ jobInfo = PrintSystemJobInfo::Add(this, printTicket);
    return jobInfo;
}

PrintSystemJobInfo^
PrintQueue::
AddJob(
    String^     jobName
    )
{
    VerifyAccess();

    // We need to pass down a print ticket so that the job ID will be available
    // immediately.  Since the caller did not specify a print ticket, we will use
    // the user/default print ticket for this print queue.
    PrintTicket^ printTicket = UserPrintTicket;
    if(printTicket == nullptr)
    {
        printTicket = DefaultPrintTicket;
    }

    PrintSystemJobInfo^ jobInfo = PrintSystemJobInfo::Add(this, jobName, printTicket);
    return jobInfo;
}

PrintSystemJobInfo^
PrintQueue::
AddJob(
    String^         jobName,
    PrintTicket^    printTicket
    )
{
    VerifyAccess();

    // Get the UserPrintTicket.  We don't need it, but fetching it has a side-effect
    // of initializing the PrinterThunkHandler.  In some cases (e.g. Win7 printing
    // to XPS printer), this doesn't happen any other way so calling this method
    // would get NullReferenceException (Dev11 780899).
    PrintTicket ^userPrintTicket = UserPrintTicket;
    if (userPrintTicket == nullptr)     // keep the compiler from optimizing away the previous call
    {
        userPrintTicket = printTicket;  // no real effect
    }

    // Note: in the other overloads of AddJob we defaulted to either the
    // UserTicket or the DefaultTicket.  We intentionally do not fallback to
    // using those tickets if the caller passed in a null ticket to allow the
    // caller to create a print job without a ticket.  This will have the
    // consequence on >= win8 that the JobID will not be available, but
    // it allows the caller to avoid the consequences of using a print ticket
    // that may specify incompatible settings with the print ticket in the
    // payload written to the print stream.
    PrintSystemJobInfo^ jobInfo = PrintSystemJobInfo::Add(this, jobName, printTicket);
    return jobInfo;
}

PrintSystemJobInfo^
PrintQueue::
AddJob(
    String^     jobName,
    String^     document,
    Boolean     fastCopy
    )
{
    VerifyAccess();

    PrintTicket^ printTicket = nullptr;

    if (!IsXpsDevice)
    {
        // We need to pass down a print ticket so that the job ID will be available
        // immediately.  Since the caller did not specify a print ticket, we will use
        // the user/default print ticket for this print queue.
        printTicket = UserPrintTicket;
        if(printTicket == nullptr)
        {
            printTicket = DefaultPrintTicket;
        }
    }

    PrintSystemJobInfo^ jobInfo = PrintSystemJobInfo::Add(this, jobName, document, fastCopy, printTicket);
    return jobInfo;
}


PrintSystemJobInfo^
PrintQueue::
AddJob(
    String^         jobName,
    String^         document,
    Boolean         fastCopy,
    PrintTicket^    printTicket
    )
{
    VerifyAccess();

    // Get the UserPrintTicket.  We don't need it, but fetching it has a side-effect
    // of initializing the PrinterThunkHandler.  In some cases (e.g. Win7 printing
    // to XPS printer), this doesn't happen any other way so calling this method
    // would get NullReferenceException (Dev11 780899).
    PrintTicket ^userPrintTicket = UserPrintTicket;
    if (userPrintTicket == nullptr)     // keep the compiler from optimizing away the previous call
    {
        userPrintTicket = printTicket;  // no real effect
    }

    // Note: in the other overloads of AddJob we defaulted to either the
    // UserTicket or the DefaultTicket.  We intentionally do not fallback to
    // using those tickets if the caller passed in a null ticket to allow the
    // caller to create a print job without a ticket.  This will have the
    // consequence on >= win8 that the JobID will not be available, but
    // it allows the caller to avoid the consequences of using a print ticket
    // that may specify incompatible settings with the print ticket in the
    // payload written to the print stream.
    PrintSystemJobInfo^ jobInfo = PrintSystemJobInfo::Add(this, jobName, document, fastCopy, printTicket);
    return jobInfo;
}


PrintSystemJobInfo^
PrintQueue::
GetJob(
    Int32   jobId
    )
{
    VerifyAccess();

    PrintSystemJobInfo^ jobInfo = PrintSystemJobInfo::Get(this, jobId);
    return jobInfo;
}

PrintJobInfoCollection^
PrintQueue::
GetPrintJobInfoCollection(
    void
    )
{
    VerifyAccess();

    return gcnew PrintJobInfoCollection(this,PrintSystemJobInfo::GetAllPropertiesFilter());
}

/*++
    Function Name:
        The following are the set of functions that
        set/get the PrintQueue properties

    Description:
        set/get Priority:           A priority value that the spooler
                                    uses to route print jobs.
        set/get DefaultPriority:    A default priority value assigned to
                                    each print job.
        set/get StartTime:          The earliest time at which the PrintQueue
                                    will print a job. This value is expressed
                                    as minutes elapsed since 12:00 AM GMT
                                    (Greenwich Mean Time).
        set/get UntilTime:          The latest time at which the PritnQueue will
                                    print a job. This value is expressed as
                                    minutes elapsed since 12:00 AM GMT
                                    (Greenwich Mean Time).
        set/get AveragePPM          Specifies the average number of pages per
                                    minute that have been printed on the PrintQueue
        get NumberOfJobs            Queries the number of print jobs that have
                                    been queued for the PrintQueue.
        set/get ShareName           The sharepoint for the PrintQueue.
        set/get Comment             A brief description of the PrintQueue.
        set/get Location            The physical location of the PrintQueue (for
                                    example, "Bldg. 38, Room 1164").
        set/get Description         Description of the contents of the structure
        set/get SepFile             The file used to create the separator page.
                                    This page is used to separate print jobs sent
                                    to the PrintQueue.
        set/get QueueDriver         The PrintQueue driver
        set/get QueuePort           The port used to transmit data to the PrintQueue
        set/get QueuePrintProcessor The print processor
        set/get DefaultPrintTicket  The global default PrintQueue print ticket.
        set/get UserPrintTicket     The current user print ticket
        get     QueueStatus         Thhe PrintQueue Status
        get     QueueAttributes

    Parameters:
        Either new values or None

    Return Value
        Either PrintQueue Properties or None
--*/
Int32
PrintQueue::Priority::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("Priority","Priority");
    return priority;
}


void
PrintQueue::Priority::
set(
    Int32   requiredPriority
    )
{
    VerifyAccess();

    if(priority != requiredPriority)
    {
        priority = requiredPriority;

        PropertiesCollection->GetProperty("Priority")->Value = priority;
    }
}


Int32
PrintQueue::DefaultPriority::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("DefaultPriority","DefaultPriority");
    return defaultPriority;
}


void
PrintQueue::DefaultPriority::
set(
    Int32 requiredDefaultPriority
    )
{
    VerifyAccess();

    if(defaultPriority != requiredDefaultPriority)
    {
        defaultPriority = requiredDefaultPriority;

        PropertiesCollection->GetProperty("DefaultPriority")->Value = defaultPriority;
    }
}


Int32
PrintQueue::StartTimeOfDay::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("StartTimeOfDay","StartTimeOfDay");
    return startTime;
}


void
PrintQueue::StartTimeOfDay::
set(
    Int32 requiredStartTime
    )
{
    VerifyAccess();

    if(startTime != requiredStartTime)
    {
        startTime = requiredStartTime;

        PropertiesCollection->GetProperty("StartTimeOfDay")->Value = startTime;
    }
}


Int32
PrintQueue::UntilTimeOfDay::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("UntilTimeOfDay","UntilTimeOfDay");
    return untilTime;
}


void
PrintQueue::UntilTimeOfDay::
set(
    Int32   requiredUntilTime
    )
{
    VerifyAccess();

    if(untilTime != requiredUntilTime)
    {
        untilTime = requiredUntilTime;

        PropertiesCollection->GetProperty("UntilTimeOfDay")->Value = untilTime;
    }
}


Int32
PrintQueue::AveragePagesPerMinute::
get(
    void
    )
{
    VerifyAccess();
    GetUnInitializedData("AveragePagesPerMinute","AveragePagesPerMinute");
    return averagePagesPerMinute;
}

Int32
PrintQueue::NumberOfJobs::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("NumberOfJobs","NumberOfJobs");
    return numberOfJobs;
}


Boolean
PrintQueue::InPartialTrust::
get(
    void
    )
{
    VerifyAccess();

    return runsInPartialTrust;
}

void
PrintQueue::InPartialTrust::
set(
    Boolean isPT
    )
{
    VerifyAccess();
    runsInPartialTrust = isPT;
}



String^
PrintQueue::ShareName::
get(
void
)
{
    VerifyAccess();

    GetUnInitializedData("ShareName","ShareName");
    return shareName;
}


void
PrintQueue::ShareName::
set(
    String^ newShareName
    )
{
    VerifyAccess();

    if(shareName != newShareName ||
       (newShareName &&
        !newShareName->Equals(shareName)))
    {
        shareName = newShareName;

        PropertiesCollection->GetProperty("ShareName")->Value = shareName;

        if (!PropertiesCollection->GetProperty("ShareName")->IsInternallyInitialized)
        {

            Int32 attributes = Int32(queueAttributes);

            if (shareName != nullptr)
            {
                attributes |= static_cast<Int32>(PrintQueueAttributes::Shared);
            }
            else
            {
                attributes &= ~static_cast<Int32>(PrintQueueAttributes::Shared);
            }

            get_InternalPropertiesCollection("Attributes")->GetProperty("Attributes")->Value = attributes;
            PropertiesCollection->GetProperty("QueueAttributes")->IsDirty = true;
        }
    }
}



String^
PrintQueue::Comment::
get(
    void
    )
{
    VerifyAccess();
    GetUnInitializedData("Comment","Comment");
    return comment;
}


void
PrintQueue::Comment::
set(
    String^ newComment
    )
{
    VerifyAccess();

    if(comment != newComment ||
       (newComment &&
        !newComment->Equals(comment)))
    {
        comment = newComment;

        PropertiesCollection->GetProperty("Comment")->Value = comment;
    }
}


String^
PrintQueue::Description::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("Description","Description");
    return description;
}


void
PrintQueue::Description::
set(
    String^ newDescription
    )
{
    VerifyAccess();

    if(description != newDescription ||
       (newDescription &&
        !newDescription->Equals(description)))
    {
        description = newDescription;

        PropertiesCollection->GetProperty("Description")->Value = description;
    }
}


String^
PrintQueue::Location::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("Location","Location");
    return location;
}


void
PrintQueue::Location::
set(
    String^ newLocation
    )
{
    VerifyAccess();

    if(location != newLocation ||
       (newLocation &&
        !newLocation->Equals(location)))
    {
        location = newLocation;

        PropertiesCollection->GetProperty("Location")->Value = location;
    }
}

void
PrintQueue::Name::
set(
    String^ name
    )
{
    Boolean mustResetInternalInitialization = false;

    if(PrintSystemObject::Name::get() != name ||
       (name&&
       !name->Equals(PrintSystemObject::Name::get())))
    {
        //
        // If the name is a UNC name revert to the pritner name
        // after stripping the server part
        //
        Boolean isPrinterConnection = PrintSystemUNCPathResolver::ValidateUNCPath(name);

        if(isPrinterConnection == true)
        {
            if(PropertiesCollection->GetProperty("Name")->IsInternallyInitialized)
            {
                mustResetInternalInitialization = true;
            }
            PrintSystemUNCPathCracker^ cracker = gcnew PrintSystemUNCPathCracker(name);
            name = cracker->PrintQueueName;

            if(!hostingPrintServer)
            {
                if(isBrowsable)
                {
                    hostingPrintServer = gcnew PrintServer(cracker->PrintServerName,PrintServerType::Browsable);
                }
                else
                {
                    hostingPrintServer = gcnew PrintServer(cracker->PrintServerName);
                }
            }
        }
        else
        {
            if(!hostingPrintServer)
            {
                hostingPrintServer = gcnew PrintServer();
            }
        }

        if(!fullQueueName)
        {
            fullQueueName = PrepareNameForDownLevelConnectivity(hostingPrintServer->Name,name);
        }

        PrintSystemObject::Name::set(name);

        PropertiesCollection->GetProperty("Name")->Value = PrintSystemObject::Name::get();
    }

    if(mustResetInternalInitialization)
    {
        PropertiesCollection->GetProperty("Name")->IsInternallyInitialized = true;
    }
}


String^
PrintQueue::Name::
get(
    void
    )
{
    return PrintSystemObject::Name::get();
}

String^
PrintQueue::SeparatorFile::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("SeparatorFile","SeparatorFile");
    return separatorFile;
}


void
PrintQueue::SeparatorFile::
set(
    String^   newSeparatorFile
    )
{
    VerifyAccess();

    if(separatorFile != newSeparatorFile ||
       (newSeparatorFile &&
        !newSeparatorFile->Equals(newSeparatorFile)))
    {
        separatorFile = newSeparatorFile;

        PropertiesCollection->GetProperty("SeparatorFile")->Value = separatorFile;
    }
}


PrintTicket^
PrintQueue::UserPrintTicket::
get(
    void
    )
{
    VerifyAccess();

    if (userPrintTicket == nullptr)
    {

        GetUnInitializedData("UserPrintTicket","UserDevMode");

        if (userDevMode != nullptr)
        {
            if (printTicketManager == nullptr)
            {
                printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
            }

            PropertiesCollection->GetProperty("UserPrintTicket")->IsInternallyInitialized = true;
            PropertiesCollection->GetProperty("UserPrintTicket")->Value = printTicketManager->ConvertDevModeToPrintTicket(userDevMode);
            delete userDevMode;
            userDevMode = nullptr;
        }
    }
    return userPrintTicket;

}


void
PrintQueue::UserPrintTicket::
set(
    PrintTicket^ newUserPrintTicket
    )
{
    VerifyAccess();

    if(userPrintTicket != newUserPrintTicket)
    {
        userPrintTicket = newUserPrintTicket;

        //
        // Set the value for downlevel thunking
        //
        if(!PropertiesCollection->GetProperty("UserPrintTicket")->IsInternallyInitialized)
        {
            if (printTicketManager == nullptr)
            {
                printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
            }

            array<Byte>^ devMode = printTicketManager->ConvertPrintTicketToDevMode(userPrintTicket, BaseDevModeType::UserDefault);
            get_InternalPropertiesCollection("UserDevMode")->GetProperty("UserDevMode")->Value = devMode;
        }
        //
        // Set the managed property
        //
        PropertiesCollection->GetProperty("UserPrintTicket")->Value = userPrintTicket;
    }
}


PrintTicket^
PrintQueue::DefaultPrintTicket::
get(
    void
    )
{
    VerifyAccess();

    if (defaultPrintTicket == nullptr)
    {
        GetUnInitializedData("DefaultPrintTicket", "DefaultDevMode");

        if (defaultDevMode != nullptr)
        {
            if (printTicketManager == nullptr)
            {
                printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
            }

            PropertiesCollection->GetProperty("DefaultPrintTicket")->IsInternallyInitialized = true;
            PropertiesCollection->GetProperty("DefaultPrintTicket")->Value = printTicketManager->ConvertDevModeToPrintTicket(defaultDevMode);
            delete defaultDevMode;
            defaultDevMode = nullptr;
        }
    }

    return defaultPrintTicket;

}


void
PrintQueue::DefaultPrintTicket::
set(
    PrintTicket^ newDefaultPrintTicket
    )
{
    VerifyAccess();

    if(defaultPrintTicket != newDefaultPrintTicket)
    {
        defaultPrintTicket = newDefaultPrintTicket;

        //
        // Set the value for downlevel thunking
        //

        if(!PropertiesCollection->GetProperty("DefaultPrintTicket")->IsInternallyInitialized)
        {
            if (printTicketManager == nullptr)
            {
                printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
            }

            array<Byte>^ devMode = printTicketManager->ConvertPrintTicketToDevMode(defaultPrintTicket, BaseDevModeType::PrinterDefault);
            get_InternalPropertiesCollection("DefaultDevMode")->GetProperty("DefaultDevMode")->Value = devMode;
        }
        //
        // Set the managed property
        //
        PropertiesCollection->GetProperty("DefaultPrintTicket")->Value = defaultPrintTicket;
    }
}

PrintJobSettings^
PrintQueue::CurrentJobSettings::
get(
    void
    )
{
    VerifyAccess();
    if(_currentJobSettings == nullptr)
    {
        _currentJobSettings = gcnew PrintJobSettings(this->UserPrintTicket);
    }
    return _currentJobSettings;
}

PrintDriver^
PrintQueue::QueueDriver::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueDriver","QueueDriverName");
    return queueDriver;
}

void
PrintQueue::QueueDriver::
set(
    PrintDriver^ newDriver
    )
{
    VerifyAccess();

    if(queueDriver != newDriver)
    {
        queueDriver = newDriver;
        //
        // Set the value for downlevel thunking
        //
        if(!PropertiesCollection->GetProperty("QueueDriver")->IsInternallyInitialized)
        {
            get_InternalPropertiesCollection("QueueDriverName")->GetProperty("QueueDriverName")->Value = queueDriver->Name;
        }
        //
        // Set the Managed Property
        //
        PropertiesCollection->GetProperty("QueueDriver")->Value = newDriver;
    }
}


PrintPort^
PrintQueue::QueuePort::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueuePort","QueuePortName");
    return queuePort;
}


void
PrintQueue::QueuePort::
set(
    PrintPort^ newPort
    )
{
    VerifyAccess();

    if(queuePort != newPort)
    {
        queuePort = newPort;
        //
        // Set the value for downlevel thunking
        //
        if(!PropertiesCollection->GetProperty("QueuePort")->IsInternallyInitialized)
        {
            get_InternalPropertiesCollection("QueuePortName")->GetProperty("QueuePortName")->Value = queuePort->Name;
        }
        //
        // Set the Managed Property
        //
        PropertiesCollection->GetProperty("QueuePort")->Value = newPort;
    }
}


PrintProcessor^
PrintQueue::QueuePrintProcessor::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueuePrintProcessor","QueuePrintProcessorName");
    return queuePrintProcessor;
}


void
PrintQueue::QueuePrintProcessor::
set(
    PrintProcessor^ newPrintProcessor
    )
{
    VerifyAccess();

    if(queuePrintProcessor != newPrintProcessor)
    {
        queuePrintProcessor = newPrintProcessor;
        //
        // Set the value for downlevel thunking
        //
        if(!PropertiesCollection->GetProperty("QueuePrintProcessor")->IsInternallyInitialized)
        {
            get_InternalPropertiesCollection("QueuePrintProcessorName")->
            GetProperty("QueuePrintProcessorName")->Value = queuePrintProcessor->Name;
        }
        //
        // Set the Managed Property
        //
        PropertiesCollection->GetProperty("QueuePrintProcessor")->Value = newPrintProcessor;
    }
}


PrintServer^
PrintQueue::HostingPrintServer::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("HostingPrintServer","HostingPrintServerName");

    return hostingPrintServer;
}


String^
PrintQueue::FullName::
get(
    void
    )
{
    VerifyAccess();

    return fullQueueName;
}


void
PrintQueue::HostingPrintServer::
set(
    PrintServer^ printServer
    )
{
    VerifyAccess();

    if(hostingPrintServer != printServer)
    {
        hostingPrintServer = printServer;
        PropertiesCollection->GetProperty("HostingPrintServer")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("HostingPrintServer")->Value = printServer;
    }
}


PrintQueueStatus
PrintQueue::QueueStatus::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return queueStatus;
}

/*++
    Function Name:
        set_Status

    Description:
        In the unmanaged world, status is a 32-bit value, but in
        the managed world status is distributed over a number of
        Boolean values each representing one individual PrintQueue
        status like {PrintQueue Paused, PrintQueue suffering paper
        jam ...} and it is this function that converts the unmanaged
        representation to the managed one. Moreover it updates the
        named property of the collection.

    Parameters:
        Int32:  The 32-bit PrintQueue status

    Return Value
        None
--*/
void
PrintQueue::Status::
set(
    Int32 status
    )
{
    queueStatus = static_cast<PrintQueueStatus>(status);

    PropertiesCollection->GetProperty("QueueStatus")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("QueueStatus")->Value = queueStatus;

    isPaused             = (status & (static_cast<Int32>(PrintQueueStatus::Paused))) ? true : false;
    isInError            = (status & (static_cast<Int32>(PrintQueueStatus::Error))) ? true : false;
    isPendingDeletion    = (status & (static_cast<Int32>(PrintQueueStatus::PendingDeletion))) ? true : false;
    isPaperJammed        = (status & (static_cast<Int32>(PrintQueueStatus::PaperJam))) ? true : false;
    isOutOfPaper         = (status & (static_cast<Int32>(PrintQueueStatus::PaperOut))) ? true : false;
    isManualFeedRequired = (status & (static_cast<Int32>(PrintQueueStatus::ManualFeed))) ? true : false;
    hasPaperProblem      = (status & (static_cast<Int32>(PrintQueueStatus::PaperProblem))) ? true : false;
    isOffline            = (status & (static_cast<Int32>(PrintQueueStatus::Offline))) ? true : false;
    isIOActive           = (status & (static_cast<Int32>(PrintQueueStatus::IOActive))) ? true : false;
    isBusy               = (status & (static_cast<Int32>(PrintQueueStatus::Busy))) ? true : false;
    isPrinting           = (status & (static_cast<Int32>(PrintQueueStatus::Printing))) ? true : false;
    isOutputBinFull      = (status & (static_cast<Int32>(PrintQueueStatus::OutputBinFull))) ? true : false;
    isNotAvailable       = (status & (static_cast<Int32>(PrintQueueStatus::NotAvailable))) ? true : false;
    isWaiting            = (status & (static_cast<Int32>(PrintQueueStatus::Waiting))) ? true : false;
    isProcessing         = (status & (static_cast<Int32>(PrintQueueStatus::Processing))) ? true : false;
    isInitializing       = (status & (static_cast<Int32>(PrintQueueStatus::Initializing))) ? true : false;
    isWarmingUp          = (status & (static_cast<Int32>(PrintQueueStatus::WarmingUp))) ? true : false;
    isTonerLow           = (status & (static_cast<Int32>(PrintQueueStatus::TonerLow))) ? true : false;
    hasNoToner           = (status & (static_cast<Int32>(PrintQueueStatus::NoToner))) ? true : false;
    doPagePunt           = (status & (static_cast<Int32>(PrintQueueStatus::PagePunt))) ? true : false;
    needUserIntervention = (status & (static_cast<Int32>(PrintQueueStatus::UserIntervention))) ? true : false;
    isOutOfMemory        = (status & (static_cast<Int32>(PrintQueueStatus::OutOfMemory))) ? true : false;
    isDoorOpened         = (status & (static_cast<Int32>(PrintQueueStatus::DoorOpen))) ? true : false;
    isServerUnknown      = (status & (static_cast<Int32>(PrintQueueStatus::ServerUnknown))) ? true : false;
    isPowerSaveOn        = (status & (static_cast<Int32>(PrintQueueStatus::PowerSave))) ? true : false;
}


PrintQueueAttributes
PrintQueue::QueueAttributes::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return queueAttributes;
}


/*++
    Function Name:
        set_Attributes

    Description:
        In the unmanaged world, attributes is a 32-bit value, but in
        the managed world attributes are distributed over a number of
        Boolean values each representing one individual PrintQueue
        attribute like {PrintQueue Shared, PrintQueue allows printing
        while spooling ...} and it is this function that converts the
        unmanaged representation to the managed one. Moreover it
        updates the named property of the collection.

    Parameters:
        Int32:  The 32-bit PrintQueue attributes

    Return Value
        None
--*/
void
PrintQueue::Attributes::
set(
    Int32 attributes
    )
{
    queueAttributes= static_cast<PrintQueueAttributes>(attributes);

    PropertiesCollection->GetProperty("QueueAttributes")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("QueueAttributes")->Value = queueAttributes;

    isQueued                        = (attributes & (static_cast<Int32>(PrintQueueAttributes::Queued))) ? true : false;
    isDirect                        = (attributes & (static_cast<Int32>(PrintQueueAttributes::Direct))) ? true : false;
    isShared                        = (attributes & (static_cast<Int32>(PrintQueueAttributes::Shared))) ? true : false;
    isHidden                        = (attributes & (static_cast<Int32>(PrintQueueAttributes::Hidden))) ? true : false;
    isDevQueryEnabled               = (attributes & (static_cast<Int32>(PrintQueueAttributes::EnableDevQuery))) ? true : false;
    arePrintedJobsKept              = (attributes & (static_cast<Int32>(PrintQueueAttributes::KeepPrintedJobs))) ? true : false;
    areCompletedJobsScheduledFirst  = (attributes & (static_cast<Int32>(PrintQueueAttributes::ScheduleCompletedJobsFirst))) ? true : false;
    isBidiEnabled                   = (attributes & (static_cast<Int32>(PrintQueueAttributes::EnableBidi))) ? true : false;
    isRawOnlyEnabled                = (attributes & (static_cast<Int32>(PrintQueueAttributes::RawOnly))) ? true : false;
    isPublished                     = (attributes & (static_cast<Int32>(PrintQueueAttributes::Published))) ? true : false;
}


/*++
    Function Name:
        A set of Boolean methods

    Description:
        Those methods return the Boolean representation
        of the PrintQueue individual attributes and status
        bits.

    Parameters:
        None

    Return Value
        Boolean: Status OR Attribute
--*/
bool
PrintQueue::IsPaused::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isPaused;
}


bool
PrintQueue::IsInError::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isInError;
}


bool
PrintQueue::IsPendingDeletion::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isPendingDeletion;
}


bool
PrintQueue::IsPaperJammed::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isPaperJammed;
}


bool
PrintQueue::IsOutOfPaper::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isOutOfPaper;
}


bool
PrintQueue::IsManualFeedRequired::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isManualFeedRequired;
}


bool
PrintQueue::HasPaperProblem::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return hasPaperProblem;
}


bool
PrintQueue::IsOffline::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isOffline;
}


bool
PrintQueue::IsIOActive::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isIOActive;
}


bool
PrintQueue::IsBusy::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isBusy;
}


bool
PrintQueue::IsPrinting::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isPrinting;
}


bool
PrintQueue::IsOutputBinFull::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isOutputBinFull;
}


bool
PrintQueue::IsNotAvailable::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isNotAvailable;
}


bool
PrintQueue::IsWaiting::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isWaiting;
}


bool
PrintQueue::IsProcessing::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isProcessing;
}


bool
PrintQueue::IsInitializing::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isInitializing;
}


bool
PrintQueue::IsWarmingUp::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isWarmingUp;
}


bool
PrintQueue::IsTonerLow::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isTonerLow;
}


bool
PrintQueue::HasToner::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return !hasNoToner;
}


bool
PrintQueue::PagePunt::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return doPagePunt;
}


bool
PrintQueue::NeedUserIntervention::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return needUserIntervention;
}


bool
PrintQueue::IsOutOfMemory::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isOutOfMemory;
}


bool
PrintQueue::IsDoorOpened::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isDoorOpened;
}


bool
PrintQueue::IsServerUnknown::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isServerUnknown;
}


bool
PrintQueue::IsPowerSaveOn::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueStatus","Status");
    return isPowerSaveOn;
}


Boolean
PrintQueue::IsQueued::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isQueued;
}


Boolean
PrintQueue::IsDirect::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isDirect;
}


Boolean
PrintQueue::IsShared::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isShared;
}


Boolean
PrintQueue::IsHidden::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isHidden;
}


Boolean
PrintQueue::IsDevQueryEnabled::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isDevQueryEnabled;
}


Boolean
PrintQueue::KeepPrintedJobs::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return arePrintedJobsKept;
}


Boolean
PrintQueue::ScheduleCompletedJobsFirst::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return areCompletedJobsScheduledFirst;
}


Boolean
PrintQueue::IsBidiEnabled::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isBidiEnabled;
}


Boolean
PrintQueue::IsRawOnlyEnabled::
get(
    void
    )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isRawOnlyEnabled;
}


Boolean
PrintQueue::IsPublished::
get(
    void
   )
{
    VerifyAccess();

    GetUnInitializedData("QueueAttributes","Attributes");
    return isPublished;
}

Boolean
PrintQueue::
GetIsXpsDevice(
    void
    )
{
    Boolean printerIsXpsDevice = false;

    try
    {
        if (isBrowsable)
        {
            ActivateBrowsableQueue();
            isBrowsable = false;
        }

        printerIsXpsDevice = printerThunkHandler->ThunkIsMetroDriverEnabled();

    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult,
                                        "PrintSystemException.PrintQueue.XpsDeviceQuery");
    }
    return printerIsXpsDevice;
}

Boolean
PrintQueue::IsXpsDevice::
get(
    void
    )
{
    VerifyAccess();

    if (!PropertiesCollection->GetProperty("IsXpsEnabled")->IsInitialized)
    {
        isXpsDevice = this->GetIsXpsDevice();
        PropertiesCollection->GetProperty("IsXpsEnabled")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("IsXpsEnabled")->Value = isXpsDevice;
    }
    return isXpsDevice;
}

void
PrintQueue::IsXpsDevice::
set(
    Boolean   isXpsEnabled
    )
{
    VerifyAccess();

    isXpsDevice = isXpsEnabled;
}

PrinterThunkHandlerBase^
PrintQueue::
CreatePrintThunkHandler(
    void
    )
{
    if(IsXpsDeviceSimulationSupported())
    {
        return gcnew XpsDeviceSimulatingPrintThunkHandler(this->FullName);
    }
    else
    {
        return PrinterThunkHandler->DuplicateHandler();
    }
}

Boolean
PrintQueue::
IsXpsDeviceSimulationSupported(
    void
    )
{
    return (IsXpsOMPrintingSupported() || PresentationNativeUnsafeNativeMethods::IsStartXpsPrintJobSupported());
}

Boolean
PrintQueue::
IsXpsOMPrintingDisabled(
void
)
{
    bool isXpsOMPrintingDisabled = false;

    try
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();
        System::Globalization::CultureInfo^ culture = System::Threading::Thread::CurrentThread->CurrentUICulture;
        String^ regKeyBasePath = manager->GetString("RegKeyBasePath", culture);
        String^ useXPSOMPrintingRegValue = manager->GetString("PrintSystemJobInfo_disableXPSOMPrinting_RegValue", culture);

        DWORD result = 0;
        Object^ objValue = Microsoft::Win32::Registry::GetValue(regKeyBasePath, useXPSOMPrintingRegValue, result);
        if (objValue != nullptr && dynamic_cast<Int32^>(objValue))
        {
            DWORD result = safe_cast<Int32>(objValue);
            if (result != 0)
            {
                isXpsOMPrintingDisabled = true;
            }
        }

        XpsOMPrintingTraceLogger::LogXpsOMStatus(!isXpsOMPrintingDisabled);
    }
    // Registry Key may be in the middle of deletion
    catch (IOException^)
    {
    }

    return isXpsOMPrintingDisabled;
}

Boolean
PrintQueue::
IsXpsOMPrintingSupported(
void
)
{
    static bool isXpsOMPrintingSupported = !PrintQueue::IsMxdwLegacyDriver(this) &&
                                            PresentationNativeUnsafeNativeMethods::IsPrintPackageTargetSupported() &&
                                            !IsXpsOMPrintingDisabled();
                                            
    return isXpsOMPrintingSupported;
}

/*++
    Function Name:
        set_HostingPrintServerName

    Description:
        Coming from the downlevel unmanaged code, we get
        a print server name and not an object. This code
        is running internally from the thunk layer up to
        the managed object.

    Parameters:
        String:     Print Server Name

    Return Value
        None
--*/
void
PrintQueue::HostingPrintServerName::
set(
    String^ serverName
    )
{
    hostingPrintServerName = serverName;
    //
    // If there is no Print Server object created
    // within this print queue, then this means that
    // we are dealing with one of the browsable objects
    // and we should create one.
    // Since this is a special path, we might need
    // to create those PrintSErver objects in a different
    // manner, than the normal path --> TBD
    //
    if(!hostingPrintServer)
    {
        HostingPrintServer = gcnew PrintServer(serverName,PrintServerType::Browsable);
    }
    else
    {
        //
        // assuming that the PrintServer was instantiated
        // for a local server and the initial name is set
        // to NULL, then update with the name you get from the
        // server
        //
        if(!hostingPrintServer->Name ||
            hostingPrintServer->Name->Length == 0)
        {
            hostingPrintServer->IsInternallyInitialized = true;
            hostingPrintServer->Name = serverName;
        }
    }
}


/*++
    Function Name:
        get_InternalPropertiesCollection

    Description:
        For any of the managed objects, a consumer can
        access a property either by the compile time name
        or by a property collection as a named property.
        For the later, the names vary whether it is coming in
        to the object from a managed consumer or from the
        thunking code bubling up in the managed world.
        So to solve this problem I supplied 2 lists of named
        properties and each list is maintained within its own
        collection. So when there is a difference in the name
        between the managed and downlevel property (usually the
        difference is mandated by a type difference) a call to
        this function would return the collection which handles
        the given name

    Parameters:
        String:     name of property at hand

    Return Value
        Collection handling the property
--*/
PrintPropertyDictionary^
PrintQueue::
get_InternalPropertiesCollection(
    String^ attributeName
    )
{
    return (PrintPropertyDictionary^)collectionsTable[attributeName];
}

/*++
    Function Name:
        A number of Set Functions.

    Description:
        Those functions help setting the unmanaged property name
        in the appropriate collection, so that the thunking layer
        can digest those with their appropriate types.
        To give some examples

                managed                    unmanaged
                -------                    ---------
        QueueDriver(Type->Driver)   | DriverName(Type->String)
        QueuePort(Type->Port)       | PortName(Type->String)
        DefaultPrintTicket(Type->JT)| DefaultDevMode(Type Byte[])

    Parameters:
        depends on the downlevel type

    Return Value
        None
--*/
void
PrintQueue::QueueDriverName::
set(
    String^ driverName
    )
{
    if(get_InternalPropertiesCollection("QueueDriverName")->GetProperty("QueueDriverName")->IsInternallyInitialized)
    {
        PropertiesCollection->GetProperty("QueueDriver")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("QueueDriver")->Value = gcnew PrintDriver(driverName);
    }
}


void
PrintQueue::QueuePrintProcessorName::
set(
    String^ printProcessorName
    )
{
    if(get_InternalPropertiesCollection("QueuePrintProcessorName")->GetProperty("QueuePrintProcessorName")->IsInternallyInitialized)
    {
        PropertiesCollection->GetProperty("QueuePrintProcessor")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("QueuePrintProcessor")->Value = gcnew PrintProcessor(printProcessorName);
    }
}


void
PrintQueue::NumberOfJobs::
set(
    int numOfJobs
    )
{
    VerifyAccess();

    numberOfJobs = numOfJobs;
    PropertiesCollection->GetProperty("NumberOfJobs")->IsInternallyInitialized = true;
    PropertiesCollection->GetProperty("NumberOfJobs")->Value = numberOfJobs;
}


void
PrintQueue::DefaultDevMode::
set(
    array<Byte>^ devMode
    )
{
    defaultDevMode     = devMode;
    defaultPrintTicket = nullptr;
}


void
PrintQueue::UserDevMode::
set(
    array<Byte>^ devMode
    )
{
    userDevMode     = devMode;
    userPrintTicket = nullptr;
}


void
PrintQueue::QueuePortName::
set(
    String^ portName
    )
{
    if(get_InternalPropertiesCollection("QueuePortName")->GetProperty("QueuePortName")->IsInternallyInitialized)
    {
        PropertiesCollection->GetProperty("QueuePort")->IsInternallyInitialized = true;
        PropertiesCollection->GetProperty("QueuePort")->Value = gcnew PrintPort(portName);
    }
}


/*++
    Function Name:
        Commit

    Description:
        The way the APIs work, is that individual properties
        are set independently and then the whole list of set
        properties is commited all at once.
        TBD: If a commit partially fails / succeedes, then
        the original value of the variable before the set
        would be retained.

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
Commit(
    void
    )
{
    VerifyAccess();

    PrintWin32Thunk::SetDataThunkObject^ setDataThunkObject = nullptr;
    try
    {
        if (isBrowsable)
        {
            ActivateBrowsableQueue();
            isBrowsable = false;
        }

        setDataThunkObject = gcnew PrintWin32Thunk::SetDataThunkObject(this->GetType());

        array<String^>^     alteredPropertiesFilter = nullptr;
        StringCollection^   mappedStringCollection = gcnew StringCollection();

        setDataThunkObject->CommitDataFromPrintSystemObject(printerThunkHandler,
                                                            this,
                                                            (alteredPropertiesFilter = GetAlteredPropertiesFilter(mappedStringCollection)));
        //
        // Reset the dirty bits in the altered attributes
        //
        if(alteredPropertiesFilter != nullptr)
        {
            for(Int32 alteredPropertiesIndex = 0;
                alteredPropertiesIndex < alteredPropertiesFilter->Length;
                alteredPropertiesIndex++)
            {
                PrintPropertyDictionary^ dictionary = nullptr;
                (dictionary = get_InternalPropertiesCollection(alteredPropertiesFilter[alteredPropertiesIndex]))->
                GetProperty(alteredPropertiesFilter[alteredPropertiesIndex])->IsDirty = false;

                if(dictionary != PropertiesCollection)
                {
                    //
                    // This means that we are dealing with a downlevel property & so we
                    // have to set also the dirty bit of the uplevel property
                    //
                    String^ mappedString;
                    PropertiesCollection->GetProperty(mappedString = mappedStringCollection[0])->IsDirty = false;
                    mappedStringCollection->RemoveAt(0);
                }
            }
        }
        //
        // Making sure that the Full Name reflects the current name
        //
        fullQueueName = PrepareNameForDownLevelConnectivity(hostingPrintServer->Name,Name);
    }
    catch (PrintCommitAttributesException^ e)
    {
        throw gcnew PrintCommitAttributesException(Marshal::GetHRForException(e),
                                                 "PrintSystemException.PrintQueue.Commit",
                                                 e->CommittedAttributesCollection,
                                                 e->FailedAttributesCollection,
                                                 Name);
    }
    __finally
    {
        delete setDataThunkObject;
        setDataThunkObject = nullptr;
    }
}


/*++
    Function Name:
        Refresh

    Description:
        This method helps in refreshing the state of the object.
        Only those properties that where either requested during
        initialization or requested later on during individual
        gets are the ones refreshed.

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
Refresh(
    void
    )
{
    VerifyAccess();

    GetDataThunkObject^ dataThunkObject = nullptr;

    try
    {
        if (isBrowsable)
        {
            ActivateBrowsableQueue();
            isBrowsable = false;
        }

        dataThunkObject = gcnew GetDataThunkObject(this->GetType());

        dataThunkObject->PopulatePrintSystemObject(printerThunkHandler,
                                                    this,
                                                    refreshPropertiesFilter);
        //
        // Making sure that the Full Name reflects the current name
        //
        fullQueueName = PrepareNameForDownLevelConnectivity(hostingPrintServer->Name,Name);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintQueueException(internalException->HResult, "PrintSystemException.PrintQueue.Refresh");
    }
    __finally
    {
        delete dataThunkObject;
    }
}

/*++
    Function Name:
        GetAllPropertiesFilter

    Description:
        The method populates a String Array of all
        possible properties of the PrintQueue object.

    Parameters:
        None

    Return Value
        String[]:   All the properties supported by
                    the PrintQueue
--*/
array<String^>^
PrintQueue::
GetAllPropertiesFilter(
    void
    )
{
    //
    // Properties = Base Class Properties + Inherited Class Properties
    //
    array<String^>^ allPropertiesFilter = gcnew array<String^>(PrintSystemObject::BaseAttributeNames()->Length +
                                                PrintQueue::primaryAttributeNames->Length);

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
        numOfAttributes < PrintQueue::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        String^ upLevelAttribute = nullptr;

        if(String^ downLevelAttribute = (String^)(upLevelToDownLevelMapping
                                        [upLevelAttribute = PrintQueue::primaryAttributeNames[numOfAttributes]]))
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
        GetAllPropertiesFilter

    Description:
        The method populates a String Array of all
        properties requested by a given filter. The
        difference between the input and the returned,
        is that I have to account for the downlevel
        named properties used to thunk to the unmanaged
        code.

    Parameters:
        String[]:   The proeprties asked for by the consumer
                    through an input filter

    Return Value
        String[]:   The properties both managed and unmanaged
                    matching such input filter.
--*/
array<String^>^
PrintQueue::
GetAllPropertiesFilter(
    array<String^>^ propertiesFilter
    )
{
    if (propertiesFilter != nullptr)
    {
        array<String^>^ allPropertiesFilter = gcnew array<String^>(propertiesFilter->Length);

        for(Int32 numOfProperties = 0;
            numOfProperties < propertiesFilter->Length;
            numOfProperties++)
        {
            String^ upLevelAttribute = nullptr;

            if(String^ downLevelAttribute = (String^)(upLevelToDownLevelMapping
                                            [upLevelAttribute = propertiesFilter[numOfProperties]]))
            {
                allPropertiesFilter[numOfProperties] = downLevelAttribute;
            }
            else
            {
                allPropertiesFilter[numOfProperties] = upLevelAttribute;
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
    Function Name:
        GetAlteredPropertiesFilter

    Description:
        When properties are commited, those that only
        changed from their initial values are committed.
        It is the responsibility of this method to figure
        out those properties that changed

    Parameters:
        None

    Return Value
        String[]:   The properties that changed from their
                    initial state

--*/
array<String^>^
PrintQueue::
GetAlteredPropertiesFilter(
    StringCollection^ uplevelAttributes
    )
{
    Int32   indexInAlteredProperties = 0;
    Int32   indexInMappedProperties  = 0;
    //
    // Properties = Base Class Properties + Inherited Class Properties
    //
    array<String^>^ probePropertiesFilter = gcnew array<String^>(PrintSystemObject::BaseAttributeNames()->Length +
                                                                 PrintQueue::primaryAttributeNames->Length);

    array<String^>^ probeMappedPropertiesFilter = gcnew array<String^>(PrintSystemObject::BaseAttributeNames()->Length +
                                                                       PrintQueue::primaryAttributeNames->Length);
    //
    // As the PrintTicket interface changed from a Stream to an Object, it is possible
    // for a caller in our APIs to use the pattern printQueue->UserPrintTicket->Property->Value = XXXX.
    // Based on this, the PrintTicket is changing without setting a property on the Print Queue and hence
    // we have to internally make a call in the PrintTicket to see whether it was altered or not
    //
    if((userPrintTicket != nullptr) &&
       (PropertiesCollection->GetProperty("UserPrintTicket")->IsDirty == false))
    {
        if(userPrintTicket->IsSettingChanged)
        {
            PropertiesCollection->GetProperty("UserPrintTicket")->IsDirty = true;

            if (printTicketManager == nullptr)
            {
                printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
            }

            array<Byte>^ devMode = printTicketManager->ConvertPrintTicketToDevMode(userPrintTicket, BaseDevModeType::UserDefault);
            get_InternalPropertiesCollection("UserDevMode")->GetProperty("UserDevMode")->Value = devMode;
        }
    }

    if((defaultPrintTicket != nullptr)&&
       (PropertiesCollection->GetProperty("DefaultPrintTicket")->IsDirty == false))
    {
        if(defaultPrintTicket->IsSettingChanged)
        {
            PropertiesCollection->GetProperty("DefaultPrintTicket")->IsDirty = true;

            if (printTicketManager == nullptr)
            {
                printTicketManager = gcnew PrintTicketManager(fullQueueName,clientPrintSchemaVersion);
            }

            array<Byte>^ devMode = printTicketManager->ConvertPrintTicketToDevMode(defaultPrintTicket, BaseDevModeType::PrinterDefault);
            get_InternalPropertiesCollection("DefaultDevMode")->GetProperty("DefaultDevMode")->Value = devMode;
        }
    }

    //
    // First Add the altered Base Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintSystemObject::BaseAttributeNames()->Length;
        numOfAttributes++)
    {
        if(PropertiesCollection->GetProperty(PrintSystemObject::BaseAttributeNames()[numOfAttributes])->IsDirty)
        {
            probePropertiesFilter[indexInAlteredProperties++] = PrintSystemObject::BaseAttributeNames()[numOfAttributes];
        }
    }

    //
    // Then Add the altered Inherited Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintQueue::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        String^ upLevelAttribute = nullptr;

        if(PropertiesCollection->GetProperty(PrintQueue::primaryAttributeNames[numOfAttributes])->IsDirty)
        {
            if(String^ downLevelAttribute = (String^)(upLevelToDownLevelMapping
                                            [upLevelAttribute = PrintQueue::primaryAttributeNames[numOfAttributes]]))
            {
                probePropertiesFilter[indexInAlteredProperties++] = downLevelAttribute;
                probeMappedPropertiesFilter[indexInMappedProperties++] = upLevelAttribute;
            }
            else
            {
                probePropertiesFilter[indexInAlteredProperties++] = upLevelAttribute;
            }
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

    if(indexInMappedProperties)
    {
        for(Int32 indexOfMovedData = 0;
            indexOfMovedData < indexInMappedProperties;
            indexOfMovedData++)
        {
            uplevelAttributes->Add(probeMappedPropertiesFilter[indexOfMovedData]);
        }
    }


    return alteredPropertiesFilter;
}


/*++
    Function Name:
        RegisterAttributesNamesTypes

    Description:
        The way the APIs work is that every compile time
        property is linked internally to a named property.
        The named property is an attribute / value pair.
        This pair has a generic type inheriting form
        PrintProperty and the specific type is
        determined by the type of the compile time property.
        By registering the named property and giving it a type,
        later on it is pretty easy to determine which specific
        type should be assigned to this named property in the
        property collection.
        This generally applies for
        1. Base class properties
        2. Managed properties
        3. Properties required for unmanaged thunking

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
RegisterAttributesNamesTypes(
    void
    )
{
    //
    // Register the attributes of the base class first
    //
    PrintSystemObject::RegisterAttributesNamesTypes(PrintQueue::attributeNameTypes);
    //
    // Register the attributes of the current class
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintQueue::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintQueue::primaryAttributeNames[numOfAttributes],
                                PrintQueue::primaryAttributeTypes[numOfAttributes]);
    }

    for(Int32 numOfAttributes = 0;
        numOfAttributes < PrintQueue::secondaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(PrintQueue::secondaryAttributeNames[numOfAttributes],
                                PrintQueue::secondaryAttributeTypes[numOfAttributes]);
    }
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
        such factories to intantiate an instance of the PrintQueue

    Parameters:
        None

    Return Value
        PrintSystemObject:  An instance of a PrintQueue
--*/
PrintSystemObject^
PrintQueue::
Instantiate(
    array<String^>^ propertiesFilter
    )
{
    return gcnew PrintQueue(propertiesFilter);
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
        such factories to intantiate an instance of the PrintQueue
        and this instance would have an optimization of not requiring
        to create a PrintServer. The PrintServer would be passed in
        as a parameter

    Parameters:
        PrintServer:        Print Server to be set as the server hosting the
                            Print Queue
        PropertiesFilter:   The set of properties required to be visible on
                            that Print Queue

    Return Value
        PrintSystemObject:  An instance of a PrintQueue
--*/
PrintSystemObject^
PrintQueue::
InstantiateOptimized(
    Object^         printServer,
    array<String^>^ propertiesFilter
    )
{
    return gcnew PrintQueue((PrintServer^)printServer,
                            propertiesFilter);
}


/*++
    Function Name:
        CreateAttributeNoValue

    Description:
        When the internal collection of proeprties for an object is
        created, the way individual properties are added to that
        collection is through using a factory. The reason for using a
        factory, is that every object is delegated adding its properties
        to its internal collection. Reason for that is that the object
        best knows it properties and their types.

    Parameters:
        String: The name of the property

    Return Value
        PrintProperty:  The property created as an
                        Attribute / Value pair
--*/
PrintProperty^
PrintQueue::
CreateAttributeNoValue(
    String^ attributeName
    )
{
    Type^ type = (Type^)PrintQueue::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName);
}


/*++
    Function Name:
        CreateAttributeValue

    Description:
        When the internal collection of proeprties for an object is
        created, the way individual properties are added to that
        collection is through using a factory. The reason for using a
        factory, is that every object is delegated adding its properties
        to its internal collection. Reason for that is that the object
        best knows it properties and their types.

    Parameters:
        String: The name of the property
        Object: The value of the property

    Return Value
        PrintProperty:  The property created as an
                        Attribute / Value pair
--*/
PrintProperty^
PrintQueue::
CreateAttributeValue(
    String^ attributeName,
    Object^ attributeValue
    )
{
    Type^ type = (Type^)PrintQueue::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue);
}


/*++
    Function Name:
        CreateAttributeNoValueLinked

    Description:
        When the internal collection of proeprties for an object is
        created, the way individual properties are added to that
        collection is through using a factory. The reason for using a
        factory, is that every object is delegated adding its properties
        to its internal collection. Reason for that is that the object
        best knows it properties and their types.

    Parameters:
        String:             The name of the property
        MulticastDelegate:  The delegate linking the named property to
                            a compile time property.

    Return Value
        PrintProperty:  The property created as an
                        Attribute / Value pair
--*/
PrintProperty^
PrintQueue::
CreateAttributeNoValueLinked(
    String^             attributeName,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintQueue::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,delegate);
}


/*++
    Function Name:
        CreateAttributeValueLinked

    Description:
        When the internal collection of proeprties for an object is
        created, the way individual properties are added to that
        collection is through using a factory. The reason for using a
        factory, is that every object is delegated adding its properties
        to its internal collection. Reason for that is that the object
        best knows it properties and their types.

    Parameters:
        String:             The name of the property
        Object:             The value of the property
        MulticastDelegate:  The delegate linking the named property to
                            a compile time property.

    Return Value
        PrintProperty:  The property created as an
                        Attribute / Value pair
--*/
PrintProperty^
PrintQueue::
CreateAttributeValueLinked(
    String^             attributeName,
    Object^             attributeValue,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)PrintQueue::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue,delegate);
}


/*++
    Function Name:
        InitializeInternalCollections

    Description:
        The function initialized the internal state of the object
        at instantiation time

    Parameters:
        None

    Return Value
        None
--*/
void
PrintQueue::
InitializeInternalCollections(
    void
    )
{

    accessVerifier = gcnew PrintSystemDispatcherObject();

    collectionsTable          = gcnew Hashtable();
    thunkPropertiesCollection = gcnew PrintPropertyDictionary();
    //
    // Initialize the PrintTickets held by the PrintQueue
    //
    InitializePrintTickets();

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
    ChangeHandler = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::Name::set);

    array<MulticastDelegate^>^ propertiesDelegates = CreatePropertiesDelegates();

    //
    // Perparing the primary (purely managed) attributes
    //
    Int32 numOfPrimaryAttributes = 0;

    for(numOfPrimaryAttributes = 0;
        numOfPrimaryAttributes < PrintQueue::primaryAttributeNames->Length;
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
        collectionsTable->Add(PrintQueue::primaryAttributeNames[numOfPrimaryAttributes],PropertiesCollection);
    }

    //
    // Perparing the secondary (used for downlevel -unamanged- thunking) attributes
    //
    for(Int32 numOfSecondaryAttributes=0;
        numOfSecondaryAttributes < PrintQueue::secondaryAttributeNames->Length;
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
        collectionsTable->Add(PrintQueue::secondaryAttributeNames[numOfSecondaryAttributes],thunkPropertiesCollection);
    }
}

/*++
    Function Name:
        InitializePrintTickets

    Description:
        Sets the user print ticket and default print ticket to null

    Parameters:
        None

    Return Value
        None:
--*/
__declspec(noinline)
void
PrintQueue::
InitializePrintTickets(
    void
    )
{
    userPrintTicket    = nullptr;
    defaultPrintTicket = nullptr;
}

/*++
    Function Name:
        CreatePropertiesDelegates

    Description:
        This is indicating which delegate is called when
        a named property is changed to reflect the change
        in the compile time property

    Parameters:
        None

    Return Value
        MultiCastDelegate[]:    An array of delegates delegated the
                                property set when a named proeprty change
--*/
array<MulticastDelegate^>^
PrintQueue::
CreatePropertiesDelegates(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = gcnew array<MulticastDelegate^>(primaryAttributeNames->Length +
                                                                                     secondaryAttributeNames->Length);
    //
    // Note to self: This should be impelemented in
    // a better way
    //

    //
    // Primary Delegates
    //
    propertiesDelegates[0]  = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::ShareName::set);
    propertiesDelegates[1]  = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::Comment::set);
    propertiesDelegates[2]  = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::Location::set);
    propertiesDelegates[3]  = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::Description::set);
    propertiesDelegates[4]  = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::Priority::set);
    propertiesDelegates[5]  = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::DefaultPriority::set);
    propertiesDelegates[6]  = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::StartTimeOfDay::set);
    propertiesDelegates[7]  = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::UntilTimeOfDay::set);
    //
    // Average Pages per Minute cannot be set through the collection interface
    //
    propertiesDelegates[8]  = nullptr;
    //
    // Number of Jobs can't be set through the collection interface
    //
    propertiesDelegates[9] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::NumberOfJobs::set);
    propertiesDelegates[10] = nullptr;
    propertiesDelegates[11] = gcnew PrintSystemDelegates::DriverValueChanged(this,&PrintQueue::QueueDriver::set);
    propertiesDelegates[12] = gcnew PrintSystemDelegates::PortValueChanged(this,&PrintQueue::QueuePort::set);
    propertiesDelegates[13] = gcnew PrintSystemDelegates::PrintProcessorValueChanged(this,&PrintQueue::QueuePrintProcessor::set);
    //
    // The hosting Print Server can't be changed through the collection interface
    //
    propertiesDelegates[14] = nullptr;
    propertiesDelegates[15] = nullptr;
    propertiesDelegates[16] = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::SeparatorFile::set);
    propertiesDelegates[17] = gcnew PrintSystemDelegates::PrintTicketValueChanged(this,&PrintQueue::DefaultPrintTicket::set);
    propertiesDelegates[18] = gcnew PrintSystemDelegates::PrintTicketValueChanged(this,&PrintQueue::UserPrintTicket::set);
    propertiesDelegates[19] = gcnew PrintSystemDelegates::BooleanValueChanged(this,&PrintQueue::IsXpsDevice::set);
    //
    // Secondary Delegates
    //
    propertiesDelegates[20] = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::HostingPrintServerName::set);
    propertiesDelegates[21] = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::QueueDriverName::set);
    propertiesDelegates[22] = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::QueuePrintProcessorName::set);
    propertiesDelegates[23] = gcnew PrintSystemDelegates::StringValueChanged(this,&PrintQueue::QueuePortName::set);
    propertiesDelegates[24] = gcnew PrintSystemDelegates::ByteArrayValueChanged(this,&PrintQueue::DefaultDevMode::set);
    propertiesDelegates[25] = gcnew PrintSystemDelegates::ByteArrayValueChanged(this,&PrintQueue::UserDevMode::set);
    propertiesDelegates[26] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::Status::set);
    propertiesDelegates[27] = gcnew PrintSystemDelegates::Int32ValueChanged(this,&PrintQueue::Attributes::set);

    return propertiesDelegates;
}


/*++
    Function Name:
        ConvertPropertyFilterToString

    Description:
        For usage with Intellisense and personas like Mort, it is
        usefull to have an enumeration that could be easily detected in
        those situations. But internally everything is represented as a
        string and not an enum and hence this functions that comverts the
        later to the former.

    Parameters:
        PrintQueueIndexedProperty[]:   An Array of enums

    Return Value
        MultiCastDelegate[]:    An Array of corresponding strings
--*/
array<String^>^
PrintQueue::
ConvertPropertyFilterToString(
    array<PrintQueueIndexedProperty>^      propertiesFilter
    )
{
    array<String^>^ propertiesFilterAsStrings = gcnew array<String^>(propertiesFilter->Length);

    for(Int32 numOfProperties = 0;
        numOfProperties < propertiesFilter->Length;
        numOfProperties++)
    {
        String^ upLevelAttribute = nullptr;

        if(String^ downLevelAttribute = (String^)(upLevelToDownLevelMapping
                                        [upLevelAttribute =
                                                 propertiesFilter[numOfProperties].ToString()]))
        {
            propertiesFilterAsStrings[numOfProperties] = downLevelAttribute;
        }
        else
        {
            propertiesFilterAsStrings[numOfProperties] = upLevelAttribute;
        }
    }

    return propertiesFilterAsStrings;
}


/*++
    Function Name:
        PrepareNameForDownLevelConnectivity

    Description:
        Although in the managed world everything is represented
        as an object. In the unamanaged world things are still
        respresented as strings and in order to instantiate those
        unamanged objects (like calling OpenPrinter) we need the
        proper name to do that. This is where this function comes
        in play as it utilizes the resolver to created the full name
        string from its composings individual parts.

    Parameters:
        String:     Server Name
        String      PrintQueue Name

    Return Value
        String:     DownLevel name
--*/
String^
PrintQueue::
PrepareNameForDownLevelConnectivity(
    String^ serverName,
    String^ printerName
    )
{
    String^ downLevelName = nullptr;

    if(serverName->Equals(PrintWin32Thunk::PrinterThunkHandler::GetLocalMachineName()))
    {
        downLevelName = printerName;
    }
    else
    {
        PrintPropertyDictionary^            resolverAttributeValueCollection = gcnew PrintPropertyDictionary();
        PrintStringProperty^                stringAttributeValue             = nullptr;
        PrintSystemProtocol^                protocol                         = nullptr;

        stringAttributeValue = gcnew PrintStringProperty("ServerName",
                                                         serverName);
        resolverAttributeValueCollection->Add(stringAttributeValue);

        stringAttributeValue = gcnew PrintStringProperty("PrinterName",
                                                         printerName);
        resolverAttributeValueCollection->Add(stringAttributeValue);

        PrintSystemPathResolver^ resolver =
        gcnew PrintSystemPathResolver(resolverAttributeValueCollection,
                                      gcnew PrintSystemUNCPathResolver(gcnew PrintSystemDefaultPathResolver));

        resolver->Resolve();

        protocol = resolver->Protocol;

        downLevelName = protocol->Path;
    }

    return downLevelName;
}


/*++
    Function Name:
        GetUnInitializedData

    Description:
        If a consumer of a property asks for a property that is
        not initialized, then we intialized the property by doing
        a real Get from the server before returning the data. This
        could happen if someone instantiated an object with a Filter
        and then later asks for a property outside the Filter range.

    Parameters:
        String:     managed property name
        String      unamanged property name

    Return Value
        None
--*/
void
PrintQueue::
GetUnInitializedData(
    String^ upLevelPropertyName,
    String^ downLevelPropertyName
    )
{
    GetDataThunkObject^ dataThunkObject = nullptr;

    try
    {
        if(!PropertiesCollection->GetProperty(upLevelPropertyName)->IsInitialized &&
           !get_InternalPropertiesCollection(downLevelPropertyName)->GetProperty(downLevelPropertyName)->IsInitialized)
        {
            if (isBrowsable)
            {
                ActivateBrowsableQueue();
                isBrowsable = false;
            }

            //
            // retrieve the data from the server
            //
            dataThunkObject = gcnew GetDataThunkObject(this->GetType());
            array<String^>^ propertyFilter = {downLevelPropertyName};
            dataThunkObject->PopulatePrintSystemObject(printerThunkHandler,
                                                        this,
                                                        propertyFilter);
            //
            // Add the property to the registered properties filter
            //
            array<String^>^ newRefreshPropertiesFilter = gcnew array<String^>(refreshPropertiesFilter->Length + 1);

            Int32 numOfProperties = 0;

            for(numOfProperties = 0;
                numOfProperties < refreshPropertiesFilter->Length;
                numOfProperties++)
            {
                newRefreshPropertiesFilter[numOfProperties] = refreshPropertiesFilter[numOfProperties];
            }

            newRefreshPropertiesFilter[numOfProperties] = gcnew String(downLevelPropertyName->ToCharArray());
            refreshPropertiesFilter = newRefreshPropertiesFilter;

        }
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintSystemException(internalException->HResult, "PrintSystemException.PrintQueue.GetUninitializedProperty");
    }
    __finally
    {
        if (dataThunkObject)
        {
            delete dataThunkObject;
            dataThunkObject = nullptr;
        }
    }
}

/*++

    Routine Name:
        BuildPortNamesString

    Routine Description:

        It builds the string of ports from an array of strings.
        If a printer is connected to more than one port,
        the names of each port must be separated by commas (for example, "LPT1:,LPT2:,LPT3:").

    Arguments:

        portNames - arrays of strings representing the ports

    Return Value:

        string representing the ports in the format that the Win32 API expects.
--*/
String^
PrintQueue::
BuildPortNamesString(
    array<String^>^ portNames
    )
{
    String^ portNamesSeparatedByComma = nullptr;

    StringBuilder^ portNamesSeparatedByCommaBuilder = gcnew StringBuilder(PrintSystemObject::MaxPath);

    if (portNamesSeparatedByCommaBuilder)
    {
        portNamesSeparatedByCommaBuilder->Append(portNames[0]);

        for (int index = 1 ; index < portNames->Length; index++)
        {
            portNamesSeparatedByCommaBuilder->AppendFormat(",{0}", portNames[index]);
        }

        portNamesSeparatedByComma = portNamesSeparatedByCommaBuilder->ToString();
    }

    return portNamesSeparatedByComma;
}


String^
PrintQueue::
GetAttributeNamePerPrintQueueObject(
    PrintProperty^  attributeValue
    )
{
    String^ name = nullptr;

    if (attributeValue && attributeValue->Name)
    {
        String^ upLevelAttribute = nullptr;

        if(String^ downLevelAttribute = (String^)(upLevelToDownLevelMapping
                                                  [upLevelAttribute = attributeValue->Name]))
        {
            name = downLevelAttribute;
        }
        else
        {
            name = upLevelAttribute;
        }
    }

    return name;
}

Object^
PrintQueue::
GetAttributeValuePerPrintQueueObject(
    PrintProperty^  attributeValue
    )
{
    Object^ value = nullptr;

    if (attributeValue && attributeValue->Name && attributeValue->Value)
    {
        Type^ type = (Type^)(PrintQueue::attributeNameTypes[attributeValue->Name]);

// OACR false positive: as PrintDriver, PrintPort, PrintProcessor and PrintServer are different types with a common base class.
#pragma warning (push)
#pragma warning (disable : 6287)
        if (type == PrintDriver::typeid     ||
            type == PrintPort::typeid              ||
            type == PrintProcessor::typeid    ||
            type == PrintServer::typeid
            )
#pragma warning (pop)
        {
            value = ((PrintSystemObject^)attributeValue->Value)->Name;
        }
        else
        {
            value = attributeValue->Value;
        }
    }

    return value;
}


Stream^
PrintQueue::
ClonePrintTicket(
    Stream^ printTicket
    )
{
    Stream^ clonedPrintTicket = nullptr;

    if(printTicket)
    {
        int printTicketLength  = static_cast<int>(printTicket->Length);
        array<Byte>^ streamData = gcnew array<Byte>(printTicketLength);
        printTicket->Read(streamData,0,printTicketLength);
        clonedPrintTicket = gcnew MemoryStream();
        clonedPrintTicket->Write(streamData,0,printTicketLength);
        clonedPrintTicket->Position = 0;
        printTicket->Position = 0;
    }

    return clonedPrintTicket;
}


Int32
PrintQueue::MaxPrintSchemaVersion::
get(
    void
    )
{
    return PrintTicketManager::MaxPrintSchemaVersion;
}


Int32
PrintQueue::ClientPrintSchemaVersion::
get(
    void
    )
{
    VerifyAccess();
    return clientPrintSchemaVersion;
}

PrintWin32Thunk::PrinterThunkHandler^
PrintQueue::PrinterThunkHandler::
get(
    void
    )
{
    return printerThunkHandler;
}

bool
PrintQueue::IsMxdwLegacyDriver(
    PrintQueue^ printQueue
    )
{
    return printQueue->QueueDriver->Name->Equals("Microsoft XPS Document Writer",
                                                                    StringComparison::OrdinalIgnoreCase);
}

/*++

    Function Name:
        CreateSerializationManager

    Description:
        Creates the appropriate Synchronous serialization manager to serialize and print the document objects.

    Parameters:
        None

    Return Value
        PackageSerializationManager

--*/
PackageSerializationManager^
PrintQueue::
CreateSerializationManager(
    bool    isBatchMode,
    bool    mustSetJobIdentifier
    )
{
    return CreateSerializationManager(isBatchMode, mustSetJobIdentifier, nullptr);
}

/*++

    Function Name:
        CreateSerializationManager

    Description:
        Creates the appropriate Synchronous serialization manager to serialize and print the document objects.

    Parameters:
        None

    Return Value
        PackageSerializationManager

--*/
PackageSerializationManager^
PrintQueue::
CreateSerializationManager(
    bool    isBatchMode,
    bool    mustSetJobIdentifier,
    PrintTicket^ printTicket
    )
{
    PackageSerializationManager^ serializationManager = nullptr;

    printingIsCancelled = false;

    bool supportsXpsSerialization = IsXpsDevice || IsXpsDeviceSimulationSupported();


    if (IsXpsOMPrintingSupported())
    {
        serializationManager = CreateXpsOMSerializationManager(isBatchMode, false /*isAsync*/, printTicket, mustSetJobIdentifier);
        
    }
    else if (!supportsXpsSerialization)
    {
        //
        // If this is a Xps device, we are going to use a Next Generation Conversion Serialization Manager
        //
        #pragma warning ( push )
        #pragma warning ( disable:4691 )
        serializationManager = gcnew NgcSerializationManager(this,isBatchMode);
        #pragma warning ( pop )
    }
    else
    {
        String^ printJobName = this->CurrentJobSettings->Description;

        if (this->CurrentJobSettings->Description == nullptr)
        {
            printJobName = defaultXpsJobName;
        }

        PrintQueueStream^ printStream = gcnew PrintQueueStream(this, printJobName, false, printTicket);

        #pragma warning ( push )
        #pragma warning ( disable:4691 )
        XpsDocument^    reachPackage = XpsDocument::CreateXpsDocument(printStream);
        #pragma warning ( pop )

        XpsPackagingPolicy^ reachPolicy = gcnew XpsPackagingPolicy(reachPackage, PackageInterleavingOrder::ResourceFirst);

        reachPolicy->PackagingProgressEvent += gcnew PackagingProgressEventHandler(printStream,
                                                                                   &PrintQueueStream::HandlePackagingProgressEvent);

        XpsSerializationManager^ xpsSerializationManager = gcnew XpsSerializationManager(reachPolicy, isBatchMode);

        //
        // Quearies to ISV's has identified four pages as the optimal page batch size
        // This sacrifices best case savings of font subsetting vs.
        // memory foot print of accumlating page data to discover font subsets
        //
        xpsSerializationManager->SetFontSubsettingPolicy(FontSubsetterCommitPolicies::CommitPerPage );
        xpsSerializationManager->SetFontSubsettingCountPolicy(4);
        serializationManager = xpsSerializationManager;

        if(serializationManager != nullptr)
        {
            ((XpsSerializationManager^)serializationManager)->XpsSerializationXpsDriverDocEvent += gcnew XpsSerializationXpsDriverDocEventHandler(this,
                                                                                                         &PrintQueue::ForwardXpsDriverDocEvent);
            System::Threading::Monitor::Enter(_lockObject);
            try
            {
                isWriterAttached = true;
                writerStream     = printStream;
                xpsDocument      = reachPackage;

                if (mustSetJobIdentifier)
                {
                    serializationManager->JobIdentifier = printStream->JobIdentifier;
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(_lockObject);
            }
        }
    }

    return serializationManager;
}

/*++

    Function Name:
        CreateSerializationManager

    Description:
        Creates the appropriate Synchronous serialization manager to serialize and print the document objects.

    Parameters:
        None

    Return Value
        PackageSerializationManager

--*/
PackageSerializationManager^
PrintQueue::
CreateSerializationManager(
    bool    isBatchMode
    )
{
    return  CreateSerializationManager(isBatchMode, false);
}

/*++

    Function Name:
        CreateAsyncSerializationManager

    Description:
        Creates the appropriate Asynchronous serialization manager to serialize and print the document objects.

    Parameters:
        None

    Return Value
        PackageAsyncSerializationManager

--*/
PackageSerializationManager^
PrintQueue::
CreateAsyncSerializationManager(
    bool    isBatchMode
    )
{
    return  CreateAsyncSerializationManager(isBatchMode, false, nullptr);
}


/*++

    Function Name:
        CreateAsyncSerializationManager

    Description:
        Creates the appropriate Asynchronous serialization manager to serialize and print the document objects.

    Parameters:
        None

    Return Value
        PackageAsyncSerializationManager

--*/
PackageSerializationManager^
PrintQueue::
CreateAsyncSerializationManager(
    bool    isBatchMode,
    bool    mustSetJobIdentifier,
    PrintTicket^ printTicket
    )
{
    PackageSerializationManager^ serializationManager = nullptr;

    printingIsCancelled = false;

    bool supportsXpsSerialization = IsXpsDevice || IsXpsDeviceSimulationSupported();

    if (IsXpsOMPrintingSupported())
    {
        serializationManager = CreateXpsOMSerializationManager(isBatchMode, true /*isAsync*/, printTicket, mustSetJobIdentifier);
    }
    else if (!supportsXpsSerialization)
    {
        if (mustSetJobIdentifier)
        {
            throw gcnew NotSupportedException;
        }

        //
        // If this is a Xps device, we are going to use a Next Generation Conversion Serialization Manager
        //
        #pragma warning ( push )
        #pragma warning ( disable:4691 )
        serializationManager = gcnew NgcSerializationManagerAsync(this,isBatchMode);
        #pragma warning ( pop )
    }
    else
    {
        String^ printJobName = this->CurrentJobSettings->Description;

        if (this->CurrentJobSettings->Description == nullptr)
        {
            printJobName = defaultXpsJobName;
        }

        PrintQueueStream^ printStream = gcnew PrintQueueStream(this, printJobName);

        XpsDocument^    reachPackage = XpsDocument::CreateXpsDocument(printStream);

        XpsPackagingPolicy^ reachPolicy = gcnew XpsPackagingPolicy(reachPackage, PackageInterleavingOrder::ResourceFirst);

        reachPolicy->PackagingProgressEvent += gcnew PackagingProgressEventHandler(printStream,
                                                                                   &PrintQueueStream::HandlePackagingProgressEvent);

        XpsSerializationManagerAsync^ xpsSerializationManagerAsync = gcnew XpsSerializationManagerAsync(reachPolicy, isBatchMode);

        //
        // Quearies to ISV's has identified four pages as the optimal page batch size
        // This sacrifices best case savings of font subsetting vs.
        // memory foot print of accumlating page data to discover font subsets
        //
        xpsSerializationManagerAsync->SetFontSubsettingPolicy(FontSubsetterCommitPolicies::CommitPerPage );
        xpsSerializationManagerAsync->SetFontSubsettingCountPolicy(4);

        serializationManager = xpsSerializationManagerAsync;

        if(serializationManager != nullptr)
        {
            ((XpsSerializationManagerAsync^)serializationManager)->XpsSerializationXpsDriverDocEvent += gcnew XpsSerializationXpsDriverDocEventHandler(this,
                                                                                                              &PrintQueue::ForwardXpsDriverDocEvent);

            System::Threading::Monitor::Enter(_lockObject);
            try
            {
                isWriterAttached = true;
                writerStream     = printStream;
                xpsDocument      = reachPackage;

                if (mustSetJobIdentifier)
                {
                    serializationManager->JobIdentifier    = printStream->JobIdentifier;
                }
            }
            __finally
            {
                System::Threading::Monitor::Exit(_lockObject);
            }
        }
    }

    return serializationManager;
}

PackageSerializationManager^
PrintQueue::
CreateXpsOMSerializationManager(
    bool            isBatchMode,
    bool            isAsync,
    PrintTicket^    printTicket,
    bool            mustSetPrintJobIdentifier
    )
{
    PackageSerializationManager^ serializationManager = nullptr;

    xpsCompatiblePrinter = gcnew XpsCompatiblePrinter(FullName);

    String^ printJobName = this->CurrentJobSettings->Description;

    if (printJobName == nullptr)
    {
        printJobName = defaultXpsJobName;
    }

    DocInfoThree^ docInfo = gcnew DocInfoThree(printJobName,
        QueuePort->Name,
        DocInfoThree::defaultDataType,
        0);

    xpsCompatiblePrinter->StartDocPrinter(docInfo, printTicket, mustSetPrintJobIdentifier);

    XpsOMPackagingPolicy^ packagingPolicy = gcnew XpsOMPackagingPolicy(xpsCompatiblePrinter->XpsPackageTarget);
    packagingPolicy->PrintQueueReference = this;
    if (isAsync)
    {
        serializationManager = gcnew XpsOMSerializationManagerAsync(packagingPolicy, isBatchMode);
    }
    else
    {
        serializationManager = gcnew XpsOMSerializationManager(packagingPolicy, isBatchMode);
    }

    return serializationManager;
}

/*++

    Function Name:
        DisposeSerializationManager

    Description:
        Some actions need to be done at the end of the life cycle of
        a serializaiton manager and this is the function to carry out
        those methods

    Parameters:
        None

    Return Value
        none
--*/
void
PrintQueue::
DisposeSerializationManager(
    void
    )
{
    DisposeSerializationManager( false );
}

/*++

    Function Name:
        DisposeSerializationManager

    Description:
        Some actions need to be done at the end of the life cycle of
        a serializaiton manager and this is the function to carry out
        those methods

    Parameters:
        bool abort indicates whether the print stream needs to be aborted or closed

    Return Value
        none
--*/

void
PrintQueue::
DisposeSerializationManager(
    bool abort
    )
{
    XpsDocument^         document    = nullptr;
    PrintQueueStream^    printStream = nullptr;

    System::Threading::Monitor::Enter(_lockObject);

    try
    {
        if (isWriterAttached == true)
        {
            isWriterAttached = false;

            if (xpsDocument != nullptr)
            {
                document       = xpsDocument;
                xpsDocument    = nullptr;
            }

            if (writerStream != nullptr)
            {
                printStream  = writerStream;
                writerStream = nullptr;
            }
        }
    }
    __finally
    {
        System::Threading::Monitor::Exit(_lockObject);
    }

    if (abort &&
        printStream != nullptr)
    {
        //Notify printstream that we have aborted before calling DisposeXpsDocument which
        //will try to write to the spool file.
        printStream->Abort();
    }

    if(document != nullptr)
    {
        document->DisposeXpsDocument();
    }

    if(printStream != nullptr)
    {
        printStream->Close();
    }

    if (xpsCompatiblePrinter != nullptr)
    {
        if (abort)
        {
            xpsCompatiblePrinter->AbortPrinter();
        }
        xpsCompatiblePrinter->EndDocPrinter();
    }
}

void
PrintQueue::
EnsureJobId(
    PackageSerializationManager^ manager
    )
{
    if (xpsCompatiblePrinter != nullptr)
    {
        manager->JobIdentifier = xpsCompatiblePrinter->JobIdentifier;
    }
}

void
PrintQueue::XpsOMPackageWriter::
set(
RCW::IXpsOMPackageWriter^ packageWriter
)
{
    xpsCompatiblePrinter->XpsOMPackageWriter = packageWriter;
}

XpsDocumentWriter^
PrintQueue::
CreateXpsDocumentWriter(
    PrintQueue^     printQueue
    )
{
    XpsDocumentWriter^  writer  = gcnew XpsDocumentWriter(printQueue);

    return writer;
}

/*--------------------------------------------------------------------------------------*/

XpsDocumentWriter^
PrintQueue::
CreateXpsDocumentWriter(
    double%     width,
    double%     height
    )
{
    XpsDocumentWriter^  writer = nullptr;
    PrintTicket^        partialTrustPrintTicket = nullptr;
    PrintQueue^         partialTrustPrintQueue  = nullptr;

    ShowPrintDialog(writer,
                    partialTrustPrintTicket,
                    partialTrustPrintQueue,
                    width,
                    height,
                    nullptr
                    );

     return writer;
}

XpsDocumentWriter^
PrintQueue::
CreateXpsDocumentWriter(
    PrintDocumentImageableArea^%    printDocumentImageableArea
     )
{
    return CreateXpsDocumentWriter(nullptr,
                                   printDocumentImageableArea);
}

XpsDocumentWriter^
PrintQueue::
CreateXpsDocumentWriter(
    PrintDocumentImageableArea^%                        printDocumentImageableArea,
    System::Windows::Controls::PageRangeSelection%      pageRangeSelection,
    System::Windows::Controls::PageRange%               pageRange
    )
{
    return CreateXpsDocumentWriter(nullptr,
                                   printDocumentImageableArea,
                                   pageRangeSelection,
                                   pageRange);
}

XpsDocumentWriter^
PrintQueue::
CreateXpsDocumentWriter(
    String^                         jobDescription,
    PrintDocumentImageableArea^%    printDocumentImageableArea
    )
{
    XpsDocumentWriter^  writer = nullptr;
    PrintTicket^        partialTrustPrintTicket = nullptr;
    PrintQueue^         partialTrustPrintQueue  = nullptr;
    double              height;
    double              width;


    if(ShowPrintDialog(writer,
                       partialTrustPrintTicket,
                       partialTrustPrintQueue,
                       height,
                       width,
                       jobDescription
                       ))
    {
        printDocumentImageableArea = CalculateImagableArea(partialTrustPrintTicket,
                                                           partialTrustPrintQueue,
                                                           height,
                                                           width
                                                           );
    }

    return writer;
}

XpsDocumentWriter^
PrintQueue::
CreateXpsDocumentWriter(
    String^                                             jobDescription,
    PrintDocumentImageableArea^%                        printDocumentImageableArea,
    System::Windows::Controls::PageRangeSelection%      pageRangeSelection,
    System::Windows::Controls::PageRange%               pageRange
    )
{
    XpsDocumentWriter^  writer = nullptr;
    PrintTicket^        partialTrustPrintTicket = nullptr;
    PrintQueue^         partialTrustPrintQueue  = nullptr;
    double              height;
    double              width;


    if(ShowPrintDialogEnablePageRange(writer,
                                     partialTrustPrintTicket,
                                     partialTrustPrintQueue,
                                     height,
                                     width,
                                     pageRangeSelection,
                                     pageRange,
                                     jobDescription
                                     ))
    {
        printDocumentImageableArea = CalculateImagableArea(partialTrustPrintTicket,
                                                           partialTrustPrintQueue,
                                                           height,
                                                           width);
    }

    return writer;
}

PrintDocumentImageableArea^
PrintQueue::
CalculateImagableArea(
    PrintTicket^        partialTrustPrintTicket,
    PrintQueue^         partialTrustPrintQueue,
    double              height,
    double              width
    )
{
    PrintDocumentImageableArea^ documentImageableArea = gcnew PrintDocumentImageableArea();

    documentImageableArea->MediaSizeWidth  = height;
    documentImageableArea->MediaSizeHeight = width;

    //
    // Now let's calculate the real size of the imageable are on the device
    //
    PrintCapabilities^ printCapabilities = partialTrustPrintQueue->GetPrintCapabilities(partialTrustPrintTicket);


    if (printCapabilities->PageImageableArea != nullptr )
    {
        documentImageableArea->OriginWidth   = printCapabilities->PageImageableArea->OriginWidth;
        documentImageableArea->OriginHeight  = printCapabilities->PageImageableArea->OriginHeight;
        documentImageableArea->ExtentWidth   = printCapabilities->PageImageableArea->ExtentWidth;
        documentImageableArea->ExtentHeight  = printCapabilities->PageImageableArea->ExtentHeight;
    }
    else
    {
        documentImageableArea->ExtentWidth   = documentImageableArea->MediaSizeWidth;
        documentImageableArea->ExtentHeight  = documentImageableArea->MediaSizeHeight;
    }
    return documentImageableArea;
}

bool
PrintQueue::
ShowPrintDialog(
    XpsDocumentWriter^%     writer,
    PrintTicket^%           partialTrustPrintTicket,
    PrintQueue^%            partialTrustPrintQueue,
    double%                 width,
    double%                 height,
    String^                 jobDescription
    )
{
    //
    // Invoke Avalon UI and get a partialTrustPrintQueue
    //
    #pragma warning ( push )
    #pragma warning ( disable:4691 )
    System::Windows::Controls::PrintDialog^ printDialog = gcnew System::Windows::Controls::PrintDialog();
    #pragma warning ( pop )
    bool dialogOk = GatherDataFromPrintDialog(printDialog,
                                              writer,
                                              partialTrustPrintTicket,
                                              partialTrustPrintQueue,
                                              width,
                                              height,
                                              jobDescription);

    return dialogOk;
}

bool
PrintQueue::
ShowPrintDialogEnablePageRange(
    XpsDocumentWriter^%                             writer,
    PrintTicket^%                                   partialTrustPrintTicket,
    PrintQueue^%                                    partialTrustPrintQueue,
    double%                                         width,
    double%                                         height,
    System::Windows::Controls::PageRangeSelection%  pageRangeSelection,
    System::Windows::Controls::PageRange%           pageRange,
    String^                                         jobDescription
    )
{
    //
    // Invoke Avalon UI and get a partialTrustPrintQueue
    //
    #pragma warning ( push )
    #pragma warning ( disable:4691 )
    System::Windows::Controls::PrintDialog^ printDialog = gcnew System::Windows::Controls::PrintDialog();
    #pragma warning ( pop )
    printDialog->UserPageRangeEnabled = true;
    bool dialogOk = GatherDataFromPrintDialog(printDialog,
                                              writer,
                                              partialTrustPrintTicket,
                                              partialTrustPrintQueue,
                                              width,
                                              height,
                                              jobDescription);
    if( dialogOk )
    {
        pageRangeSelection = printDialog->PageRangeSelection;
        pageRange = printDialog->PageRange;
    }

    return dialogOk;
}

bool
PrintQueue::
GatherDataFromPrintDialog(
    System::Windows::Controls::PrintDialog^ printDialog,
    XpsDocumentWriter^%     writer,
    PrintTicket^%           partialTrustPrintTicket,
    PrintQueue^%            partialTrustPrintQueue,
    double%                 width,
    double%                 height,
    String^                 jobDescription
    )
{

    bool dialogOk = false;
    Nullable<bool>  boolNullable = printDialog->ShowDialog();

    if(boolNullable.HasValue &&
       boolNullable.Value == true)
    {
        dialogOk = true;

        partialTrustPrintTicket = printDialog->PrintTicket;
        partialTrustPrintQueue  = printDialog->PrintQueue;
        if(partialTrustPrintQueue!=nullptr &&
            jobDescription!=nullptr)
        {
            partialTrustPrintQueue->CurrentJobSettings->Description = jobDescription;
        }
        partialTrustPrintQueue->InPartialTrust = true;

        writer  = gcnew XpsDocumentWriter(partialTrustPrintQueue, nullptr);

        PartialTrustPrintTicketEventHandler^
         printTicketEventHandler = gcnew PartialTrustPrintTicketEventHandler(partialTrustPrintTicket);

        writer->WritingPrintTicketRequired +=
        gcnew WritingPrintTicketRequiredEventHandler(printTicketEventHandler,
                                                     &PartialTrustPrintTicketEventHandler::
                                                     SetPrintTicketInPartialTrust);

        width  = printDialog->PrintableAreaWidth;
        height = printDialog->PrintableAreaHeight;
    }

    return dialogOk;
}

PrintQueue::
PartialTrustPrintTicketEventHandler::
PartialTrustPrintTicketEventHandler(
    PrintTicket^        printTicket
    ):
    isPrintTicketHandedOver(false)
{
    partialTrustPrintTicket = printTicket;
}

void
PrintQueue::
PartialTrustPrintTicketEventHandler::
SetPrintTicketInPartialTrust(
    Object^                                sender,
    WritingPrintTicketRequiredEventArgs^   args
    )
{
    if(!isPrintTicketHandedOver)
    {
        if ( (args->CurrentPrintTicketLevel == PrintTicketLevel::FixedDocumentSequencePrintTicket) ||
             (args->CurrentPrintTicketLevel == PrintTicketLevel::FixedDocumentPrintTicket) )
        {
            args->CurrentPrintTicket = partialTrustPrintTicket;
            //
            // In partial trust, we only have one print ticket for the whole
            // document and we should hand it over only once to the calling
            // component.
            //
            isPrintTicketHandedOver = true;
        }
    }
}

Boolean
PrintQueue::
IsXpsDocumentEventSupported(
    XpsDocumentEventType    escape
    )
{
    return printerThunkHandler->IsXpsDocumentEventSupported(escape,
                                                           (escape == XpsDocumentEventType::AddFixedDocumentSequencePre));
}

void
PrintQueue::
ForwardXpsDriverDocEvent(
    Object^                                                                        sender,
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    try
    {
        if (IsXpsDevice && IsXpsDocumentEventSupported(e->DocumentEvent))
        {
            switch (e->DocumentEvent)
            {
                case XpsDocumentEventType::AddFixedDocumentSequencePre:
                case XpsDocumentEventType::AddFixedDocumentSequencePost:
                {
                    ForwardXpsFixedDocumentSequenceEvent(e);
                    break;
                }
                case XpsDocumentEventType::AddFixedDocumentPre:
                case XpsDocumentEventType::AddFixedDocumentPost:
                {
                    ForwardXpsFixedDocumentEvent(e);
                    break;
                }
                case XpsDocumentEventType::AddFixedPagePre:
                case XpsDocumentEventType::AddFixedPagePost:
                {
                    ForwardXpsFixedPageEvent(e);
                    break;
                }
                case XpsDocumentEventType::AddFixedDocumentSequencePrintTicketPre:
                {
                    ForwardXpsFixedDocumentSequencePrintTicket(e);
                    break;
                }
                case XpsDocumentEventType::AddFixedDocumentPrintTicketPre:
                {
                    ForwardXpsFixedDocumentPrintTicket(e);
                    break;
                }
                case XpsDocumentEventType::AddFixedPagePrintTicketPre:
                {
                    ForwardXpsFixedPagePrintTicket(e);
                    break;
                }
                case XpsDocumentEventType::XpsDocumentCancel:
                {
                    XpsDocumentEventCancel();

                    break;
                }
                case XpsDocumentEventType::AddFixedPagePrintTicketPost:
                case XpsDocumentEventType::AddFixedDocumentPrintTicketPost:
                case XpsDocumentEventType::AddFixedDocumentSequencePrintTicketPost:
                case XpsDocumentEventType::None:
                default:
                {
                    break;
                }
            }
        }
    }
    catch(InternalPrintSystemException^ internalException)
    {
        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
}


void
PrintQueue::
ForwardXpsFixedDocumentSequenceEvent(
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    SafeHandle^ inputBufferSafeHandle = nullptr;
    Int32       returnValue           = DOCUMENTEVENT_UNSUPPORTED;

    try
    {
        inputBufferSafeHandle = UnmanagedXpsDocEventBuilder::XpsDocEventFixedDocSequence(e->DocumentEvent,
                                                                                         writerStream->JobIdentifier,
                                                                                         (this->CurrentJobSettings->Description == nullptr) ?
                                                                                             defaultXpsJobName :
                                                                                             this->CurrentJobSettings->Description,
                                                                                         nullptr,
                                                                                         false);
        returnValue = this->XpsDocumentEvent(e->DocumentEvent,
                                             inputBufferSafeHandle);

    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }
    }
}

void
PrintQueue::
ForwardXpsFixedDocumentEvent(
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    SafeHandle^ inputBufferSafeHandle = nullptr;
    Int32       returnValue           = DOCUMENTEVENT_UNSUPPORTED;

    try
    {
        inputBufferSafeHandle = UnmanagedXpsDocEventBuilder::XpsDocEventFixedDocument(e->DocumentEvent,
                                                                                      e->CurrentCount,
                                                                                      nullptr,
                                                                                      false);
        returnValue = this->XpsDocumentEvent(e->DocumentEvent,
                                            inputBufferSafeHandle);
    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }
    }
}

void
PrintQueue::
ForwardXpsFixedPageEvent(
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    SafeHandle^ inputBufferSafeHandle = nullptr;
    Int32       returnValue           = DOCUMENTEVENT_UNSUPPORTED;

    try
    {
        inputBufferSafeHandle = UnmanagedXpsDocEventBuilder::XpsDocEventFixedPage(e->DocumentEvent,
                                                                                  e->CurrentCount,
                                                                                  nullptr,
                                                                                  false);
        returnValue = this->XpsDocumentEvent(e->DocumentEvent,
                                             inputBufferSafeHandle);
    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }
    }
}

void
PrintQueue::
ForwardXpsFixedDocumentSequencePrintTicket(
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    SafeHandle^     inputBufferSafeHandle = nullptr;
    MemoryStream^   printTicketStream     = nullptr;

    try
    {
        if (e->PrintTicket != nullptr)
        {
            printTicketStream = e->PrintTicket->GetXmlStream();
        }

        inputBufferSafeHandle = UnmanagedXpsDocEventBuilder::XpsDocEventFixedDocSequence(e->DocumentEvent,
                                                                                         writerStream->JobIdentifier,
                                                                                         (this->CurrentJobSettings->Description == nullptr) ?
                                                                                              defaultXpsJobName :
                                                                                              this->CurrentJobSettings->Description,
                                                                                         printTicketStream,
                                                                                         true);

        this->XpsDocumentEventPrintTicket(XpsDocumentEventType::AddFixedDocumentSequencePrintTicketPre,
                                          XpsDocumentEventType::AddFixedDocumentSequencePrintTicketPost,
                                          inputBufferSafeHandle,
                                          e);
    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        if (printTicketStream != nullptr)
        {
            delete printTicketStream;
            printTicketStream = nullptr;
        }

        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        if (printTicketStream != nullptr)
        {
            delete printTicketStream;
            printTicketStream = nullptr;
        }
    }
}

void
PrintQueue::
ForwardXpsFixedDocumentPrintTicket(
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    SafeHandle^ inputBufferSafeHandle = nullptr;
    Stream^     printTicketStream     = nullptr;

    try
    {
        if (e->PrintTicket != nullptr)
        {
            printTicketStream = e->PrintTicket->GetXmlStream();
        }

        inputBufferSafeHandle = UnmanagedXpsDocEventBuilder::XpsDocEventFixedDocument(e->DocumentEvent,
                                                                                      e->CurrentCount,
                                                                                      printTicketStream,
                                                                                      true);

        this->XpsDocumentEventPrintTicket(XpsDocumentEventType::AddFixedDocumentPrintTicketPre,
                                          XpsDocumentEventType::AddFixedDocumentPrintTicketPost,
                                          inputBufferSafeHandle,
                                          e);
    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        if (printTicketStream != nullptr)
        {
            delete printTicketStream;
            printTicketStream = nullptr;
        }

        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        if (printTicketStream != nullptr)
        {
            delete printTicketStream;
            printTicketStream = nullptr;
        }
    }
}

void
PrintQueue::
ForwardXpsFixedPagePrintTicket(
    System::Windows::Xps::Serialization::XpsSerializationXpsDriverDocEventArgs^    e
    )
{
    SafeHandle^     inputBufferSafeHandle = nullptr;
    Stream^         printTicketStream     = nullptr;

    try
    {
        if (e->PrintTicket != nullptr)
        {
            printTicketStream = e->PrintTicket->GetXmlStream();
        }

        inputBufferSafeHandle = UnmanagedXpsDocEventBuilder::XpsDocEventFixedDocument(e->DocumentEvent,
                                                                                      e->CurrentCount,
                                                                                      printTicketStream,
                                                                                      true);

        this->XpsDocumentEventPrintTicket(XpsDocumentEventType::AddFixedPagePrintTicketPre,
                                          XpsDocumentEventType::AddFixedPagePrintTicketPost,
                                          inputBufferSafeHandle,
                                          e);
    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        if (printTicketStream != nullptr)
        {
            delete printTicketStream;
            printTicketStream = nullptr;
        }

        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (inputBufferSafeHandle != nullptr)
        {
            delete inputBufferSafeHandle;
        }

        if (printTicketStream != nullptr)
        {
            delete printTicketStream;
            printTicketStream = nullptr;
        }
    }
}

Int32
PrintQueue::
XpsDocumentEvent(
    XpsDocumentEventType    escape,
    SafeHandle^             inputBufferSafeHandle
    )
{
    Int32       returnValue = DOCUMENTEVENT_UNSUPPORTED;

    if (inputBufferSafeHandle != nullptr)
    {
        returnValue = printerThunkHandler->ThunkDocumentEvent(escape,
                                                                inputBufferSafeHandle);
    }

    return returnValue;
}

Int32
PrintQueue::
XpsDocumentEventPrintTicket(
    XpsDocumentEventType                    preEscape,
    XpsDocumentEventType                    postEscape,
    SafeHandle^                             inputBufferSafeHandle,
    XpsSerializationXpsDriverDocEventArgs^  e
    )
{
    MemoryStream^   driverPrintTicketStream = nullptr;
    PrintTicket^    driverPrintTicket       = nullptr;
    Int32           returnValue             = DOCUMENTEVENT_UNSUPPORTED;

    try
    {
        returnValue = printerThunkHandler->ThunkDocumentEventPrintTicket(preEscape,
                                                                         postEscape,
                                                                         inputBufferSafeHandle,
                                                                         driverPrintTicketStream);
        if (returnValue)
        {
            if (driverPrintTicketStream)
            {
                driverPrintTicket = gcnew PrintTicket(driverPrintTicketStream);
            }
            e->PrintTicket = driverPrintTicket;
        }
    }
    catch(InternalPrintSystemException^ internalException)
    {
        if (driverPrintTicketStream != nullptr)
        {
            delete driverPrintTicketStream;
            driverPrintTicketStream = nullptr;
        }
        throw PrintSystemJobInfo::CreatePrintJobException(internalException->HResult, "PrintSystemException.PrintSystemJobInfo.XpsDocumentEvent");
    }
    __finally
    {
        if (driverPrintTicketStream != nullptr)
        {
            delete driverPrintTicketStream;
            driverPrintTicketStream = nullptr;
        }
    }
    return returnValue;
}

void
PrintQueue::
XpsDocumentEventCancel(
    void
    )
{
    printerThunkHandler->ThunkDocumentEvent(XpsDocumentEventType::XpsDocumentCancel);
}

void
PrintQueue::
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
PrintQueue::CreatePrintQueueException(
    int hresult,
    String^ messageId
    )
{
    return gcnew PrintQueueException(hresult, messageId, Name);
}

__declspec(noinline)
Exception^
PrintQueue::CreatePrintSystemException(
    int hresult,
    String^ messageId
    )
{
    return gcnew PrintSystemException(hresult, messageId);
}

/*--------------------------------------------------------------------------------------*/
/*                              PrintQueueCollection Implementation                     */
/*--------------------------------------------------------------------------------------*/

PrintQueueCollection::
PrintQueueCollection(
    void
    )
{
    printQueuesCollection = gcnew System::Collections::Generic::Queue<PrintQueue ^>();

    accessVerifier = gcnew PrintSystemDispatcherObject();

}

PrintQueueCollection::
PrintQueueCollection(
    PrintServer^                        printServer,
    array<String^>^                     propertyFilter,
    array<EnumeratedPrintQueueTypes>^   enumerationFlag
    )
{
    EnumDataThunkObject^ enumDataThunkObject = nullptr;

    printQueuesCollection = gcnew System::Collections::Generic::Queue<PrintQueue ^>();

    accessVerifier = gcnew PrintSystemDispatcherObject();

    try
    {
        enumDataThunkObject =
        gcnew EnumDataThunkObject(System::Printing::PrintQueue::typeid);

        enumDataThunkObject->GetPrintSystemValuesPerPrintQueues(printServer,
                                                                enumerationFlag,
                                                                printQueuesCollection,
                                                                AddNameAndHostToProperties(propertyFilter));
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw printServer->CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintQueues.Enumerate");
    }
    __finally
    {
        if (enumDataThunkObject)
        {
            delete enumDataThunkObject;
            enumDataThunkObject = nullptr;
        }
    }
}

PrintQueueCollection::
PrintQueueCollection(
    PrintServer^    printServer,
    array<String^>^ propertyFilter
    )
{
    EnumDataThunkObject^ enumDataThunkObject = nullptr;

    printQueuesCollection = gcnew System::Collections::Generic::Queue<PrintQueue ^>();

    accessVerifier = gcnew PrintSystemDispatcherObject();

    try
    {
        enumDataThunkObject =
        gcnew EnumDataThunkObject(System::Printing::PrintQueue::typeid);

        array<EnumeratedPrintQueueTypes>^    enumerationFlag = {EnumeratedPrintQueueTypes::Local};

        enumDataThunkObject->GetPrintSystemValuesPerPrintQueues(printServer,
                                                                enumerationFlag,
                                                                printQueuesCollection,
                                                                AddNameAndHostToProperties(propertyFilter));

    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw printServer->CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintQueues.Enumerate");
    }

    __finally
    {
        if (enumDataThunkObject)
        {
            delete enumDataThunkObject;
            enumDataThunkObject = nullptr;
        }
    }
}

PrintQueueCollection::
~PrintQueueCollection(
    )
{
    VerifyAccess();
    printQueuesCollection = nullptr;
}

array<String^>^
PrintQueueCollection::
AddNameAndHostToProperties(
    array<String^>^ propertyFilter
    )
{
    Int32 index = 0;
    array<String^>^ NameAndHostPropertiesFilter = gcnew array<String^>(propertyFilter->Length + 2);

     NameAndHostPropertiesFilter[0] = L"Name";
     NameAndHostPropertiesFilter[1] = L"HostingPrintServerName";

     for(index = 0; index < propertyFilter->Length; index++)
    {
        NameAndHostPropertiesFilter[index + 2] = propertyFilter[index];
    }

    return NameAndHostPropertiesFilter;
}


void
PrintQueueCollection::
Add(
    PrintQueue^ printQueue
    )
{
    VerifyAccess();
    printQueuesCollection->Enqueue(printQueue);
}

System::
Collections::
Generic::
IEnumerator<PrintQueue ^>^
PrintQueueCollection::
GetEnumerator(
    void
    )
{
    VerifyAccess();

    return printQueuesCollection->GetEnumerator();
}


System::
Collections::
IEnumerator^
PrintQueueCollection::
GetNonGenericEnumerator(
    void
    )
{
    VerifyAccess();

    return printQueuesCollection->GetEnumerator();
}

Object^
PrintQueueCollection::SyncRoot::
get(
    void
    )
{
    return const_cast<Object^>(syncRoot);
}

void
PrintQueueCollection::
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


