// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//-----------------------------------------------------------------------------
//

//
//  The following are copied from various NT headers, primarily wdm.h.  I could
//  not figure out a way of including wdm.h and windows.h together.
//
//  These APIs are primarily intended for device drivers, but are technically
//  available to user mode code via exports in ntdll.dll.
//
//-----------------------------------------------------------------------------

#define DbgBreakPoint __debugbreak

extern "C" {
  ULONG __cdecl DbgPrint(PCSTR, ...);
  __declspec(dllimport) void __stdcall RtlAssert(PVOID, PVOID, ULONG, PSTR);
}

#if DBG
#define ASSERTMSG( msg, exp ) \
    ((!(exp)) ? \
        (RtlAssert( #exp, __FILE__, __LINE__, msg ),FALSE) : \
        TRUE)
#else
#define ASSERTMSG( msg, exp ) ((void) 0)
#endif // DBG

#define NTSTATUS LONG
#define STATUS_UNSUCCESSFUL              ((NTSTATUS)0xC0000001L)
#define STATUS_INSUFFICIENT_RESOURCES    ((NTSTATUS)0xC000009AL)
#define STATUS_COMMITMENT_LIMIT          ((NTSTATUS)0xC000012DL)

FORCEINLINE
VOID
InitializeListHead(
    __out PLIST_ENTRY ListHead
    )
{
    ListHead->Flink = ListHead->Blink = ListHead;
}

__checkReturn
BOOLEAN
FORCEINLINE
IsListEmpty(
    __in const LIST_ENTRY * ListHead
    )
{
    return (BOOLEAN)(ListHead->Flink == ListHead);
}

FORCEINLINE
BOOLEAN
RemoveEntryList(
    __in PLIST_ENTRY Entry
    )
{
    PLIST_ENTRY Blink;
    PLIST_ENTRY Flink;

    Flink = Entry->Flink;
    Blink = Entry->Blink;
    Blink->Flink = Flink;
    Flink->Blink = Blink;
    return (BOOLEAN)(Flink == Blink);
}

FORCEINLINE
PLIST_ENTRY
RemoveHeadList(
    __inout PLIST_ENTRY ListHead
    )
{
    PLIST_ENTRY Flink;
    PLIST_ENTRY Entry;

    Entry = ListHead->Flink;
    Flink = Entry->Flink;
    ListHead->Flink = Flink;
    Flink->Blink = ListHead;
    return Entry;
}

FORCEINLINE
VOID
InsertTailList(
    __inout PLIST_ENTRY ListHead,
    __inout __drv_aliasesMem PLIST_ENTRY Entry
    )
{
    PLIST_ENTRY Blink;

    Blink = ListHead->Blink;
    Entry->Flink = ListHead;
    Entry->Blink = Blink;
    Blink->Flink = Entry;
    ListHead->Blink = Entry;
}

