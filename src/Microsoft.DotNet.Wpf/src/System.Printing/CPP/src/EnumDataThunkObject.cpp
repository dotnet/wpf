// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        

/*++
    Abstract:

        This file contains the implementation for EnumDataThunkObject object.
        This object enumerates the objects of a given type by calling Win32 APIs.
        The Win32 APIs to be called are determined based on the propertiesFilter
        parameter. The objects are created and only the properties in the propertiesFilter
        are populated with data. The objects are added to the printObjectsCollection.
--*/
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Collections::Specialized;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

using namespace MS::Internal::PrintWin32Thunk;

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

#ifndef  __PRINTSYSTEMATTRIBUTEVALUEFACTORY_HPP__
#include <PrintSystemAttributeValueFactory.hpp>
#endif

#ifndef  __PRINTSYSTEMOBJECTFACTORY_HPP__
#include <PrintSystemObjectFactory.hpp>
#endif

using namespace System::Printing::Activation;

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef __ENUMDATATHUNKOBJECT_HPP__
#include "EnumDataThunkObject.hpp"
#endif


using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::JobThunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::PrintQueueThunk;


/*++

Routine Name:

    EnumDataThunkObject

Routine Description:

    Constructor

Arguments:

    printingType    -   the type of the object that is going to be enumerated.

Return Value:

    N\A

--*/
EnumDataThunkObject::
EnumDataThunkObject(
    Type^   printingType
    )
{
    this->printingType = printingType;
}

/*++

Routine Name:

    EnumDataThunkObject

Routine Description:

    Private constructor

Arguments:

    None

Return Value:

    N\A

--*/
EnumDataThunkObject::
EnumDataThunkObject(
    void
    ):  printingType(nullptr)
{
}

/*++

Routine Name:

    ~EnumDataThunkObject

Routine Description:

    Destructor

Arguments:

    None

Return Value:

    N\A

--*/
EnumDataThunkObject::
~EnumDataThunkObject(
    void
    )
{
}

/*++

Routine Name:

    GetPrintSystemValuesPerPrintQueues

Routine Description:

    This method build the coverage list with the InfoLevelThunk objects, one for each Win32
    level that's going to be called. For each InfoLevelThunk object, it populates the data and then
    creates an object of the given type, sets the properties requested in the propertiesFilter
    and then adds the object to the printObjectsCollection.

Arguments:

    printerThunkHandler      - This object wraps the unmanaged Win32 printer handle
                               and has the knowledge of doing the native calls.
    printObjectsCollection   - the collection that will hold the enumerated objects.
    propertiesFilter         - The list of properties to be populated in the enumerated objects

Return Value:

    void

--*/
void
EnumDataThunkObject::
GetPrintSystemValuesPerPrintQueues(
    PrintServer^                        printServer,
    array<EnumeratedPrintQueueTypes>^   flags,
    System::Collections::
    Generic::Queue<PrintQueue^>^        printObjectsCollection,
    array<String^>^                     propertyFilter
    )
{
    InfoLevelMask           attributesMask = InfoLevelMask::NoLevel;
    InfoLevelCoverageList^  coverageList   = nullptr;

    try
    {
        //
        // Builds the bit mask for the attributes in a collection. The collections are assumed to have the same
        // attributes - fix this!
        //
        attributesMask = TypeToLevelMap::GetCoverageMaskForPropertiesFilter(printingType,
                                                                            TypeToLevelMap::OperationType::Enumeration,
                                                                            propertyFilter);

        if (attributesMask != InfoLevelMask::NoLevel)
        {
            MapEnumeratePrinterQueuesFlags(flags);

            try
            {
                coverageList = BuildCoverageListAndEnumerateData(printServer->Name,
                                                                 win32EnumerationFlags,
                                                                 attributesMask);
            }
            catch (InternalPrintSystemException^)
            {
                win32EnumerationFlags |= PRINTER_ENUM_NAME;

                coverageList = BuildCoverageListAndEnumerateData(printServer->Name,
                                                                 win32EnumerationFlags,
                                                                 attributesMask);

            }

            Hashtable^   attributeMap = TypeToLevelMap::GetAttributeMapPerType(printingType,
                                                                               TypeToLevelMap::OperationType::Enumeration);

            if (coverageList)
            {
                for(UInt32 objectIndex = 0; objectIndex < coverageList->Count; objectIndex++)
                {
                    String^             valueName            = "Attributes";
                    InfoAttributeData^  infoData             = (InfoAttributeData^)attributeMap[valueName];
                    InfoLevelThunk^     infoLevelThunk       = (InfoLevelThunk^)coverageList->
                                                                                GetInfoLevelThunk((UInt64)infoData->mask);
                    Int32               printQueueAttributes = *((Int32^)infoLevelThunk->
                                                                         GetValueFromInfoData(valueName,
                                                                                              objectIndex));

                    win32PrinterAttributeFlags = TweakTheFlags(win32PrinterAttributeFlags);

                    if ((printQueueAttributes & win32PrinterAttributeFlags) == win32PrinterAttributeFlags)
                    {
                        PrintSystemObject^ printSystemObject =
                        PrintSystemObjectFactory::Value->Instantiate(PrintQueue::typeid,
                                                                     propertyFilter);

                        for(Int32 propertyIndex = 0; propertyIndex < propertyFilter->Length; propertyIndex++)
                        {
                            String^             valueName       = propertyFilter[propertyIndex];
                            InfoAttributeData^  infoData        = (InfoAttributeData^)attributeMap[valueName];
                            InfoLevelThunk^     infoLevelThunk  = (InfoLevelThunk^)coverageList->
                                                                                   GetInfoLevelThunk((UInt64)infoData->mask);

                            if(infoLevelThunk)
                            {
                                Object^             value = infoLevelThunk->GetValueFromInfoData(valueName, objectIndex);

                                if (value)
                                {
                                    printSystemObject->get_InternalPropertiesCollection(valueName)->
                                    GetProperty(valueName)->IsInternallyInitialized = true;

                                    printSystemObject->get_InternalPropertiesCollection(valueName)->
                                    GetProperty(valueName)->Value = value;
                                }
                            }

                        }

                        printObjectsCollection->Enqueue(safe_cast<PrintQueue^>(printSystemObject));
                    }
                }
            }
        }
    }
    __finally
    {
        coverageList->Release();
    }

}

void
EnumDataThunkObject::
GetPrintSystemValuesPerPrintJobs(
    PrintQueue^                  printQueue,
    System::
    Collections::
    Generic::
    Queue<PrintSystemJobInfo^>^  printObjectsCollection,
    array<String^>^              propertyFilter,
    UInt32                       firstJobIndex,
    UInt32                       numberOfJobs
    )
{
    InfoLevelMask           attributesMask = InfoLevelMask::NoLevel;
    InfoLevelCoverageList^  coverageList   = nullptr;

    try
    {
        //
        // Builds the bit mask for the attributes in a collection.
        //
        attributesMask = TypeToLevelMap::GetCoverageMaskForPropertiesFilter(printingType,
                                                                            TypeToLevelMap::OperationType::Enumeration,
                                                                            propertyFilter);

        if (attributesMask != InfoLevelMask::NoLevel)
        {
            coverageList = BuildJobCoverageListAndEnumerateData(printQueue->PrinterThunkHandler,
                                                                attributesMask,
                                                                firstJobIndex,
                                                                numberOfJobs);

            Hashtable^   attributeMap = TypeToLevelMap::GetAttributeMapPerType(printingType,
                                                                               TypeToLevelMap::OperationType::Enumeration);

            if (coverageList)
            {
                for(UInt32 objectIndex = 0; objectIndex < coverageList->Count; objectIndex++)
                {
                    PrintSystemObject^ printSystemJobInfo =
                    PrintSystemObjectFactory::Value->InstantiateOptimized(PrintSystemJobInfo::typeid,
                                                                          printQueue,
                                                                          propertyFilter);

                    for(Int32 propertyIndex = 0; propertyIndex < propertyFilter->Length; propertyIndex++)
                    {
                        String^             valueName       = propertyFilter[propertyIndex];
                        InfoAttributeData^  infoData        = (InfoAttributeData^)attributeMap[valueName];
                        InfoLevelThunk^     infoLevelThunk  = (InfoLevelThunk^)coverageList->
                                                                               GetInfoLevelThunk((UInt64)infoData->mask);

                        if (infoLevelThunk)
                        {
                            Object^             value = infoLevelThunk->GetValueFromInfoData(valueName, objectIndex);

                            if (value)
                            {
                                printSystemJobInfo->get_InternalPropertiesCollection(valueName)->
                                GetProperty(valueName)->IsInternallyInitialized = true;

                                printSystemJobInfo->get_InternalPropertiesCollection(valueName)->
                                GetProperty(valueName)->Value = value;
                            }
                        }
                    }

                    printObjectsCollection->Enqueue(safe_cast<PrintSystemJobInfo^>(printSystemJobInfo));
                }
            }
        }
    }
    __finally
    {
        coverageList->Release();
    }
}

/*++

Routine Name:

    BuildCoverageListAndEnumerateData

Routine Description:

    Based on the unmanaged attributes mask determined from the propertiesFilter,
    this method gets the list of the InfoLevelThunk objects, one for each level
    that needs to be called. Then for each InfoLevelThunk object it will populate
    the object with the unmanaged data. Under the covers, we will call the "Enum" Win32 APIs
    to get data from the server.


Arguments:

    printerThunkHandler - this object wraps the unmanaged Win32 printer handle
                          and has the knowledge of doing the native calls.
    InfoLevelMask       - this is the mask of attributes that needs to be populated.

Return Value:

    The method returns a list of InfoLevelThunk fully populated with unmanaged "clean" data.

--*/
InfoLevelCoverageList^
EnumDataThunkObject::
BuildCoverageListAndEnumerateData(
    String^                 serverName,
    UInt32                  flags,
    InfoLevelMask           mask
    )
{
    InfoLevelCoverageList^ coverageList = nullptr;
    UInt32                 printCount   = 0;

    coverageList = TypeToLevelMap::GetThunkProfileForPrintType(printingType)->GetCoverageList(mask);

    IEnumerator^ coverageListEnumerator = coverageList->GetEnumerator();

    for ( ;coverageListEnumerator->MoveNext(); )
    {
        Win32PrinterThunk^ printerLevelThunk  = (Win32PrinterThunk^)coverageListEnumerator->Current;

        UInt32 count = printerLevelThunk->CallWin32ApiToEnumeratePrintInfoData(serverName, flags);

        if (count != printCount)
        {
            //
            // Decide what we wnat to do.
            // throw new String(S"A printer was deleted while enumerating.");
            //
        }

        printCount = count;

        if (printCount == 0)
        {
            break;
        }
    }

    coverageList->Count = printCount;

    return coverageList;
}

InfoLevelCoverageList^
EnumDataThunkObject::
BuildJobCoverageListAndEnumerateData(
    PrinterThunkHandler^    printingHandler,
    InfoLevelMask           mask,
    UInt32                  firstJobIndex,
    UInt32                  numberOfJobs
    )
{
    InfoLevelCoverageList^ coverageList = nullptr;
    UInt32                 printCount   = 0;

    coverageList = TypeToLevelMap::GetThunkProfileForPrintType(printingType)->GetCoverageList(mask);

    IEnumerator^ coverageListEnumerator = coverageList->GetEnumerator();

    for ( ;coverageListEnumerator->MoveNext(); )
    {
        Win32JobThunk^ jobLevelThunk  = (Win32JobThunk^)coverageListEnumerator->Current;

        UInt32 count = jobLevelThunk->CallWin32ApiToEnumeratePrintInfoData(printingHandler,
                                                                           firstJobIndex,
                                                                           numberOfJobs);

        if (count != printCount)
        {
            //
            // Decide what we wnat to do.
            // throw new String(S"A printer was deleted while enumerating.");
            //
        }

        printCount = count;

        if (printCount == 0)
        {
            break;
        }
    }

    coverageList->Count = printCount;

    return coverageList;
}

/*++

Routine Name:

    TweakTheFlags

Routine Description:

    This routine converts the attributes specified by the
    EnumeratedPrintQueueTypes  combination into Win32 enumeration flags.

Arguments:

    attributeFlags    - EnumeratedPrintQueueTypes flags

Return Value:

    Win32 EnumPrinters flags

--*/
UInt32
EnumDataThunkObject::
TweakTheFlags(
    UInt32  attributeFlags
    )
{
    attributeFlags &= (UInt32)(~EnumeratedPrintQueueTypes::Connections);
    attributeFlags &= (UInt32)(~EnumeratedPrintQueueTypes::Local);

    return attributeFlags;
}


void
EnumDataThunkObject::
MapEnumeratePrinterQueuesFlags(
    array<EnumeratedPrintQueueTypes>^   enumerateFlags
    )
{
    this->win32PrinterAttributeFlags = 0;
    this->win32EnumerationFlags      = 0;

    for (Int32 index = 0; index < enumerateFlags->Length; index++)
    {
        this->win32PrinterAttributeFlags |= (UInt32)enumerateFlags[index];
    }

    UInt32 miscellaneousFlags = (UInt32)( EnumeratedPrintQueueTypes::TerminalServer  |
                                          EnumeratedPrintQueueTypes::Fax             |
                                          EnumeratedPrintQueueTypes::KeepPrintedJobs |
                                          EnumeratedPrintQueueTypes::EnableBidi      |
                                          EnumeratedPrintQueueTypes::RawOnly         |
                                          EnumeratedPrintQueueTypes::WorkOffline     |
                                          EnumeratedPrintQueueTypes::Queued          |
                                          EnumeratedPrintQueueTypes::DirectPrinting  |
                                          EnumeratedPrintQueueTypes::PublishedInDirectoryServices);

    UInt32 connectionFlag = (UInt32)(EnumeratedPrintQueueTypes::Connections | 
                                     EnumeratedPrintQueueTypes::PushedUserConnection |
                                     EnumeratedPrintQueueTypes::PushedMachineConnection);


    if (win32PrinterAttributeFlags & connectionFlag)
    {
        win32EnumerationFlags |= PRINTER_ENUM_CONNECTIONS;
    }

    if (win32PrinterAttributeFlags & (UInt32)EnumeratedPrintQueueTypes::Shared &&
        !(win32PrinterAttributeFlags & miscellaneousFlags))
    {
        win32EnumerationFlags |= PRINTER_ENUM_SHARED;
    }

    if(~(connectionFlag) & win32PrinterAttributeFlags)
    {
        win32EnumerationFlags |= PRINTER_ENUM_LOCAL;
    }
}

