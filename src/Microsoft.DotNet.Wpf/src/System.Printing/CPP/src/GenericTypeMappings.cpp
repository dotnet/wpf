// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:

        TypeToLevelMap - utility class that does the type mapping between the LAPI and
        thunk objects, for each type of operation (Get, Set, Enum).

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

#ifndef  __PRINTSYSTEMINC_HPP__
#include <PrintSystemInc.hpp>
#endif

using namespace System::Printing;
using namespace System::Printing::IndexedProperties;

#ifndef  __GENERICTHUNKINGINC_HPP__
#include <GenericThunkingInc.hpp>
#endif



using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;

/*++

Routine Name:

    BuildAttributesMapForGetOperations

Routine Description:

    Static method that builds the table of delegates per PrintSubSystem object type.

Arguments:

    None

Return Value:

    N\A

--*/
void
TypeToLevelMap::
BuildAttributesMapForGetOperations(
    void
    )
{
    perTypeAttributesMapForGetOperations->Add(System::Printing::PrintQueue::typeid,
                                              gcnew TypeToLevelMap::GetStaticAttributeMap(&PrintQueueThunk::PrinterThunkingProfile::GetStaticAttributeMapForGetOperations));

    perTypeAttributesMapForGetOperations->Add(System::Printing::PrintSystemJobInfo::typeid,
                                              gcnew TypeToLevelMap::GetStaticAttributeMap(&JobThunk::JobThunkingProfile::GetStaticAttributeMap));

    perTypeAttributesMapForGetOperations->Add(System::Printing::PrintDriver::typeid, 
                                              gcnew TypeToLevelMap::GetStaticAttributeMap(&DriverThunk::DriverThunkingProfile::GetStaticAttributeMap)); 
}

/*++

Routine Name:

    BuildAttributesMapForSetOperations

Routine Description:

    Static method that builds the table of delegates per PrintSubSystem object type.

Arguments:

    None

Return Value:

    N\A

--*/
void
TypeToLevelMap::
BuildAttributesMapForSetOperations(
    void
    )
{
    perTypeAttributesMapForSetOperations->Add(System::Printing::PrintQueue::typeid,
                                              gcnew TypeToLevelMap::GetStaticAttributeMap(&PrintQueueThunk::PrinterThunkingProfile::GetStaticAttributeMapForSetOperations));

    perTypeAttributesMapForSetOperations->Add(System::Printing::PrintSystemJobInfo::typeid,
                                              gcnew TypeToLevelMap::GetStaticAttributeMap(&JobThunk::JobThunkingProfile::GetStaticAttributeMap));

    perTypeAttributesMapForSetOperations->Add(System::Printing::PrintDriver::typeid, 
                                              gcnew TypeToLevelMap::GetStaticAttributeMap(&DriverThunk::DriverThunkingProfile::GetStaticAttributeMap)); 
}

/*++

Routine Name:

    BuildAttributesMapForEnumOperations

Routine Description:

    Static method that builds the table of delegates per PrintSubSystem object type.

Arguments:

    None

Return Value:

    N\A

--*/
void
TypeToLevelMap::
BuildAttributesMapForEnumOperations(
    void
    )
{
    perTypeAttributesMapForEnumOperations->Add(System::Printing::PrintQueue::typeid,
                                               gcnew TypeToLevelMap::GetStaticAttributeMap(&PrintQueueThunk::PrinterThunkingProfile::GetStaticAttributeMapForEnumOperations));

    perTypeAttributesMapForEnumOperations->Add(System::Printing::PrintSystemJobInfo::typeid,
                                               gcnew TypeToLevelMap::GetStaticAttributeMap(&JobThunk::JobThunkingProfile::GetStaticAttributeMap));
    
    perTypeAttributesMapForEnumOperations->Add(System::Printing::PrintDriver::typeid, 
                                               gcnew TypeToLevelMap::GetStaticAttributeMap(&DriverThunk::DriverThunkingProfile::GetStaticAttributeMap)); 
}

/*++

Routine Name:

    BuildReconcileMask

Routine Description:

    Static method that builds the table of delegates per PrintSubSystem object type.

Arguments:

    None

Return Value:

    N\A

--*/
void
TypeToLevelMap::
BuildReconcileMask(
    void
    )
{
    perTypeReconcileMap->Add(System::Printing::PrintQueue::typeid,
                             gcnew TypeToLevelMap::ReconcileMask(&PrintQueueThunk::PrinterThunkingProfile::ReconcileMask));

    perTypeReconcileMap->Add(System::Printing::PrintSystemJobInfo::typeid,
                             gcnew TypeToLevelMap::ReconcileMask(&JobThunk::JobThunkingProfile::ReconcileMask));

    perTypeReconcileMap->Add(System::Printing::PrintDriver::typeid, 
                             gcnew TypeToLevelMap::ReconcileMask(&DriverThunk::DriverThunkingProfile::ReconcileMask));         
}

/*++

Routine Name:

    GetStaticAttributesMapPerTypeForGetOperations

Routine Description:

    Static method that looks up the table for a given PrintSubSystem type and returns the
    associated delegate.

Arguments:

    printingType - type to be looked up

Return Value:

    N\A

--*/
TypeToLevelMap::GetStaticAttributeMap^
TypeToLevelMap::
GetStaticAttributesMapPerTypeForGetOperations(
    Type^   printingType
    )
{
    return (GetStaticAttributeMap^)perTypeAttributesMapForGetOperations[printingType];
}

/*++

Routine Name:

    GetStaticAttributesMapPerTypeForEnumOperations

Routine Description:

    Static method that looks up the table for a given PrintSubSystem type
    and returns the associated delegate.

Arguments:

    printingType - type to be looked up

Return Value:

    N\A

--*/
TypeToLevelMap::GetStaticAttributeMap^
TypeToLevelMap::
GetStaticAttributesMapPerTypeForEnumOperations(
    Type^   printingType
    )
{
    return (GetStaticAttributeMap^)perTypeAttributesMapForEnumOperations[printingType];

}

/*++

Routine Name:

    GetStaticAttributesMapPerTypeForSetOperations

Routine Description:

    Static method that looks up the table for a given PrintSubSystem type
    and returns the associated delegate.

Arguments:

    printingType - type to be looked up

Return Value:

    N\A

--*/
TypeToLevelMap::GetStaticAttributeMap^
TypeToLevelMap::
GetStaticAttributesMapPerTypeForSetOperations(
    Type^   printingType
    )
{
    return (GetStaticAttributeMap^)perTypeAttributesMapForSetOperations[printingType];
}

/*++

Routine Name:

    GetStaticReconcileMaskPerType

Routine Description:

    Static method that looks up the table for a given PrintSubSystem type
    and returns the associated delegate.

Arguments:

    printingType - type to be looked up

Return Value:

    N\A

--*/
TypeToLevelMap::ReconcileMask^
TypeToLevelMap::
GetStaticReconcileMaskPerType(
    Type^    printingType
    )
{
    return (ReconcileMask^)perTypeReconcileMap[printingType];
}

/*++

Routine Name:

    GetThunkProfileForPrintType

Routine Description:

    Static method that creates the thunking profile object that corresponds to the
    given PrintSubSystem type. The thunking profile obejct has the "know how" to
    thunk the PrintSubSystem attributes associated with the PrintSubSystem type.

Arguments:

    printingType - type to be thunked up

Return Value:

    thunking profile object

--*/
IThunkingProfile^
TypeToLevelMap::
GetThunkProfileForPrintType(
    Type^    printingType
    )
{
    //
    // Decide if you want a hash table of an if statement is faster
    //
    IThunkingProfile^    thunkProfile = nullptr;

    if (printingType == System::Printing::PrintQueue::typeid)
    {
        thunkProfile = gcnew PrintQueueThunk::PrinterThunkingProfile;
    }
    else if (printingType == System::Printing::PrintDriver::typeid)
    {
        thunkProfile = gcnew DriverThunk::DriverThunkingProfile;
    }
    else if (printingType == System::Printing::PrintSystemJobInfo::typeid)
    {
        thunkProfile = gcnew JobThunk::JobThunkingProfile;
    }

    return thunkProfile;
}


/*++

Routine Name:

    GetAttributeMapPerType

Routine Description:

    Static method that looks up attribute map for a given PrintSubSystem type
    and a given operation.

Arguments:

    printingType - type to be looked up

Return Value:

    N\A

--*/
Hashtable^
TypeToLevelMap::
GetAttributeMapPerType(
    Type^                           printingType,
    TypeToLevelMap::OperationType   operationType
    )
{
    GetStaticAttributeMap^ attributeMapGetter = nullptr;

    switch (operationType)
    {
        case OperationType::Get:
        {
            attributeMapGetter = GetStaticAttributesMapPerTypeForGetOperations(printingType);
            break;
        }

        case OperationType::Enumeration:
        {
            attributeMapGetter = GetStaticAttributesMapPerTypeForEnumOperations(printingType);
            break;
        }

        case OperationType::Set:
        {
            attributeMapGetter = GetStaticAttributesMapPerTypeForSetOperations(printingType);
            break;
        }

        default:
        {
            break;
        }
    }

    return attributeMapGetter->Invoke();
}

/*++

Routine Name:

    InvokeReconcileMaskPerType

Routine Description:

    Static method that invokes the reconcile delegate associated with a given
    PrintSubSystem type.

Arguments:

    printingType - PrintSubSystem type
    mask         - level mask to be reconciled

Return Value:

    N\A

--*/
UInt64
TypeToLevelMap::
InvokeReconcileMaskPerType(
    Type^           printingType,
    InfoLevelMask   mask
    )
{
    ReconcileMask^ levelReconciliator = GetStaticReconcileMaskPerType(printingType);
    return levelReconciliator->Invoke((UInt64)mask);
}

/*++

Routine Name:

    GetCoverageMaskForPropertiesFilter

Routine Description:

    Static method that determines the Win32 levels that need to be called
    for a given operation to cover the attributes in the propertiesFilter array

Arguments:

    printingType     - PrintSubSystem type to be looked up
    operationType    - type of operation that requires thunking
    propertiesFilter - array of strings that hold the names of the arrtibutes that
                       need to be thunked

Return Value:

    mask of levels that cover the given propertiesFilter

--*/
InfoLevelMask
TypeToLevelMap::
GetCoverageMaskForPropertiesFilter(
    Type^                           printingType,
    TypeToLevelMap::OperationType   operationType,
    array<String^>^                 propertiesFilter
    )
{
    UInt64       mightHaveLevelsMask = 0;
    UInt64       mustHaveLevelsMask  = 0;
    Hashtable^   attributeMap        = nullptr;

    try
    {
        if (propertiesFilter &&
            (attributeMap = GetAttributeMapPerType(printingType, operationType)))
        {
            IEnumerator^ propertiesFilterEnumerator = propertiesFilter->GetEnumerator();

            //
            // For each attribute in the collection gets the name of the value and maps it in the per type attribute map.
            // Try switch case - could be faster.
            //
            for ( ;propertiesFilterEnumerator->MoveNext(); )
            {
                //
                // Map attribute and get the bit mask.
                //
                InfoAttributeData^ infoData = (InfoAttributeData^)attributeMap[propertiesFilterEnumerator->Current];

                //
                // If the attribute shares any mustHave levels with the previous processed
                // attributes, then just continue, the attribute is already covered.
                //
                if (mustHaveLevelsMask & (UInt64)infoData->mask)
                {
                    continue;
                }
                else if (infoData->isSingleLevelCovered)
                {
                    //
                    // If the attribute is covered by only one level, then
                    // that level is a mustHave.
                    //
                    mustHaveLevelsMask |= (UInt64)infoData->mask;
                }
                else
                {
                    //
                    // For the first entry just initialize the masks.
                    //
                    if (mightHaveLevelsMask == 0)
                    {
                        mightHaveLevelsMask = (UInt64)infoData->mask;
                    }
                    else
                    {
                        mightHaveLevelsMask &= (UInt64)infoData->mask;
                    }
                }
            }

            //
            // If the mightHaveLevelsMask is different than NULL, then we have levels that
            // common between attributes. The levels could be redundant. For each type,
            // we have a different way to figure which is the best pick. Before sending the
            // mightHaveLevelsMask to the type to resolve redundancies, check to see whether
            // at least on might level is already in the must list. If it is, then we don't care
            // for the rest of the levels, since the attribute will be covered by the level in the must.
            //
            if (mightHaveLevelsMask & mustHaveLevelsMask)
            {
                mightHaveLevelsMask = 0;
            }
            else
            {
                mightHaveLevelsMask = InvokeReconcileMaskPerType(printingType, static_cast<InfoLevelMask>(mightHaveLevelsMask));
            }
            //
            // Add the must have attributes. There shouldn't be any common attributes between
            // mustHave and the attributesMask.
            //
        }
    }
    __finally
    {
    }

    return static_cast<InfoLevelMask>(mightHaveLevelsMask | mustHaveLevelsMask);
}

