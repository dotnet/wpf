// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:        
//

#pragma once



typedef struct _ENUMDEF {
    char *psz;          // description
    ULONG64 ul;         // enum value
} ENUMDEF;

// The following define expands 'ENUM(x)' to '"x", x':

#define ENUM(x) { #x, x }

#define END_ENUM { 0, 0 }


typedef struct _FLAGDEF {
    char *psz;          // description
    ULONG64 fl;         // flag
} FLAGDEF;

// The following define expands 'FLAG(x)' to '"x", x':

#define FLAG(x) { #x, x }

#define END_FLAG { 0, 0 }




extern char *pszHRESULT(HRESULT hr);
extern char *pszWinDbgError(ULONG ulError);


enum EnumFlagType {
    ENUM_FIELD,
    ENUM_FIELD_LIMITED,     // Enum list is not a complete list of valid values
    FLAG_FIELD,
    PARENT_FIELDS,
    CALL_FUNC
};


typedef struct _EnumFlagEntry EnumFlagEntry;

typedef struct _EnumFlagField {
    CHAR            FieldName[MAX_PATH];

    EnumFlagType    EFType;

    union {
        void           *Param;      // To alleviate casting in declarations
        FLAGDEF        *FlagDef;
        ENUMDEF        *EnumDef;
        EnumFlagEntry  *Parent;
        HRESULT       (*EFFunc)(__inout OutputControl*, __inout PDEBUG_CLIENT, __in const DEBUG_VALUE *);
    };

} EnumFlagField;


typedef struct _EnumFlagEntry {
    __nullterminated CHAR   TypeName[MAX_PATH];
    ULONG                   TypeId;
    ULONG                   FieldEntries;
    EnumFlagField          *FieldEntry;
} EnumFlagEntry;

#define EFTypeEntry(type)   { #type, 0, ARRAYSIZE(aeff##type), aeff##type}



ULONG64
OutputFlags(
    __inout OutputControl *OutCtl,
    __in __nullterminated const FLAGDEF *pFlagDef,
    ULONG64 fl,
    BOOL SingleLine
    );

BOOL
OutputEnum(
    __inout OutputControl *OutCtl,
    __in __nullterminated const ENUMDEF *pEnumDef,
    ULONG64 ul
    );

BOOL
OutputEnumWithParenthesis(
    __inout OutputControl *OutCtl,
    __in __nullterminated const ENUMDEF *pEnumDef,
    ULONG64 ul
    );

BOOL
OutputFieldValue(
    __inout OutputControl *OutCtl,
    __in const EnumFlagEntry *pEFEntry,
    __in PCSTR pszField,
    __in const DEBUG_VALUE *Value,
    __inout PDEBUG_CLIENT Client,
    BOOL Compact
    );

//
// This global must be defined by caller of OutputTypeFieldValue unless
// pEFDatabase is always passed in.
// The final value in the array must be     { "", 0, 0, NULL}
//
extern EnumFlagEntry EFDatabase[];

BOOL
OutputTypeFieldValue(
    __inout OutputControl *OutCtl,
    __in PCSTR pszType,
    __in PCSTR pszField,
    __in const DEBUG_VALUE *Value,
    __inout PDEBUG_CLIENT Client,
    BOOL Compact,
    __in __nullterminated const EnumFlagEntry *pEFDatabase = EFDatabase
    );



