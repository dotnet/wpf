// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        This file contains the implementation for SetDataThunkObject object.
        This object commits the dirty data in the PrintSystemObject by calling Win32 APIs.
        The propertiesFilter specify the set of dirty properties.
        The Win32 APIs to be called are determined based on the propertiesFilter
        parameter.

--*/
#include "win32inc.hpp"

using namespace System;
using namespace System::IO;
using namespace System::Collections;
using namespace System::Collections::ObjectModel;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace System::Xml;
using namespace System::Xml::XPath;
using namespace System::Collections::Specialized;

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif

using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;


#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif

#ifndef  __GETDATATHUNKOBJECT_HPP__
#include <SetDataThunkObject.hpp>
#endif


using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::JobThunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::PrintQueueThunk;

/*++

Routine Name:

    SetDataThunkObject

Routine Description:

    Constructor

Arguments:

    printingType    -   the type of the object whose dirty data is going to be commited.

Return Value:

    N\A

--*/
SetDataThunkObject::
SetDataThunkObject(
    Type^   printingType
    )
{
    this->printingType = printingType;
}


/*++

Routine Name:

    ~SetDataThunkObject

Routine Description:

    Destructor

Arguments:

    N/A

Return Value:

    N\A

--*/
SetDataThunkObject::
~SetDataThunkObject(
    void
    )
{
}

/*++

Routine Name:

    BuildCoverageListToSetData

Routine Description:

    Based on the unmanaged attributes mask determined from the dirty properties,
    this method gets the list of the InfoLevelThunk objects, one for each level
    that needs to be called to cover the list of dirty properties. Then for each
    InfoLevelThunk obejct it will populate the object for the commit, by populating
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
SetDataThunkObject::
BuildCoverageListToSetData(
    PrinterThunkHandler^    printerThunkHandler,
    InfoLevelMask           mask
    )
{
    InfoLevelCoverageList^ coverageList = nullptr;

    coverageList = TypeToLevelMap::GetThunkProfileForPrintType(printingType)->GetCoverageList(mask);

    IEnumerator^ coverageListEnumerator = coverageList->GetEnumerator();

    for ( ;coverageListEnumerator->MoveNext(); )
    {
        InfoLevelThunk^ infoLevelThunk  = (InfoLevelThunk^)coverageListEnumerator->Current;

        infoLevelThunk->BeginCallWin32ApiToSetPrintInfoData(printerThunkHandler);
    }

    return coverageList;
}

/*++

Routine Name:

    SetDataFromCoverageList

Routine Description:

    For each InfoLevelThunk object it calls the method that commits the data to the server.

Arguments:

    printerThunkHandler - this object wraps the unmanaged Win32 printer handle
                          and has the knowledge of doing the native calls.
    coverageList        - list of InfoLevelThunk fully populated with unmanaged "clean" data.
    printingType        - type of the object being commited.

Return Value:

    true if succeeded. The method throws PrintCommitAttributesException
    if one or more attributes couldn't be commited.

--*/
bool
SetDataThunkObject::
SetDataFromCoverageList(
    PrinterThunkHandler^    printingHandler,
    array<String^>^         propertiesFilter,
    InfoLevelCoverageList^  coverageList,
    Type^                   setDataType
    )
{
    bool                returnValue            = false;
    Win32PrinterThunk^  printerLevelThunk      = nullptr;

    try
    {
        IEnumerator^ coverageListEnumerator = coverageList->GetEnumerator();

        for ( ;coverageListEnumerator->MoveNext(); )
        {
            printerLevelThunk  = (Win32PrinterThunk^)coverageListEnumerator->Current;

            //
            // We don'r check the return value. The call throws if something fails.
            //
            printerLevelThunk->EndCallWin32ApiToSetPrintInfoData(printingHandler);
        }

        returnValue = true;
    }
    catch (InternalPrintSystemException^ internalException)
    {
        Collection<String^>^ committedAttributes = gcnew Collection<String^>();
        Collection<String^>^ failedAttributes   = gcnew Collection<String^>();

        GetCommitedAndFailedAttributes(propertiesFilter,
                                       coverageList,
                                       committedAttributes,
                                       failedAttributes);

        throw CreatePrintCommitAttributesException(internalException->HResult,
                                                   committedAttributes,
                                                   failedAttributes);
    }

    return returnValue;
}

/*++

Routine Name:

    CreatePrintCommitAttributesException

Routine Description:

    Creates an instance of PrintCommitAttributesException

Arguments:

    hResult                 - Error code
    committedAttributes     - List of attributes that were commited.
    committedAttributes     - List of attributes that were not commited.

Return Value:

    Exception object

--*/
__declspec(noinline)
Exception^
SetDataThunkObject::
CreatePrintCommitAttributesException (
    int                  hResult,
    Collection<String^>^ committedAttributes,
    Collection<String^>^ failedAttributes
    )
{
    return gcnew PrintCommitAttributesException(hResult, committedAttributes, failedAttributes);
}

/*++

Routine Name:

    GetCommitedAndFailedAttributes

Routine Description:

    For each InfoLevelThunk object it calls the method that commits the data to the server.

Arguments:

    propertiesFilter     - The list of dirty properties
    coverageList         - List of InfoLevelThunk.
    committedAttributes   - ArrayList of String objects that represent the names of the
                           attributes that are were succesfully commited to the Spooler service.
    failedAttributes     - ArrayList of String objects that represent the names of the
                           attributes that are failed to be commited to the Spooler service.

Return Value:

    void

--*/
void
SetDataThunkObject::
GetCommitedAndFailedAttributes(
    array<String^>^         propertiesFilter,
    InfoLevelCoverageList^  coverageList,
    Collection<String^>^    committedAttributes,
    Collection<String^>^    failedAttributes
 )
{
    if (committedAttributes && failedAttributes   &&
        propertiesFilter   && coverageList)
    {
        Hashtable^   attributeMap = TypeToLevelMap::GetAttributeMapPerType(printingType,
                                                                           TypeToLevelMap::OperationType::Set);

        for (Int32 index = 0; index < propertiesFilter->Length; index++)
        {
            InfoAttributeData^  infoData       = (InfoAttributeData^)attributeMap[propertiesFilter[index]];
            UInt64              attributeMask  = (UInt64)infoData->mask;
            InfoLevelThunk^     infoLevelThunk = coverageList->GetInfoLevelThunk(attributeMask);

            if (infoLevelThunk->Succeeded)
            {
                committedAttributes->Add(propertiesFilter[index]);
            }
            else
            {
                failedAttributes->Add(propertiesFilter[index]);
            }
        }
    }
}

/*++

Routine Name:

    CommitDataFromPrintSystemObject

Routine Description:

    This method build the coverage list with the InfoLevelThunk objects, one for each Win32
    level that's going to be called. For each InfoLevelThunk object, it sets the dirty data
    from the printSystemObject in the coverageList and then commits the data.

Arguments:

    printerThunkHandler - this object wraps the unmanaged Win32 printer handle
                          and has the knowledge of doing the native calls.
    printSystemObject   - The object that it going to commit the dirty data.
    propertiesFilter    - The list of dirty properties

Return Value:

    For now, False True. To be determined what we want to do.

--*/
bool
SetDataThunkObject::
CommitDataFromPrintSystemObject(
    PrinterThunkHandler^        printerThunkHandler,
    PrintSystemObject^          printSystemObject,
    array<String^>^             propertiesFilter
    )
{
    bool                    returnValue    = false;
    InfoLevelMask           attributesMask = InfoLevelMask::NoLevel;
    InfoLevelCoverageList^  coverageList   = nullptr;

    try
    {
        //
        // Builds the bit mask for the attributes in a collection.
        //
        attributesMask = TypeToLevelMap::GetCoverageMaskForPropertiesFilter(printingType,
                                                                            TypeToLevelMap::OperationType::Set,
                                                                            propertiesFilter);

        if (attributesMask != InfoLevelMask::NoLevel)
        {
            //
            // BuildCoverageListToSetData builds the Win32 buffer by calling the Win32 Get APIs.
            // If one of the Get APIs fail, then we are going to abort the whole set, as opposed
            // doing a partial commit.
            //
            coverageList = BuildCoverageListToSetData(printerThunkHandler, attributesMask);

            if (coverageList)
            {
                //
                // SetAttributesFromCoverageList sets the dirty data in the Win32 buffer.
                // If anything throws in here, then we are not going to do a partial commit.
                //
                returnValue = SetAttributesFromCoverageList(printSystemObject, propertiesFilter, coverageList);

                if (returnValue)
                {
                    //
                    // SetDataFromCoverageList does the real commit and calls the Win32 Set APIs.
                    // If we catch a PrintSystemException it means that one of the Set calls failed.
                    // We are going to abort the commit, since we expect that the next of the calls will fail.
                    // We are going to wrap the exception in a "commit" exception where we report
                    // which of the attributes were succesfully commited.
                    //
                    returnValue = SetDataFromCoverageList(printerThunkHandler,
                                                          propertiesFilter,
                                                          coverageList,
                                                          printSystemObject->GetType());
                }
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

    SetAttributesFromCoverageList

Routine Description:

    For each InfoLevelThunk object, it sets the dirty data from the printSystemObject in the
    coverageList. The coverageList is now in a state where it is ready to call the Win32 APIs
    to commit the data to the server.

Arguments:

    printSystemObject   - The object that it going to commit the dirty data.
    propertiesFilter    - The list of dirty properties
    coverageList        - The coverage list made out of the InfoLevelThunk objects,
                          one for each Win32 level that needs to be called.

Return Value:

    For now, False True. To be determined what we want to do.

--*/
bool
SetDataThunkObject::
SetAttributesFromCoverageList(
    PrintSystemObject^                      printSystemObject,
    array<String^>^                         propertiesFilter,
    InfoLevelCoverageList^                  coverageList
    )
{
    bool returnValue = false;

    Hashtable^   attributeMap = TypeToLevelMap::GetAttributeMapPerType(printingType,
                                                                        TypeToLevelMap::OperationType::Set);

    for (Int32 numOfProperties = 0;
            numOfProperties < propertiesFilter->Length;
            numOfProperties++)
    {
        PrintProperty^      attributeValue = printSystemObject->
                                             get_InternalPropertiesCollection(propertiesFilter[numOfProperties])->
                                             GetProperty(propertiesFilter[numOfProperties]);
        InfoAttributeData^  infoData       = (InfoAttributeData^)attributeMap[attributeValue->Name];
        InfoLevelThunk^     infoLevelThunk = coverageList->GetInfoLevelThunk((UInt64)infoData->mask);

        infoLevelThunk->SetValueFromAttributeValue(attributeValue->Name, attributeValue->Value);
    }

    returnValue = true;

    return returnValue;
}
