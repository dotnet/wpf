// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++
    Abstract:
        
        PrinterThunkingProfile - This object holds the knowledge about how a PrintQueue object
        thunks into unmanaged code. It does the mapping between the attributes 
        and Win32 levels for different types of operations, it does the level reconciliation 
        and based on a coverage mask, it creates the coverage list.

--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 

#ifndef __GENERICPRINTEREVELTHUNK_HPP__
#include "GenericPrinterLevelThunk.hpp"
#endif

#ifndef __GENERICPRINTERTHUNKFILTER_HPP__
#include "GenericPrinterThunkFilter.hpp"
#endif



using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::PrintQueueThunk;

/*++

Routine Name:   

    RegisterAttributeMap

Routine Description:

    Called by the static constructor. It registers the 
    attributes maps for each type of operations. 

Arguments:

    None.
    
Return Value:

    N\A

--*/
void
PrinterThunkingProfile::
RegisterAttributeMap(
    void
    )
{
    for(int numOfAttributes = 0;
        numOfAttributes < attributeNames->Length;
        numOfAttributes++)
    {
        getAttributeMap->Add(attributeNames[numOfAttributes], 
                             attributeLevelCoverageForGetOperations[numOfAttributes]);
        setAttributeMap->Add(attributeNames[numOfAttributes], 
                             attributeLevelCoverageForSetOperations[numOfAttributes]);
        enumAttributeMap->Add(attributeNames[numOfAttributes], 
                              attributeLevelCoverageForEnumOperations[numOfAttributes]);
    }     
}

/*++

Routine Name:   

    GetCoverageList

Routine Description:

    Given a mask, it build the coverage list for the PrintQueue type.

Arguments:

    coverageMask - bit mask of levels that cover a given attribute collection
    
Return Value:

    Coverage list

--*/
InfoLevelCoverageList^
PrinterThunkingProfile::
GetCoverageList(
    InfoLevelMask   coverageMask
    )
{
    InfoLevelCoverageList^  coverageList = gcnew InfoLevelCoverageList;

    for (int level = 1; level < levelMaskTable->Length; level++)
    {
        if ((UInt64)PrinterThunkingProfile::levelMaskTable[level] & (UInt64)coverageMask)
        {
            coverageList->Add(gcnew Win32PrinterThunk(level, levelMaskTable[level]));
        }
    }

    return coverageList;
}

/*++

Routine Name:   

    GetStaticAttributeMapForEnumOperations

Routine Description:

    Static method that returns the attribute mask for enum operations

Arguments:

    None
    
Return Value:

    Attribute map for enum operations

--*/
Hashtable^
PrinterThunkingProfile::
GetStaticAttributeMapForEnumOperations(
    void
    )
{
    return enumAttributeMap;        
}

/*++

Routine Name:   

    GetStaticAttributeMapForGetOperations

Routine Description:

    Static method that returns the attribute mask for get operations

Arguments:

    None
    
Return Value:

    Attribute map for get operations

--*/
Hashtable^
PrinterThunkingProfile::
GetStaticAttributeMapForGetOperations(
    void
    )
{
    return getAttributeMap;        
}

/*++

Routine Name:   

    GetStaticAttributeMapForSetOperations

Routine Description:

    Static method that returns the attribute mask for set operations

Arguments:

    None
    
Return Value:

    Attribute map for set operations

--*/
Hashtable^
PrinterThunkingProfile::
GetStaticAttributeMapForSetOperations(
    void
    )
{
    return setAttributeMap;        
}

/*++

Routine Name:   

    ReconcileMask

Routine Description:

    This method is called to remove redundancies. This is hard coded per type.
    The idea is that the mask has levels that cover the same attributes and 
    we want to pick the one that is the lowest cost. 
    The levels that overlap are 2 and X. 2 has a bigger cost associated with
    and we want to pick X. 

Arguments:

    mask
    
Return Value:

    reconciled mask

--*/
UInt64
PrinterThunkingProfile::
ReconcileMask(
    UInt64  mask
    )
{
    if (mask & (UInt64)InfoLevelMask::LevelOne)
    {
        mask &= (UInt64)InfoLevelMask::LevelOne;
    }
    else if (mask & (UInt64)InfoLevelMask::LevelThree)
    {
        mask &= (UInt64)InfoLevelMask::LevelThree;
    }
    else if (mask & (UInt64)InfoLevelMask::LevelFour)
    {
        mask &= (UInt64)InfoLevelMask::LevelFour;
    }
    else if (mask & (UInt64)InfoLevelMask::LevelFive)
    {
        mask &= (UInt64)InfoLevelMask::LevelFive;
    }
    else if (mask & (UInt64)InfoLevelMask::LevelSix)
    {
        mask &= (UInt64)InfoLevelMask::LevelSix;
    }

    return mask;
}
