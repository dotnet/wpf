// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++
    Abstract:

        LocalPrintServer object definition.
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

    LocalPrintServer

Routine Description:

    Constructor. It binds the object to the print server.

Arguments:

    None

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    void
    ):
PrintServer(),
defaultPrintQueue(nullptr),
accessVerifier(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = LocalPrintServer::GetAllPropertiesFilter();
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}

/*++

Routine Name:

    LocalPrintServer

Routine Description:

    Constructor

Arguments:

    type    - print server type.

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    PrintServerType type
    ):
PrintServer(nullptr,
            type),
defaultPrintQueue(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = LocalPrintServer::GetAllPropertiesFilter();
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}


/*++

Routine Name:

    LocalPrintServer

Routine Description:

    Constructor

Arguments:

    propertiesFilter    - server properties to be initialized while building the object.

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    array<LocalPrintServerIndexedProperty>^  propertiesFilter
    ):
PrintServer(nullptr,
            (array<PrintServerIndexedProperty>^)propertiesFilter),
defaultPrintQueue(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = GetAllPropertiesFilter(ConvertPropertyFilterToString(propertiesFilter));
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}


/*++

Routine Name:

    LocalPrintServer

Routine Description:

    Constructor

Arguments:

    propertiesFilter    - server properties to be initialized while building the object.

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    array<String^>^       propertiesFilter
    ):
PrintServer(nullptr,
            propertiesFilter),
accessVerifier(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = LocalPrintServer::GetAllPropertiesFilter(propertiesFilter);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}


/*++

Routine Name:

    LocalPrintServer

Routine Description:

    Constructor. Binds the object to the local print server and populates
    all properties.

Arguments:

    desiredAccess   -   requested permissions

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    PrintSystemDesiredAccess    desiredAccess
    ):
PrintServer(nullptr,
            desiredAccess),
defaultPrintQueue(nullptr),
accessVerifier(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = LocalPrintServer::GetAllPropertiesFilter();

        GetUnInitializedData(refreshPropertiesFilter);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}


/*++

Routine Name:

    LocalPrintServer

Routine Description:

    Constructor. Binds the object to the local print server.

Arguments:

    propertiesFilter - server properties to be initialized while building the object.
    desiredAccess    - requested permissions

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    array<LocalPrintServerIndexedProperty>^    propertiesFilter,
    PrintSystemDesiredAccess                   desiredAccess
    ):
PrintServer(nullptr,
            (array<PrintServerIndexedProperty>^)propertiesFilter,
            desiredAccess),
accessVerifier(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = GetAllPropertiesFilter(ConvertPropertyFilterToString(propertiesFilter));

        GetUnInitializedData(refreshPropertiesFilter);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}

/*++

Routine Name:

    LocalPrintServer

Routine Description:

    Constructor. Binds the object to the local print server.

Arguments:

    propertiesFilter - server properties to be initialized while building the object.
    desiredAccess    - requested permissions

Return Value:

    N\A

--*/
LocalPrintServer::
LocalPrintServer(
    array<String^>^             propertiesFilter,
    PrintSystemDesiredAccess    desiredAccess
    ):
PrintServer(nullptr,
            propertiesFilter,
            desiredAccess),
accessVerifier(nullptr)
{
    try
    {
        Initialize();

        this->IsInternallyInitialized = true;

        refreshPropertiesFilter = LocalPrintServer::GetAllPropertiesFilter(propertiesFilter);

        GetUnInitializedData(refreshPropertiesFilter);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Generic");
    }
}

/*++

Routine Name:

    get_DefaultPrintQueue

Routine Description:

    Property

Arguments:

    None

Return Value:

    object that represents the default print queue.

--*/
PrintQueue^
LocalPrintServer::DefaultPrintQueue::
get(
    void
    )
{
    VerifyAccess();

    if(IsDelayInitialized)
    {
        GetUnInitializedData(refreshPropertiesFilter);
        IsDelayInitialized = false;
    }
    else
    {
        GetDataFromServer("DefaultPrintQueue", false);
    }

    return defaultPrintQueue;
}

/*++

Routine Name:

    set_DefaultPrintQueue

Routine Description:

    Property

Arguments:

    queue - PrintQueue object that represents the default print queue

Return Value:

    void

--*/
void
LocalPrintServer::DefaultPrintQueue::
set(
    PrintQueue^     requiredDefaultQueue
    )
{
    VerifyAccess();

    if(requiredDefaultQueue != defaultPrintQueue ||
       (requiredDefaultQueue &&
        !requiredDefaultQueue->Name->Equals(defaultPrintQueue->Name)))
    {
        defaultPrintQueue = requiredDefaultQueue;

        PropertiesCollection->GetProperty("DefaultPrintQueue")->Value = defaultPrintQueue;
    }
}

/*++

Routine Name:

    GetDefaultPrintQueue

Routine Description:

    Returns the default print queue

Arguments:

    None

Return Value:

    None

--*/
PrintQueue^
LocalPrintServer::
GetDefaultPrintQueue(
    void
    )
{
   return (gcnew LocalPrintServer())->DefaultPrintQueue;
}

/*++

Routine Name:

    ConnectToPrintQueue

Routine Description:

    Creates a printer connection to a given printer.

Arguments:

    printQueuePath   - \\server\share or \\server\printerName
Return Value:

    None

--*/
bool
LocalPrintServer::
ConnectToPrintQueue(
    String^ printQueuePath
    )
{
    VerifyAccess();

    bool    returnValue = false;

    try
    {
        returnValue = PrintWin32Thunk::PrinterThunkHandler::ThunkAddPrinterConnection(printQueuePath);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.AddConnection");
    }

    return returnValue;
}

/*++

Routine Name:

    ConnectToPrintQueue

Routine Description:

    Creates a printer connection to a given printer.

Arguments:

    printerPath   - \\server\share or \\server\printerName
Return Value:

    None

--*/
bool
LocalPrintServer::
ConnectToPrintQueue(
    PrintQueue^ queue
    )
{
    VerifyAccess();

    bool    returnValue = false;

    try
    {
        returnValue = PrintWin32Thunk::PrinterThunkHandler::ThunkAddPrinterConnection(GetFullPrintQueueName(queue));
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.AddConnection");
    }

    return returnValue;
}


/*++

Routine Name:

    DisconnectFromPrintQueue

Routine Description:

    Deletes a printer connection to a given printer.

Arguments:

    printQueuePath   - \\server\share or \\server\printerName
Return Value:

    None

--*/
bool
LocalPrintServer::
DisconnectFromPrintQueue(
    String^ printQueuePath
    )
{
    VerifyAccess();

    bool    returnValue = false;

    try
    {
        returnValue = PrintWin32Thunk::PrinterThunkHandler::ThunkDeletePrinterConnection(printQueuePath);
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.DeleteConnection");
    }

    return returnValue;
}

/*++

Routine Name:

    DisconnectFromPrintQueue

Routine Description:

    Deletes a printer connection to a given printer.

Arguments:

    printerPath   - \\server\share or \\server\printerName
Return Value:

    None

--*/
bool
LocalPrintServer::
DisconnectFromPrintQueue(
    PrintQueue^ queue
    )
{
    VerifyAccess();

    bool    returnValue = false;

    try
    {
        returnValue = PrintWin32Thunk::PrinterThunkHandler::ThunkDeletePrinterConnection(GetFullPrintQueueName(queue));
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.DeleteConnection");
    }

    return returnValue;
}

/*++

Routine Name:

    GetFullPrintQueueName

Routine Description:

    Build the full print queue name that can be used in the thunking code.

Arguments:

    PrintQueue  - queue object

Return Value:

    printerPath   - \\server\printerName
    None

--*/
String^
LocalPrintServer::
GetFullPrintQueueName(
    PrintQueue^ queue
    )
{
    PrintPropertyDictionary^ resolverAttributeValueCollection = gcnew PrintPropertyDictionary();
    PrintStringProperty^     stringAttributeValue = nullptr;

    stringAttributeValue = gcnew PrintStringProperty("ServerName",
                                                     queue->HostingPrintServer->Name);
    resolverAttributeValueCollection->Add(stringAttributeValue);

    stringAttributeValue = gcnew PrintStringProperty("PrinterName",
                                                     queue->Name);
    resolverAttributeValueCollection->Add(stringAttributeValue);

    PrintSystemPathResolver^ resolver =
    gcnew PrintSystemPathResolver(resolverAttributeValueCollection,
                                  gcnew PrintSystemUNCPathResolver(gcnew PrintSystemDefaultPathResolver));

    resolver->Resolve();

    return resolver->Protocol->Path;
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
LocalPrintServer::
RegisterAttributesNamesTypes(
    void
    )
{
    //
    // Register the attributes of the base class first
    //
    PrintServer::RegisterAttributesNamesTypes(LocalPrintServer::attributeNameTypes);
    //
    // Register the attributes of the current class
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < LocalPrintServer::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        attributeNameTypes->Add(LocalPrintServer::PrimaryAttributeNames()[numOfAttributes],
                                LocalPrintServer::PrimaryAttributeTypes()[numOfAttributes]);
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
LocalPrintServer::
CreateAttributeNoValue(
    String^ attributeName
    )
{
    Type^ type = (Type^)LocalPrintServer::attributeNameTypes[attributeName];

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
LocalPrintServer::
CreateAttributeValue(
    String^ attributeName,
    Object^ attributeValue
    )
{
    Type^ type = (Type^)LocalPrintServer::attributeNameTypes[attributeName];

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
LocalPrintServer::
CreateAttributeNoValueLinked(
    String^             attributeName,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)LocalPrintServer::attributeNameTypes[attributeName];

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
LocalPrintServer::
CreateAttributeValueLinked(
    String^             attributeName,
    Object^             attributeValue,
    MulticastDelegate^  delegate
    )
{
    Type^ type = (Type^)LocalPrintServer::attributeNameTypes[attributeName];

    return PrintPropertyFactory::Value->Create(type,attributeName,attributeValue,delegate);
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
LocalPrintServer::
GetDataFromServer(
    String^     property,
    Boolean     forceRefresh
    )
{
    if (property)
    {
        PrintProperty^  attributeValue = PropertiesCollection->GetProperty(property);

        if (forceRefresh || !attributeValue->IsInitialized)
        {
            //
            // must do delegate table
            //
            try
            {
                attributeValue->IsInternallyInitialized = true;

                String^ defaultPrinterName = PrintWin32Thunk::PrinterThunkHandler::ThunkGetDefaultPrinter();

                if (property->Equals("DefaultPrintQueue"))
                {
                    PrintServer ^targetPrintServer  = nullptr;
                    Boolean     isPrinterConnection = false;
                    //
                    // If the default PrintQueue is a connection, then we
                    // have to instantiate both a PrintServer representing the
                    // Print Server on which this queue lives and a PrintQueue
                    // representing the connection.
                    // Otherise we instantiate a PrintQueue on this LocalPrintServer
                    // 1. Test for type of default PrintQueue
                    //
                    isPrinterConnection = PrintSystemUNCPathResolver::ValidateUNCPath(defaultPrinterName);
                    if(isPrinterConnection == true)
                    {
                        //
                        // 2. If it is then, break name into ServerName and PrintQueueName;
                        //
                        PrintSystemUNCPathCracker^ cracker = gcnew PrintSystemUNCPathCracker(defaultPrinterName);
                        targetPrintServer   = gcnew PrintServer(cracker->PrintServerName,
                                                                PrintServerType::Browsable);
                        defaultPrinterName  = cracker->PrintQueueName;
                    }
                    else
                    {
                        //
                        // This means that this is not a Connection.
                        // Could be something else other than a local
                        // printer, but we don't care at this stage
                        //
                        targetPrintServer = this;
                    }
                    attributeValue->Value = gcnew PrintQueue(targetPrintServer,
                                                             defaultPrinterName);
                }

                attributeValue->IsInternallyInitialized = false;

            }
            catch (InternalPrintSystemException^ internalException)
            {
                throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.GetDefaultPrinter");
            }
        }
    }
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
void
LocalPrintServer::
GetUnInitializedData(
    array<String^>^ properties
    )
{
    for(int numOfProperties = 0;
        numOfProperties < properties->Length;
        numOfProperties++)
    {
        GetDataFromServer(properties[numOfProperties], false);
    }
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
LocalPrintServer::
ComitDirtyData(
    array<String^>^ properties
    )
{
    if (properties != nullptr)
    {
        for(int propertyIndex = 0;
            propertyIndex < properties->Length;
            propertyIndex++)
        {
            PrintProperty^  attributeValue = attributeValue = PropertiesCollection->GetProperty(properties[propertyIndex]);

            //
            // hase something table driven
            //
            if (attributeValue->Name->Equals("DefaultPrintQueue"))
            {
                try
                {
                    PrintWin32Thunk::PrinterThunkHandler::ThunkSetDefaultPrinter(GetFullPrintQueueName((PrintQueue^)attributeValue->Value));
                }
                catch (InternalPrintSystemException^ internalException)
                {
                    throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.SetDefaultPrinter");
                }
            }
        }
   }

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
LocalPrintServer::
Commit(
    void
    )
{
    VerifyAccess();

    try
    {
        ComitDirtyData(GetAlteredPropertiesFilter());
        PrintServer::Commit();
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
LocalPrintServer::
Refresh(
    void
    )
{
    VerifyAccess();

    try
    {
        GetDataFromServer("DefaultPrintQueue", true);
        PrintServer::Refresh();
    }
    catch (InternalPrintSystemException^ internalException)
    {
        throw CreatePrintServerException(internalException->HResult, "PrintSystemException.PrintServer.Refresh");
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
LocalPrintServer::
GetAlteredPropertiesFilter(
    void
    )
{
    Int32   indexInAlteredProperties = 0;

    array<String^>^ probePropertiesFilter = gcnew array<String^>(LocalPrintServer::primaryAttributeNames->Length);

    for(Int32 numOfAttributes = 0;
        numOfAttributes < LocalPrintServer::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        String^ upLevelAttribute = LocalPrintServer::primaryAttributeNames[numOfAttributes];

        if(PropertiesCollection->GetProperty(LocalPrintServer::primaryAttributeNames[numOfAttributes])->IsDirty)
        {
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

    Initialize

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
LocalPrintServer::
Initialize(
    void
    )
{
    accessVerifier = gcnew PrintSystemDispatcherObject();

    array<MulticastDelegate^>^ propertiesDelegates = CreatePropertiesDelegates();

    for(Int32 numOfPrimaryAttributes=0;
        numOfPrimaryAttributes < LocalPrintServer::primaryAttributeNames->Length;
        numOfPrimaryAttributes++)
    {
        PrintProperty^ printSystemAttributeValue = nullptr;

        printSystemAttributeValue =
        ObjectsAttributesValuesFactory::Value->Create(this->GetType(),
                                                      primaryAttributeNames[numOfPrimaryAttributes],
                                                      propertiesDelegates[numOfPrimaryAttributes]);

        PrintSystemObject::PropertiesCollection->Add(printSystemAttributeValue);
    }

}


/*++

Routine Name:

    CreatePropertiesDelegates

Routine Description:

    This method creates the delegates associated with each property
    of this object. The purpose to invoke one of these delegated is to keep
    the LocalPrintServer's properties in sync with the attribute value collection.
    The delegates are associated with the attribute value created for each of the
    object's property. The attribute value collection is updated through the thunking code
    with data coming from the Spooler service.

Arguments:

    None

Return Value:

    None

--*/
array<MulticastDelegate^>^
LocalPrintServer::
CreatePropertiesDelegates(
    void
    )
{
    array<MulticastDelegate^>^ propertiesDelegates = gcnew array<MulticastDelegate^>(primaryAttributeNames->Length);

    propertiesDelegates[0]  = gcnew PrintSystemDelegates::PrintQueueValueChanged(this,&LocalPrintServer::DefaultPrintQueue::set);

    return propertiesDelegates;
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
LocalPrintServer::
GetAllPropertiesFilter(
    void
    )
{
    //
    // Properties = Base Class Properties + Inherited Class Properties
    //
    array<String^>^ allPropertiesFilter = gcnew array<String^>(LocalPrintServer::primaryAttributeNames->Length);

    //
    // Then Add the Inherited Class Properties
    //
    for(Int32 numOfAttributes = 0;
        numOfAttributes < LocalPrintServer::primaryAttributeNames->Length;
        numOfAttributes++)
    {
        allPropertiesFilter[numOfAttributes] = LocalPrintServer::primaryAttributeNames[numOfAttributes];
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
LocalPrintServer::
GetAllPropertiesFilter(
    array<String^>^ propertiesFilter
    )
{
    array<String^>^ allPropertiesFilter = nullptr;
    Int32   numOfPrintServerProperties  = 0;

    for(Int32 index = 0; index < propertiesFilter->Length; index++)
    {
        if (LocalPrintServer::attributeNameTypes->ContainsKey(propertiesFilter[index]))
        {
            numOfPrintServerProperties++;
        }
    }

    allPropertiesFilter = gcnew array<String^>(numOfPrintServerProperties);

    for(Int32 index = 0, numOfPrintServerProperties = 0; index < propertiesFilter->Length; index++)
    {
        if (LocalPrintServer::attributeNameTypes->ContainsKey(propertiesFilter[index]))
        {
            allPropertiesFilter[numOfPrintServerProperties] = propertiesFilter[index];
        }
    }

    return allPropertiesFilter;
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
LocalPrintServer::
ConvertPropertyFilterToString(
    array<LocalPrintServerIndexedProperty>^        propertiesFilter
    )
{
    array<String^>^ propertiesFilterAsStrings = gcnew array<String^>(propertiesFilter->Length);
    Int32   numOfLocalPrintServerProperties = 0;

    for(Int32 numOfProperties = 0;
        numOfProperties < propertiesFilter->Length;
        numOfProperties++)
    {
        String^ attributeName = propertiesFilter[numOfProperties].ToString();

        if (LocalPrintServer::attributeNameTypes->ContainsKey(attributeName))
        {
            propertiesFilterAsStrings[numOfLocalPrintServerProperties++] = attributeName;
        }
    }

/*    if (numOfLocalPrintServerProperties < propertiesFilter->Length)
    {
        String^ sizedPropertiesFilterAsStrings[] = gcnew String^[numOfLocalPrintServerProperties];

        for (Int32 index = 0; index < numOfLocalPrintServerProperties; index++)
        {
            sizedPropertiesFilterAsStrings[index] = propertiesFilterAsStrings[index];
        }
    }*/

    return propertiesFilterAsStrings;
}


void
LocalPrintServer::
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


