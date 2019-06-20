// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//+-----------------------------------------------------------------------------
//  
//
//  
//  File: ttmem.cpp
//
//  Description:    
//      Routines to allocate and free memory.
//
//
//------------------------------------------------------------------------------

#include "typedefs.h"

#include "ttmem.h"

#include "fsassert.h"

using namespace System::Security;
using namespace System::Security::Permissions;

// <SecurityNote>
//  Critical - allocates native mem and returns a pointer to it.
// </SecurityNote>
void * Mem_Alloc(size_t size)
{
    return calloc(1, size);
}


// <SecurityNote>
//  Critical - Frees an arbitrary native pointer.
// </SecurityNote>
void Real_Mem_Free(void * pv)
{
    free (pv);
}


// Mem_Free/Mem_Alloc are expensive in partial trust. More than half of the calls to Mem_Free are 
// with NULL pointers. So we check for NULL pointer before going into expensive assert and interop.
// There are more optimizations possible (for example grouping Mem_Alloc calls). But this is safe.
// <SecurityNote>
//  Critical - Frees an arbitrary native pointer.
// </SecurityNote>
void Mem_Free(void * pv)
{
    if (pv != NULL)
    {
        Real_Mem_Free(pv);
    }        
}


// <SecurityNote>
//  Critical - allocates native mem and returns a pointer to it.
// </SecurityNote>
void * Mem_ReAlloc(void * base, size_t newSize)
{
    return realloc(base, newSize);
}

int16 Mem_Init(void)
{
    return MemNoErr;
}

void Mem_End(void)
{
}

