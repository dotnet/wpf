// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


#include "StdAfx.h"
#include "PbList.h"

/////////////////////////////////////////////////////////////////////////////

#ifdef DBG
void AssertSizeOfList(CPbList<INT> * pList, INT cExpected)
{
    ASSERT(0 <= cExpected);
    ASSERT(0 == cExpected &&  pList->IsEmpty() ||
           0 <  cExpected && !pList->IsEmpty());

    // a sample enumeration forward

    PBLKEY key = pList->GetHead();
    INT cActualF = 0;
    while (!pList->IsAtEnd(key))
    {
        cActualF++;
        key = pList->GetNext(key);
    }

    // a sample enumeration backwards

    key = pList->GetTail();
    INT cActualB = 0;
    while (!pList->IsAtEnd(key))
    {
        cActualB++;
        key = pList->GetPrev(key);
    }

    // assert 

    ASSERT (cActualF == cExpected);
    ASSERT (cActualB == cExpected);
}

void TestPbList()
{
    DHR;
    PBLKEY   key = PBLKEY_NULL;
    CPbList<INT> list;

    // still empty
    ASSERT (list.IsEmpty());

    // add item 1
    CHR_VERIFY(list.AddToTail(&key));
    list[key] = 1;
    AssertSizeOfList(&list, 1);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // add item 2
    CHR_VERIFY(list.AddToTail(&key));
    list[key] = 2;
    AssertSizeOfList(&list, 2);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // add item 3
    CHR_VERIFY(list.AddToTail(&key));
    list[key] = 3;
    AssertSizeOfList(&list, 3);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 3);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // add item 4, to head
    CHR_VERIFY(list.AddToHead(&key));
    list[key] = 4;
    AssertSizeOfList(&list, 4);
    key = list.GetHead();
    ASSERT (list[key] == 4);
    key = list.GetNext(key);
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 3);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // delete the head item
    key = list.GetHead();
    CHR_VERIFY(list.Remove(key));
    AssertSizeOfList(&list, 3);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 3);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // delete the tail item
    key = list.GetTail();
    CHR_VERIFY(list.Remove(key));
    AssertSizeOfList(&list, 2);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // delete the tail item again
    key = list.GetTail();
    CHR_VERIFY(list.Remove(key));
    AssertSizeOfList(&list, 1);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // delete the last remaining item
    key = list.GetHead();
    CHR_VERIFY(list.Remove(key));
    AssertSizeOfList(&list, 0);
    ASSERT (list.IsEmpty());

    // populate a bigger list (reversed in order)
    AssertSizeOfList(&list, 0);
    CHR_VERIFY(list.AddToHead(&key));
    list[key] = 1;
    AssertSizeOfList(&list, 1);
    CHR_VERIFY(list.AddToHead(&key));
    list[key] = 2;
    AssertSizeOfList(&list, 2);
    CHR_VERIFY(list.AddToHead(&key));
    list[key] = 3;
    AssertSizeOfList(&list, 3);
    CHR_VERIFY(list.AddToHead(&key));
    list[key] = 4;
    AssertSizeOfList(&list, 4);
    CHR_VERIFY(list.AddToHead(&key));
    list[key] = 5;
    AssertSizeOfList(&list, 5);

    // delete from the middle
    key = list.GetHead();
    key = list.GetNext(key);
    key = list.GetNext(key);
    CHR_VERIFY(list.Remove(key));
    AssertSizeOfList(&list, 4);
    key = list.GetHead();
    ASSERT (list[key] == 5);
    key = list.GetNext(key);
    ASSERT (list[key] == 4);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // move last item to front
    key = list.GetTail();
    CHR_VERIFY(list.MoveToHead(key));
    AssertSizeOfList(&list, 4);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 5);
    key = list.GetNext(key);
    ASSERT (list[key] == 4);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // move second item to back
    key = list.GetHead();
    key = list.GetNext(key);
    CHR_VERIFY(list.MoveToTail(key));
    AssertSizeOfList(&list, 4);
    key = list.GetHead();
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 4);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 5);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // insert an item to head
    CHR_VERIFY(list.InsertBefore(list.GetHead(), &key));
    list[key] = 3;
    AssertSizeOfList(&list, 5);
    key = list.GetHead();
    ASSERT (list[key] == 3);
    key = list.GetNext(key);
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 4);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 5);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

    // insert an item in the middle
    key = list.GetHead();
    key = list.GetNext(key);
    key = list.GetNext(key);
    CHR_VERIFY(list.InsertBefore(key, &key));
    list[key] = 6;
    AssertSizeOfList(&list, 6);
    key = list.GetHead();
    ASSERT (list[key] == 3);
    key = list.GetNext(key);
    ASSERT (list[key] == 1);
    key = list.GetNext(key);
    ASSERT (list[key] == 6);
    key = list.GetNext(key);
    ASSERT (list[key] == 4);
    key = list.GetNext(key);
    ASSERT (list[key] == 2);
    key = list.GetNext(key);
    ASSERT (list[key] == 5);
    key = list.GetNext(key);
    ASSERT (list.IsAtEnd(key));

CLEANUP:
    return;
}
#endif


