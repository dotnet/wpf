// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


/*++



Module Name:

    debug.cxx

Abstract:

    This file contains debug routines to debug extenstion problems.


--*/


#include <precomp.hxx>


#if DBG

const char NoIndent[] = "";

void
vPrintNativeFieldInfo(
    PFIELD_INFO pFI,
    const char *pszIndent)
{
    if (!pFI) return;

    DbgPrint("  %sPUCHAR  fName     = \"%s\"\n", pszIndent, pFI->fName);
    DbgPrint("  %sPUCHAR  printName = \"%s\"\n", pszIndent, pFI->printName);
    DbgPrint("  %sULONG   size      = 0x%x\n", pszIndent, pFI->size);
    DbgPrint("  %sULONG   fOptions  = 0x%08x\n", pszIndent, pFI->fOptions);
    DbgPrint("  %sULONG64 address   = 0x%I64x\n", pszIndent, pFI->address);
    DbgPrint("  %sPVOID   fieldCallBack = 0x%p\n", pszIndent, pFI->fieldCallBack);
}


void
vPrintNativeSymDumpParam(
    PSYM_DUMP_PARAM pSDP,
    BOOL bDumpFields,
    const char *pszIndent)
{
    if (!pSDP) return;

    char    pszNextIndent[80];

    StringCchPrintfA(pszNextIndent, ARRAY_SIZE(pszNextIndent), "%s  ", pszIndent);

    DbgPrint("  %sULONG               size     = 0x%x\n", pszIndent, pSDP->size);
    DbgPrint("  %sPUCHAR              sName    = \"%s\"\n", pszIndent, pSDP->sName);
    DbgPrint("  %sULONG               Options  = 0x%08x\n", pszIndent, pSDP->Options);
    DbgPrint("  %sULONG64             addr     = 0x%I64x\n", pszIndent, pSDP->addr);
    DbgPrint("  %sPFIELD_INFO         listLink = 0x%p\n", pszIndent, pSDP->listLink);
    DbgPrint("  %sPVOID               Context  = 0x%p\n", pszIndent, pSDP->Context);
    DbgPrint("  %sPSYM_DUMP_FIELD_CALLBACK CallbackRoutine = 0x%p\n", pszIndent, pSDP->CallbackRoutine);
    DbgPrint("  %sULONG               nFields  = %d\n", pszIndent, pSDP->nFields);
    DbgPrint("  %sPFIELD_INFO         Fields   = 0x%p\n", pszIndent, pSDP->Fields);

    if (bDumpFields && pSDP->Fields)
    {
        for (ULONG nField = 0; nField < pSDP->nFields; nField++)
        {
            DbgPrint("  %sFIELD_INFO          Fields[%d] = {\n", pszIndent, nField);
            vPrintNativeFieldInfo(&pSDP->Fields[nField], pszNextIndent);
            DbgPrint("  %s}\n", pszIndent);
        }
    }
}

#endif  DBG



