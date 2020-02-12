// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//+----------------------------------------------------------------------------
//

//
//  Abstract:        
//

#include "precomp.hxx"


#define CASEENUM(x) case x: psz = #x; break

static char achFlags[100];

char *pszHRESULT(HRESULT hr)
{
    char *psz;

    switch (hr)
    {
    case 0: psz = "OK"; break;
    CASEENUM(S_FALSE);
    CASEENUM(E_NOTIMPL);
    CASEENUM(E_OUTOFMEMORY);
    CASEENUM(E_INVALIDARG);
    CASEENUM(E_NOINTERFACE);
    CASEENUM(E_ABORT);
    CASEENUM(E_FAIL);
    default:
        switch (hr & 0xCFFFFFFF)
        {
        CASEENUM(STATUS_UNSUCCESSFUL);
        default:
            StringCchPrintfA(achFlags, ARRAYSIZE(achFlags), "unknown HRESULT 0x%08lx", hr);
            psz = achFlags;
            break;
        }
        break;
    }
    return(psz);
}


char *pszWinDbgError(ULONG ulError)
{
    char *psz;

    switch (ulError)
    {
    case 0: psz = "no error"; break;
    CASEENUM(MEMORY_READ_ERROR);
    CASEENUM(SYMBOL_TYPE_INDEX_NOT_FOUND);
    CASEENUM(SYMBOL_TYPE_INFO_NOT_FOUND);
    CASEENUM(FIELDS_DID_NOT_MATCH);
    CASEENUM(NULL_SYM_DUMP_PARAM);
    CASEENUM(NULL_FIELD_NAME);
    CASEENUM(INCORRECT_VERSION_INFO);
    CASEENUM(EXIT_ON_CONTROLC);
    CASEENUM(CANNOT_ALLOCATE_MEMORY);
    default:
        StringCchPrintfA(achFlags, ARRAYSIZE(achFlags), "unknown WinDbg error 0x08%x", ulError);
        psz = achFlags;
        break;
    }
    return(psz);
}



/******************************Public*Routine******************************\
*   output standard flags
*
*
\**************************************************************************/

ULONG64
OutputFlags(
    __inout OutputControl *OutCtl,
    __in __nullterminated const FLAGDEF *pFlagDef,
    ULONG64 fl,
    BOOL SingleLine
    )
{
    ULONG64 FlagsFound = 0;

    if (fl == 0)
    {
        while (pFlagDef->psz != NULL)
        {
            if (pFlagDef->fl == 0)
            {
                if (!SingleLine) OutCtl->Output("\n       ");
                OutCtl->Output("%s",pFlagDef->psz);
            }

            pFlagDef++;
        }
    }
    else
    {
        while (pFlagDef->psz != NULL)
        {
            if (pFlagDef->fl & fl)
            {
                if (!SingleLine)
                {
                    OutCtl->Output("\n       ");
                }
                else if (FlagsFound)
                {
                    OutCtl->Output(" | ");
                }

                OutCtl->Output("%s",pFlagDef->psz);

                if (FlagsFound & pFlagDef->fl)
                {
                    OutCtl->Output(" (SHARED FLAG)");
                }
                FlagsFound |= pFlagDef->fl;
            }

            pFlagDef++;
        }
    }

    return fl & ~FlagsFound;
}


/******************************Public*Routine******************************\
*   output standard enum values
*
*
\**************************************************************************/

BOOL
OutputEnum(
    __inout OutputControl *OutCtl,
    __in __nullterminated const ENUMDEF *pEnumDef,
    ULONG64 ul
    )
{
    while (pEnumDef->psz != NULL)
    {
        if (pEnumDef->ul == ul)
        {
            OutCtl->Output(pEnumDef->psz);
            return (TRUE);
        }

        pEnumDef++;
    }

    return (FALSE);
}

BOOL
OutputEnumWithParenthesis(
    __inout OutputControl *OutCtl,
    __in __nullterminated const ENUMDEF *pEnumDef,
    ULONG64 ul
    )
{
    while (pEnumDef->psz != NULL)
    {
        if (pEnumDef->ul == ul)
        {
            OutCtl->Output("(%s)", pEnumDef->psz);
            return (TRUE);
        }

        pEnumDef++;
    }

    return (FALSE);
}


/******************************Public*Routine******************************\
*   Output interpretation of pszField's value if found in pEFEntry
*
*
\**************************************************************************/

BOOL
OutputFieldValue(
    __inout OutputControl *OutCtl,
    __in const EnumFlagEntry *pEFEntry,
    __in PCSTR pszField,
    __in const DEBUG_VALUE *Value,
    __inout PDEBUG_CLIENT Client,
    BOOL Compact
    )
{
    EnumFlagField  *pEFField;
    DEBUG_VALUE     ConvValue;

    if (OutCtl == NULL ||
        pEFEntry == NULL ||
        pszField == NULL ||
        Value == NULL ||
        Value->Type == DEBUG_VALUE_INVALID)
    {
        return FALSE;
    }

    for (ULONG i = 0; i < pEFEntry->FieldEntries; i++)
    {
        pEFField = &pEFEntry->FieldEntry[i];

        if (pEFField->EFType == PARENT_FIELDS)
        {
            if (OutputFieldValue(OutCtl, pEFField->Parent, pszField, Value, Client, Compact))
            {
                return TRUE;
            }
        }
        else if (strcmp(pszField, pEFField->FieldName) == 0)
        {
            switch (pEFField->EFType)
            {
                case FLAG_FIELD:
                {
                    ULONG64 flRem;

                    if (Value->Type != DEBUG_VALUE_INT64)
                    {
                        if (OutCtl->CoerceValue(Value, DEBUG_VALUE_INT64, &ConvValue) != S_OK)
                        {
                            return FALSE;
                        }
                        Value = &ConvValue;
                    }

                    if (Compact)
                    {
                        OutCtl->Output(" (");
                    }
                    flRem = OutputFlags(OutCtl, pEFField->FlagDef, Value->I64, Compact);
                    if (flRem && ((flRem != 0xffffffff00000000) || !(Value->I64 & 0x80000000)))
                    {
                        if (!Compact) OutCtl->Output("\n      ");
                        OutCtl->Output("  Unknown Flags: 0x%I64x", flRem);
                    }
                    if (Compact)
                    {
                        OutCtl->Output(")");
                    }
                    return TRUE;
                }

                case ENUM_FIELD:
                case ENUM_FIELD_LIMITED:
                {
                    if (Value->Type != DEBUG_VALUE_INT64)
                    {
                        if (OutCtl->CoerceValue(Value, DEBUG_VALUE_INT64, &ConvValue) != S_OK)
                        {
                            return FALSE;
                        }
                        Value = &ConvValue;
                    }

                    OutCtl->Output(" ");
                    if (!OutputEnumWithParenthesis(OutCtl, pEFField->EnumDef, Value->I64))
                    {
                        if (pEFField->EFType != ENUM_FIELD_LIMITED)
                        {
                            OutCtl->Output("(Unknown Value)", Value->I64);
                        }
                    }
                    return TRUE;
                }

                case CALL_FUNC:
                    OutCtl->Output(" ");
                    pEFField->EFFunc(OutCtl, Client, Value);
                    return TRUE;

                default:
                    OutCtl->OutErr("        Unknown database entry type.\n");
                    break;
            }
        }
    }

    return FALSE;
}


/******************************Public*Routine******************************\
*   Output interpretations of known fields as stored in EFDatabase
*       (Known flags & enum values as well some special fields.)
*
*
\**************************************************************************/

BOOL
OutputTypeFieldValue(
    __inout OutputControl *OutCtl,
    __in PCSTR pszType,
    __in PCSTR pszField,
    __in const DEBUG_VALUE *Value,
    __inout PDEBUG_CLIENT Client,
    BOOL Compact,
    __in __nullterminated const EnumFlagEntry *pEFDatabase
    )
{
    if (OutCtl == NULL ||
        Value == NULL ||
        Value->Type == DEBUG_VALUE_INVALID)
    {
        return FALSE;
    }

    BOOL                  FoundType = FALSE;
    const EnumFlagEntry  *pEFEntry = pEFDatabase;

    for (pEFEntry = pEFDatabase;
         pEFEntry->TypeName[0] != '\0';
         pEFEntry++)
    {
        if (strcmp(pszType, pEFEntry->TypeName) == 0)
        {
            FoundType = TRUE;
            break;
        }
    }

    if (!FoundType)
    {
        // Check if this type is a clean typedef
        // (Test it against database with prefixed
        // '_'s and 'tag's removed.)
        for (pEFEntry = pEFDatabase;
             pEFEntry->TypeName[0] != '\0';
             pEFEntry++)
        {
            if ((pEFEntry->TypeName[0] == '_') ?
                (strcmp(pszType, &pEFEntry->TypeName[1]) == 0) :
                (pEFEntry->TypeName[0] == 't' &&
                 pEFEntry->TypeName[1] == 'a' &&
                 pEFEntry->TypeName[2] == 'g' &&
                 strcmp(pszType, &pEFEntry->TypeName[3]) == 0))
            {
                FoundType = TRUE;
                break;
            }
        }

    }

    return (FoundType) ?
        OutputFieldValue(OutCtl, pEFEntry, pszField, Value, Client, Compact) :
        FALSE;
}




