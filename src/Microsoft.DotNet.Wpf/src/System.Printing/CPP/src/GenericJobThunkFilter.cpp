// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                       
    Abstract:
        
        JobThunkingProfile - This object holds the knowledge about how a Job object
        thunks into unmanaged code. It does the mapping between the attributes 
        and Win32 levels, it does the level reconciliation and based on a 
        coverage mask, it creates the coverage list.
--*/
#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 

#ifndef __GENERICJOBINFOLEVELTHUNK_HPP__
#include "GenericJobLevelThunk.hpp"
#endif

#ifndef __GENERICJOBTHUNKFILTER_HPP__
#include "GenericJobThunkFilter.hpp"
#endif



using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping::JobThunk;
using namespace MS::Internal::PrintWin32Thunk::DirectInteropForJob;

/*++

Routine Name:   

    RegisterAttributeMap

Routine Description:

    Called by the static constructor. It registers the 
    attributes maps for each type of operations. For Job we have 
    the same map for all kinds of operations.

Arguments:

    None.
    
Return Value:

    N\A

--*/
void
JobThunkingProfile::
RegisterAttributeMap(
    void
    )
{
    for(int numOfAttributes = 0;
        numOfAttributes < attributeNames->Length;
        numOfAttributes++)
    {
        attributeMap->Add(attributeNames[numOfAttributes], 
                          attributeLevelCoverage[numOfAttributes]);
    }  

}

/*++

Routine Name:   

    GetCoverageList

Routine Description:

    Given a mask, it build the coverage list for the Job type.

Arguments:

    coverageMask - bit mask of levels that cover a given attribute collection
    
Return Value:

    Coverage list

--*/
InfoLevelCoverageList^
JobThunkingProfile::
GetCoverageList(
    InfoLevelMask coverageMask
    )
{
    InfoLevelCoverageList^  coverageList = gcnew InfoLevelCoverageList;

    for (int level = 1; level < levelMaskTable->Length; level++)
    {
        if ((UInt64)levelMaskTable[level] & (UInt64)coverageMask)
        {
            coverageList->Add(gcnew Win32JobThunk(level, levelMaskTable[level]));
        }
    }

    return coverageList;
}

/*++

Routine Name:   

    GetStaticAttributeMap

Routine Description:

    Static method that returns the attribute mask for all operations

Arguments:

    None
    
Return Value:

    Attribute map for enum operations

--*/
Hashtable^
JobThunkingProfile::
GetStaticAttributeMap(
    void
    )
{
    return attributeMap;        
}

/*++

Routine Name:   

    ReconcileMask

Routine Description:

    This method is called to remove redundancies. This is hard coded per type.
    The idea is that the mask has levels that cover the same attributes and 
    we want to pick the one that is the lowest cost. 
    For Job, we don't expect any redundancies.

Arguments:

    mask
    
Return Value:

    reconciled mask

--*/
UInt64
JobThunkingProfile::
ReconcileMask(
    UInt64  mask
    )
{
    return mask;
}
