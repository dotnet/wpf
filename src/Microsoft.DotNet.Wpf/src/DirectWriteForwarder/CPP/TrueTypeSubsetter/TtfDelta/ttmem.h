// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * TTmem.h: Interface file for TTmem.c 
  *
  *
  * 
  * This file must be included in order to use the memory manager access functions.
  */
  
#ifndef TTMEM_DOT_H_DEFINED
#define TTMEM_DOT_H_DEFINED        

#define MemNoErr 0
#define MemErr -1 

int16 Mem_Init(void);
/* Initialize memory manager internal structures */ 
/* return MemNoErr if successful */

void Mem_End(void);  
/* free all memory previously allocated and free memory structure */


void * Mem_Alloc(size_t); 
/* void *Mem_Alloc(size)
  * allocate a size bytes of memory   
  *
  * RETURN VALUE
  *  Pointer to a block of data  
  */

void Mem_Free(void *);
/* free up a block of data */

void * Mem_ReAlloc(void *, CONST size_t);
/* void *Mem_ReAlloc( pOldPtr, newSize)
 * reallocate and copy data
 *
 * INPUT 
 * pOldPtr - pointer to old block
 * newSize - size of new pointer to allocate 
 *
 * RETURN VALUE
 *  Pointer to a block of data 
 */
 void *Mem_ReAllocDelta(void * pOldPtr, CONST size_t Delta);
/* void *Mem_ReAllocDelta( pOldPtr, Delta)
 * reallocate and copy data
 *
 * INPUT 
 * pOldPtr - pointer to old block
 * Delta - Amount to increment block size 
 *
 * RETURN VALUE
 *  Pointer to a block of data 
 */

#endif /* CTTMEM_DOT_H_DEFINED */  
