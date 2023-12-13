// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        This file contains the implementation for GetDataThunkObject object.
        This object populates the PrintSystemObject with data by calling Win32 APIs.
        The Win32 APIs to be called are determined based on the propertiesFilter
        parameter.

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
using namespace System::Threading;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

using namespace MS::Internal::PrintWin32Thunk;

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::JobThunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::PrintQueueThunk;

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef __GETDATATHUNKOBJECT_HPP__
#include "GetDataThunkObject.hpp"
#endif



/*++

Routine Name:

    GetDataThunkObject

Routine Description:

    Constructor

Arguments:

    printingType    -   the type of the object whose properties are going to be refreshed.

Return Value:

    N\A

--*/
GetDataThunkObject::
GetDataThunkObject(
    Type^   printingType
    ) : cookie(nullptr)
{
    if (!(this->printingType = printingType))
    {
        InternalExceptionResourceManager^ manager = gcnew InternalExceptionResourceManager();

        throw gcnew ArgumentException(manager->GetString("ArgumentException.InvalidValue",
                                                         Thread::CurrentThread->CurrentUICulture),
                                      "printingType");
    }
}

/*++

Routine Name:

    GetDataThunkObject

Routine Description:

    Private constructor

Arguments:

    None

Return Value:

    N\A

--*/
GetDataThunkObject::
GetDataThunkObject(
    void
    ):cookie(nullptr)
{
}

/*++

Routine Name:

    ~GetDataThunkObject

Routine Description:

    Destructor

Arguments:

    None

Return Value:

    N\A

--*/
GetDataThunkObject::
~GetDataThunkObject(
    void
    )
{
}

/*++

Routine Name:

    PopulatePrintSystemObject

Routine Description:

    This method refreshes the printSystemObject's properties specified in
    the propertiesFilter.

Arguments:

    printerThunkHandler - this object wraps the unmanaged Win32 printer handle
                          and has the knowledge of doing the native calls.
    printSystemObject   - The object that it going to be refreshed.
    propertiesFilter    - The list of properties to be refreshed

Return Value:

    True if the operation succeeded.

--*/
bool
GetDataThunkObject::
PopulatePrintSystemObject(
    PrinterThunkHandler^    printingHandler,
    PrintSystemObject^      printSystemObject,
    array<String^>^         propertiesFilter
    )
{
    bool                    returnValue    = false;
    InfoLevelMask           attributesMask = InfoLevelMask::NoLevel;
    InfoLevelCoverageList^  coverageList   = nullptr;

    try
    {
        if (!printingHandler)
        {
            throw gcnew ArgumentNullException("printingHandler");
        }
        if (!printSystemObject)
        {
            throw gcnew ArgumentNullException("printSystemObject");
        }
        if (!propertiesFilter)
        {
            throw gcnew ArgumentNullException("propertiesFilter");
        }
        //
        // Builds the bit mask for the attributes in a collection.
        //
        attributesMask = TypeToLevelMap::GetCoverageMaskForPropertiesFilter(printingType,
                                                                            TypeToLevelMap::OperationType::Get,
                                                                            propertiesFilter);

        if (attributesMask != InfoLevelMask::NoLevel)
        {
            coverageList = BuildCoverageListAndGetData(printingHandler, attributesMask);

            if (coverageList)
            {
                //
                // Call the Win32 APIs and populate the collection.
                //
                returnValue = PopulateAttributesFromCoverageList(printSystemObject,
                                                                 propertiesFilter,
                                                                 coverageList);
            }
        }
    }
    __finally
    {
        if (coverageList)
        {
            delete coverageList;
        }
    }

    return returnValue;
}

/*++

Routine Name:

    BuildCoverageListAndGetData

Routine Description:

    Based on the unmanaged attributes mask determined from the propertiesFilter,
    this method gets the list of the InfoLevelThunk objects, one for each level
    that needs to be called. Then for each InfoLevelThunk object it will populate
    the object with the unmanaged data. Under the covers, we will call the "Get" Win32 APIs
    to get data from the server.

Arguments:

    printerThunkHandler - this object wraps the unmanaged Win32 printer handle
                          and has the knowledge of doing the native calls.
    InfoLevelMask       - this is the mask of attributes that needs to be commited.

Return Value:

    The method returns a list of InfoLevelThunk fully populated with unmanaged "clean" data.

--*/
InfoLevelCoverageList^
GetDataThunkObject::
BuildCoverageListAndGetData(
    PrinterThunkHandler^    printingHandler,
    InfoLevelMask           mask
    )
{
    InfoLevelCoverageList^ coverageList = TypeToLevelMap::GetThunkProfileForPrintType(printingType)->GetCoverageList(mask);

    IEnumerator^ coverageListEnumerator = coverageList->GetEnumerator();

    for ( ;coverageListEnumerator->MoveNext(); )
    {
        InfoLevelThunk^ printerLevelThunk  = (InfoLevelThunk^)coverageListEnumerator->Current;

        printerLevelThunk->CallWin32ApiToGetPrintInfoData(printingHandler, Cookie);
    }

    return coverageList;
}

/*++

Routine Name:

    PopulateAttributesFromCoverageList

Routine Description:

    This method build the coverage list with the InfoLevelThunk objects, one for each Win32
    level that's going to be called. For each InfoLevelThunk object, it populates the data and then
    creates an object of the given type, sets the properties requested in the propertiesFilter
    and then adds the object to the printObjectsCollection.

Arguments:

    printSystemObject   - The object that it going to commit the dirty data.
    propertiesFilter    - The list of dirty propertires
    coverageList        - The coverage list made out of the InfoLevelThunk objects,
                          one for each Win32 level that needs to be called.

Return Value:

    void

--*/
bool
GetDataThunkObject::
PopulateAttributesFromCoverageList(
    PrintSystemObject^          printSystemObject,
    array<String^>^             propertiesFilter,
    InfoLevelCoverageList^      coverageList
    )
{
    Hashtable^   attributeMap = TypeToLevelMap::GetAttributeMapPerType(printingType, TypeToLevelMap::OperationType::Get);
    //
    // For each attribute in the collection, get the bit mask and populate it.
    //
    for (Int32 numOfProperties=0;
            numOfProperties < propertiesFilter->Length;
            numOfProperties++)
    {
        PrintProperty^       attributeValue = printSystemObject->
                                              get_InternalPropertiesCollection(propertiesFilter[numOfProperties])->
                                              GetProperty(propertiesFilter[numOfProperties]);
        InfoAttributeData^   infoData       = (InfoAttributeData^)attributeMap[attributeValue->Name];
        UInt64               attributeMask  = (UInt64)infoData->mask;

        InfoLevelThunk^ infoLevelThunk = coverageList->GetInfoLevelThunk(attributeMask);
        attributeValue->IsInternallyInitialized = true;

        if (infoLevelThunk)
        {
            attributeValue->Value = infoLevelThunk->GetValueFromInfoData(attributeValue->Name);
        }
        //
        // This line is intended to make sure that the state is reset. In only one
        // scneario and that is of the "Name" property and in condidtion of local
        // printer I can't reset the state without tremendous code complications.
        //
        attributeValue->IsInternallyInitialized = false;
    }

    return true;
}

void
GetDataThunkObject::Cookie::
set(
    Object^ internalCookie
    )
{
    cookie = internalCookie;
}

Object^
GetDataThunkObject::Cookie::
get(
    void
    )
{
    return cookie;
}
