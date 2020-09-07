// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
        
/*++                                                                          
    Abstract:

        InfoLevelCoverageList - this is the container object that holds the thunk
        objects. The list is used to group the thunk objects and then enumerated
        to call the thunking on each object. The thunk objects are expected to be 
        of the same type. For instance, a InfoLevelCoverageList generated for 
        PrinterThunkingProfile will only hold objects of type Win32PrinterThunk.
        
--*/

#include "win32inc.hpp"

#ifndef  __INTEROPNAMESPACEUSAGE_HPP__
#include <InteropNamespaceUsage.hpp>
#endif

#ifndef  __PRINTSYSTEMINTEROPINC_HPP__
#include <PrintSystemInteropInc.hpp>
#endif 



using namespace MS::Internal::PrintWin32Thunk;
using namespace MS::Internal::PrintWin32Thunk::Win32ApiThunk;
using namespace MS::Internal::PrintWin32Thunk::AttributeNameToInfoLevelMapping;

/*++

Routine Name:   

    InfoLevelCoverageList

Routine Description:

    Constructor 

Arguments:

    none
    
Return Value:

    N\A

--*/
InfoLevelCoverageList::
InfoLevelCoverageList(
    void
    ) : objectCount(1)
{
    coverageList = gcnew ArrayList;
}

/*++

Routine Name:   

    Add

Routine Description:

    Adds thunk object to the list

Arguments:

    profile - thunk object
    
Return Value:

    None

--*/
void
InfoLevelCoverageList::
Add(
    InfoLevelThunk^   profile
    )
{
    coverageList->Add(profile);
}

/*++

Routine Name:   

    GetEnumerator

Routine Description:

    Returns the covelageList enumerator.

Arguments:

    None
    
Return Value:

    IEnumerator

--*/
IEnumerator^
InfoLevelCoverageList::
GetEnumerator(
    void
    )
{
    return coverageList->GetEnumerator();
}

/*++

Routine Name:   

    GetInfoLevelThunk

Routine Description:

    Looks up the thunk object that matches the given mask

Arguments:

    mask - InfoLevelMask value
    
Return Value:

    Returns the thunk object that matches the mask

--*/
InfoLevelThunk^
InfoLevelCoverageList::
GetInfoLevelThunk(
    UInt64  mask
    )
{
    IEnumerator^      coverageListEnumerator = coverageList->GetEnumerator();
    InfoLevelThunk^   infoLevelThunk         = nullptr;
            
    for ( ;coverageListEnumerator->MoveNext(); )
    {
        InfoLevelThunk^ currentInfoLevelThunk  = (InfoLevelThunk^)coverageListEnumerator->Current;
        
        if ((UInt64)currentInfoLevelThunk->LevelMask & mask)
        {
            infoLevelThunk = currentInfoLevelThunk;
            break;
        }        
    }

    return infoLevelThunk;
}

/*++

Routine Name:   

    InternalDispose

Routine Description:

    Internal Dispose method. It calls Dispose on the objects hels in the list.

Arguments:

    disposing - true when called from destructor
    
Return Value:

    N\A

--*/
void
InfoLevelCoverageList::
Release(
    void
    )
{
    if(!isDisposed)
    {
        IEnumerator^ coverageListEnumerator = coverageList->GetEnumerator();
        
        for ( ;coverageListEnumerator->MoveNext(); )
        {
            InfoLevelThunk^   infoLevelThunk = (InfoLevelThunk^)coverageListEnumerator->Current;
            infoLevelThunk->Release();
        }
        isDisposed = true;
    }
}


/*++

Routine Name:   

    set_Count

Routine Description:

    property

Arguments:

    count - Number of print objects that the thunk objects hold.
            To not be confunded with the number of thunk objects in the 
            coverage list.
    
Return Value:

    N\A

--*/
void
InfoLevelCoverageList::Count::
set(
    UInt32 count
    )
{
    objectCount = count;
}


/*++

Routine Name:   

    get_Count

Routine Description:

    property

Arguments:

    N/A
    
Return Value:

    Number of print objects that the thunk objects hold.

--*/
UInt32
InfoLevelCoverageList::Count::
get(
    void
    )
{
    return objectCount;
}

