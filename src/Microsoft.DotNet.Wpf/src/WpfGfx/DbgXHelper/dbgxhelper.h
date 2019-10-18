// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:        
//


// This is a 64 bit aware debugger extension library
#define KDEXT_64BIT


#pragma warning (push)
// 4245: 'argument' : conversion from 'NTSTATUS' to 'DWORD', signed/unsigned mismatch
#pragma warning (disable : 4245)
#include <wdbgexts.h>
#pragma warning (pop)
// When using the structures in wdbgexts.h UCHARs are
// used.  For C++ we need to get the type right.
#define DbgStr(s)   (PUCHAR)s

#include <dbgeng.h>


//+----------------------------------------------------------------------------
//
//  Structure:  ModuleParameters
//
//  Synopsis:   
//
//-----------------------------------------------------------------------------

typedef struct {
    ULONG64                 Base;
    ULONG                   Index;
    __nullterminated CHAR   Name[MAX_PATH];
    __nullterminated CHAR   Ext[4];
    DEBUG_MODULE_PARAMETERS DbgModParams;
} ModuleParameters;


//
// Global data consumed by DbgXHelper.lib and must be defined by ext DLL.
//

extern ModuleParameters UM_Module;


//
// Prototypes for initialize event callbacks that must be defined by ext DLL.
//

HRESULT OnExtensionInitialize(
    __inout PDEBUG_CLIENT DebugClient
    );

void OnExtensionUninitialize();

HRESULT OnSymbolInitialize(
    __in HRESULT hrCurrent,
    __inout PDEBUG_CLIENT Client
    );


//
// Global data provided by DbgXHelper.lib
//

extern HINSTANCE    ghDllInst;

extern ULONG        TargetMachine;
extern ULONG        TargetClass;
extern ULONG        PlatformId;
extern ULONG        MajorVer;
extern ULONG        MinorVer;
extern ULONG        SrvPack;
extern ULONG        BuildNo;

extern ModuleParameters Type_Module;


//
// Macros to define extension methods
//

//
// undef the wdbgexts
//
#undef DECLARE_API

#define DECLARE_API(extension)                                  \
CPPMOD HRESULT CALLBACK extension(__inout PDEBUG_CLIENT Client, __in PCSTR args)

#define BEGIN_API(extension) InitAPI(Client, #extension);


//
// General helper routines provided by DbgXHelper.lib
//


HRESULT
InitAPI(
    __inout PDEBUG_CLIENT Client,
    __in PCSTR ExtName
    );


HRESULT
GetDebugClient(
    __deref_out PDEBUG_CLIENT *pClient
    );

HRESULT
SymbolInit(
    __inout PDEBUG_CLIENT Client
    );

HRESULT
GetModuleParameters(
    __inout PDEBUG_CLIENT Client,
    __out ModuleParameters *Module,
    BOOL TryReload
    );

HRESULT
GetTypeId(
    __inout PDEBUG_CLIENT Client,
    __in PCSTR Type,
    __out PULONG TypeId,
    __out_opt PULONG64 Module
    );


#define EVALUATE_DEFAULT_TYPE   DEBUG_VALUE_INVALID
#define EVALUATE_DEFAULT_RADIX  0

#define EVALUATE_COMPACT_EXPR   1
#define EVALUATE_DEFAULT_FLAGS  0

HRESULT
Evaluate(
    __inout PDEBUG_CLIENT Client,
    __in PCSTR Expression,
    ULONG DesiredType,
    ULONG Radix,
    __out PDEBUG_VALUE Value,
    __out_opt PULONG RemainderIndex = NULL,
    __out_opt PULONG StartIndex = NULL,
    FLONG Flags = EVALUATE_DEFAULT_FLAGS
    );


//
// Other DbgXHelper.lib provided routines and classes
//

#include "event.hxx"
#include "output.hxx"
#include "flags.hxx"
#include "input.hxx"



