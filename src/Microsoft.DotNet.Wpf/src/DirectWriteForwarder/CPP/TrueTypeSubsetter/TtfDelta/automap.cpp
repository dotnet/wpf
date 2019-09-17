// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: AutoMap.C   
 * 
 * 
 * Module to Add Glyph Indices into list of Indices to keep, based on 
 * information found in the Font File's GSUB, JSTF and BASE tables, if any. 
 **************************************************************************/

/* Inclusions ----------------------------------------------------------- */

#include "typedefs.h"
#include "ttff.h"
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "ttftable.h"
#include "ttftabl1.h"
#include "util.h"
#include "ttferror.h" /* for error codes */
#include "ttmem.h"
#include "automap.h"


int16 MortAutoMap(TTFACC_FILEBUFFERINFO * pInputBufferInfo,   /* ttfacc info */
                 uint8 * pabKeepGlyphs, /* binary list of glyphs to keep - to be updated here */
                 uint16 usnGlyphs,    /* number of glyphs in list */
                 uint16 fKeepFlag)
{
MORTBINSRCHHEADER MortBinSrchHeader;
MORTLOOKUPSINGLE  MortLookup;
uint16 nEntries;
uint16 usBytesRead;
uint32 ulOffset;
uint32 ulLength;
uint32 ulLastOffset;
int16 errCode = NO_ERROR;

    ulOffset = TTTableOffset( pInputBufferInfo, MORT_TAG );
     ulLength = TTTableLength( pInputBufferInfo, MORT_TAG );
    ulLastOffset = ulOffset+ulLength;

    if (ulOffset == DIRECTORY_ERROR || ulLength == 0)    /* nothing to map, we're done */
        return NO_ERROR;

    ulOffset += GetGenericSize(MORTHEADER_CONTROL);     /* skip over mortheader */

    if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *)&MortBinSrchHeader, SIZEOF_MORTBINSRCHHEADER, MORTBINSRCHHEADER_CONTROL, ulOffset, &usBytesRead ) )!= NO_ERROR)
        return errCode;

    ulOffset += usBytesRead;

    for ( nEntries = MortBinSrchHeader.nEntries; nEntries > 0 && ulOffset < ulLastOffset;nEntries--)
    {
        if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *)&MortLookup, SIZEOF_MORTLOOKUPSINGLE, MORTLOOKUPSINGLE_CONTROL, ulOffset, &usBytesRead ) )!= NO_ERROR)
            return errCode;
        ulOffset += usBytesRead;
        
        if ( MortLookup.glyphid1 < usnGlyphs && pabKeepGlyphs[MortLookup.glyphid1] == fKeepFlag && MortLookup.glyphid2 < usnGlyphs && pabKeepGlyphs[MortLookup.glyphid2] == 0)
            pabKeepGlyphs[MortLookup.glyphid2] = (uint8)(fKeepFlag + 1); /* set this value too */
    }
    return NO_ERROR;
}

/* ------------------------------------------------------------------- */
/* Static function to syncronize the Keep Glyph List with the Coverage list */
/* (add in values if necessary) */ 
/* ------------------------------------------------------------------- */
static int16 UpdateKeepWithCoverage(TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint8 * pabKeepGlyphs, uint16 usnGlyphs, uint16 fKeepFlag, uint32 ulBaseOffset, uint32 ulCoverageOffset, uint16 *pArray, uint16 usLookupType, uint16 usSubstFormat)
{
uint32 ulOffset;
uint16 usCoverageFormat;
GSUBCOVERAGEFORMAT1    Coverage1;
GSUBCOVERAGEFORMAT2    Coverage2;
uint16 usCount;    /* number of array elements in coverage table */
uint16 usGlyphCount; /* number of glyphs processed in Coverage Table */
uint16 usGlyphID;
uint16 *pGlyphIDArray = NULL;
GSUBRANGERECORD *pRangeRecordArray = NULL;
uint16 i, j, k, l;
uint16 usBytesRead;
uint32 ulBytesRead;
int16 errCode = NO_ERROR;

    if ((ulCoverageOffset == 0) || (pArray == NULL))
        return NO_ERROR;
    ulOffset = ulBaseOffset + ulCoverageOffset;
    if ((errCode = ReadWord( pInputBufferInfo, &usCoverageFormat, ulOffset)) != NO_ERROR)
        return errCode;
    /* OK, read in the actual Coverage Table */
    switch (usCoverageFormat)
    {
    case 1:
        if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&Coverage1, SIZEOF_GSUBCOVERAGEFORMAT1, GSUBCOVERAGEFORMAT1_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
            return errCode;
        ulOffset += usBytesRead;
        usCount = Coverage1.GlyphCount;
        pGlyphIDArray = (uint16 *)Mem_Alloc(usCount * sizeof(uint16));
        if (pGlyphIDArray == NULL)
            return ERR_MEM;
        if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *)pGlyphIDArray, WORD_CONTROL, ulOffset, &ulBytesRead, usCount, sizeof(uint16)) )!= NO_ERROR)
        {
            Mem_Free(pGlyphIDArray);
            return errCode;
        }
          break;
    case 2:
        if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&Coverage2, SIZEOF_GSUBCOVERAGEFORMAT2, GSUBCOVERAGEFORMAT2_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
            return errCode;
        ulOffset += usBytesRead;
        usCount = Coverage2.CoverageRangeCount;
        pRangeRecordArray = (GSUBRANGERECORD *)Mem_Alloc(usCount * SIZEOF_GSUBRANGERECORD);
        if (pRangeRecordArray == NULL)
            return ERR_MEM;
        if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *) pRangeRecordArray,  GSUBRANGERECORD_CONTROL, ulOffset, &ulBytesRead, usCount, SIZEOF_GSUBRANGERECORD) )!= NO_ERROR)
        {
            Mem_Free(pRangeRecordArray);
            return errCode;
        }
        break;
    default:
        return ERR_INVALID_TTO;
    }
    
    for (i = 0, j = 0, usGlyphCount = 0; i < usCount && errCode == NO_ERROR; ++usGlyphCount) /* while the entire Coverage Table has not been processed */
    {
        /* First, get a glyph Code from coverage table */
        if (usCoverageFormat == 1)
        {  
            usGlyphID = pGlyphIDArray[i];
            ++i; 
        }
        else 
        {     
              usGlyphID = (uint16) (pRangeRecordArray[i].RangeStart + j);
            if (usGlyphID < pRangeRecordArray[i].RangeEnd)
                ++j;
            else  /* go to next range */
            {
                ++i;
                j = 0;
            }
        }
        
        /* Next, see if it exists, and deal with corresponding Substitute data */
        if (usGlyphID >= usnGlyphs || pabKeepGlyphs[usGlyphID] != fKeepFlag)
            continue;

        /* now read in the actual Subtitute Glyph Data and process */
        switch (usLookupType)
        {
        case GSUBSingleLookupType: 
            if (usSubstFormat == 1)
            {
                if ((usGlyphID + (int16) *pArray) < usnGlyphs && pabKeepGlyphs[usGlyphID + (int16) *pArray] == 0)
                    pabKeepGlyphs[usGlyphID + (int16) *pArray] = (uint8)(fKeepFlag + 1);
            }
            else
            {
                if (pArray[usGlyphCount] < usnGlyphs && pabKeepGlyphs[pArray[usGlyphCount]] == 0)
                    pabKeepGlyphs[pArray[usGlyphCount]] = (uint8)(fKeepFlag + 1);
            }
            break;
        case GSUBMultipleLookupType:
        {
            uint16 usSequenceGlyphCount;
            uint16 *pausGlyphID = NULL;

            if (pArray[usGlyphCount] == 0)
                break;
            ulOffset = ulBaseOffset + pArray[usGlyphCount];
            if ((errCode = ReadWord( pInputBufferInfo, &usSequenceGlyphCount, ulOffset ) )!= NO_ERROR)
                break;
            ulOffset += sizeof(uint16);
            pausGlyphID = (uint16 *)Mem_Alloc(usSequenceGlyphCount * sizeof(uint16));
            if (pausGlyphID == NULL)
            {
                errCode = ERR_MEM;
                break;
            }
            errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *)pausGlyphID, WORD_CONTROL, ulOffset, &ulBytesRead, usSequenceGlyphCount, sizeof(uint16));
            if (errCode == NO_ERROR)
            {            
                for (k = 0; k < usSequenceGlyphCount; ++k)
                {
                     if (pausGlyphID[k] < usnGlyphs && pabKeepGlyphs[pausGlyphID[k]] == 0)
                       pabKeepGlyphs[pausGlyphID[k]] = (uint8)(fKeepFlag + 1);
                }
            }
            Mem_Free (pausGlyphID);
            break;
        }
        case GSUBAlternateLookupType:    
        {
            uint16 usAlternateGlyphCount;
            uint16 *pausGlyphID = NULL;

            if (pArray[usGlyphCount] == 0)
                break;
            ulOffset = ulBaseOffset + pArray[usGlyphCount];
            if ((errCode = ReadWord( pInputBufferInfo, &usAlternateGlyphCount, ulOffset ) )!= NO_ERROR)
                break;
            ulOffset += sizeof(uint16);
            pausGlyphID = (uint16 *)Mem_Alloc(usAlternateGlyphCount * sizeof(uint16));
            if (pausGlyphID == NULL)
            {
                errCode = ERR_MEM;
                break;
            }
            if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *)pausGlyphID, WORD_CONTROL, ulOffset, &ulBytesRead, usAlternateGlyphCount, sizeof(uint16)) )== NO_ERROR)
            {
                for (k = 0; k < usAlternateGlyphCount; ++k)
                {
                     if (pausGlyphID[k] < usnGlyphs && pabKeepGlyphs[pausGlyphID[k]] == 0)
                       pabKeepGlyphs[pausGlyphID[k]] = (uint8)(fKeepFlag + 1);
                }
            }
            Mem_Free (pausGlyphID);
            break;
        }
        case GSUBLigatureLookupType:
        {
            uint16 usLigatureCompCount;
            uint16 usLigatureCount;
            uint16 *pausCompGlyphID = NULL; /* glyph IDs components of ligature */
            uint16 usLigatureGlyphID;   /* actual glyph to substitute for ligatures */
            uint16 *pausLigatureOffsetArray = NULL;
            GSUBLIGATURE GSUBLigature;

            if (pArray[usGlyphCount] == 0)
                break;
            ulOffset = ulBaseOffset + pArray[usGlyphCount];
            if ((errCode = ReadWord( pInputBufferInfo, &usLigatureCount, ulOffset ) )!= NO_ERROR)
                break;
            ulOffset += sizeof(uint16);
            pausLigatureOffsetArray = (uint16 *)Mem_Alloc(usLigatureCount * sizeof(uint16));
            if (pausLigatureOffsetArray == NULL)
            {
                errCode = ERR_MEM;
                break;
            }
            if ((errCode = ReadGenericRepeat( pInputBufferInfo,  (uint8 *)pausLigatureOffsetArray, WORD_CONTROL, ulOffset, &ulBytesRead, usLigatureCount, sizeof(uint16)) ) == NO_ERROR)
            {        
                for (l = 0; l < usLigatureCount; ++l)
                {
                    if (pausLigatureOffsetArray[l] == 0)
                        continue;
                    ulOffset = ulBaseOffset + (pArray)[usGlyphCount] + pausLigatureOffsetArray[l];
                    if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBLigature, SIZEOF_GSUBLIGATURE, GSUBLIGATURE_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                    {
                        Mem_Free (pausLigatureOffsetArray);
                        Mem_Free(pGlyphIDArray);
                        Mem_Free(pRangeRecordArray);
                        return errCode;
                    }
                    ulOffset += usBytesRead;
                    usLigatureCompCount = GSUBLigature.LigatureCompCount; 
                    usLigatureGlyphID = GSUBLigature.GlyphID; 
                    if (usLigatureGlyphID >= usnGlyphs || pabKeepGlyphs[usLigatureGlyphID] != 0)
                        continue;  /* already in list, go to next ligature */
                    pausCompGlyphID = (uint16 *)Mem_Alloc((usLigatureCompCount - 1) * sizeof(uint16));
                    if (pausCompGlyphID == NULL)
                    {
                        Mem_Free (pausLigatureOffsetArray);
                        Mem_Free(pGlyphIDArray);
                        Mem_Free(pRangeRecordArray);
                        return ERR_MEM;
                    }
                    if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *)pausCompGlyphID, WORD_CONTROL, ulOffset, &ulBytesRead, (uint16) (usLigatureCompCount - 1) , sizeof(uint16)) )!= NO_ERROR)
                    {
                        Mem_Free (pausCompGlyphID);
                        Mem_Free (pausLigatureOffsetArray);
                        Mem_Free(pGlyphIDArray);
                        Mem_Free(pRangeRecordArray);
                        return errCode;
                    }
                    for (k = 0; k < usLigatureCompCount - 1; ++k)
                    {
                         if (pausCompGlyphID[k] >= usnGlyphs || pabKeepGlyphs[pausCompGlyphID[k]] == 0)
                            break; /* if one of the components is not in list, don't worry about ligature */
                    }
                    if (k == (usLigatureCompCount - 1) && pabKeepGlyphs[usLigatureGlyphID] == 0) /* got to the end of the component list */
                        pabKeepGlyphs[usLigatureGlyphID] = (uint8)(fKeepFlag+1);
                    Mem_Free(pausCompGlyphID);
                }
            }
            Mem_Free (pausLigatureOffsetArray);
            break;
            }
        }

    }
    Mem_Free(pGlyphIDArray);
    Mem_Free(pRangeRecordArray);
    return errCode;
} 
/* ------------------------------------------------------------------- */
/* static function to read glyphid out of BaseCoordFormat2 table and   */
/* add it to the KeepGlyph list */
/* ------------------------------------------------------------------- */
static int16 ProcessBaseCoord(TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint32 ulOffset, uint8 * pabKeepGlyphs, uint16 usnGlyphs, uint16 fKeepFlag)
{
BASECOORDFORMAT2 BASECoordFormat2;
uint16 BASECoordFormat;
int16 errCode;
uint16 usBytesRead;

    if ((errCode = ReadWord( pInputBufferInfo,  &BASECoordFormat, ulOffset ) )!= NO_ERROR)
        return errCode;
    if (BASECoordFormat != 2)
        return NO_ERROR;
     if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *) &BASECoordFormat2, SIZEOF_BASECOORDFORMAT2, BASECOORDFORMAT2_CONTROL, ulOffset, &usBytesRead ) )!= NO_ERROR)
        return errCode;
    if (BASECoordFormat2.GlyphID < usnGlyphs && pabKeepGlyphs[BASECoordFormat2.GlyphID] == 0)
        pabKeepGlyphs[BASECoordFormat2.GlyphID] = (uint8)(fKeepFlag + 1);
    return NO_ERROR;

}
/* ------------------------------------------------------------------- */
/* static function to read the glyphids from a MinMax record and add it*/
/* to the KeepGlyph list */
/* ------------------------------------------------------------------- */
static int16 ProcessMinMax(TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint32 ulOffset, uint8 * pabKeepGlyphs, uint16 usnGlyphs, uint16 fKeepFlag)
{
BASEMINMAX BASEMinMax;
BASEFEATMINMAXRECORD BASEFeatMinMaxRecord;
uint16 i;
uint16 usBytesRead;
uint32 ulCurrentOffset;
int16 errCode;

    if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEMinMax, SIZEOF_BASEMINMAX, BASEMINMAX_CONTROL, ulOffset, &usBytesRead ) )!= NO_ERROR)
        return errCode;
    if (BASEMinMax.MinCoordOffset != 0)
        if ((errCode = ProcessBaseCoord( pInputBufferInfo, ulOffset + BASEMinMax.MinCoordOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
            return errCode;
    if (BASEMinMax.MaxCoordOffset != 0)
        if ((errCode = ProcessBaseCoord( pInputBufferInfo, ulOffset + BASEMinMax.MaxCoordOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
            return errCode;
    ulCurrentOffset = ulOffset + usBytesRead;
    for (i = 0; i < BASEMinMax.FeatMinMaxCount; ++i)
    {
        if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEFeatMinMaxRecord, SIZEOF_BASEFEATMINMAXRECORD, BASEFEATMINMAXRECORD_CONTROL, ulCurrentOffset, &usBytesRead ) )!= NO_ERROR)
            return errCode;
        ulCurrentOffset += usBytesRead;
        if (BASEFeatMinMaxRecord.MinCoordOffset != 0)
            if ((errCode = ProcessBaseCoord( pInputBufferInfo, ulOffset + BASEFeatMinMaxRecord.MinCoordOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
                return errCode;
        if (BASEFeatMinMaxRecord.MaxCoordOffset != 0)
            if ((errCode = ProcessBaseCoord( pInputBufferInfo, ulOffset + BASEFeatMinMaxRecord.MaxCoordOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
                return errCode;
    }
    return NO_ERROR;
}
/* ------------------------------------------------------------------- */
/* Entry Point to module */
/* function to grab all referenced glyph IDs from GSUB, JSTF and BASE */
/* TTO tables and add them into the list of glyphs to keep */ 
/* ------------------------------------------------------------------- */
int16 TTOAutoMap( TTFACC_FILEBUFFERINFO * pInputBufferInfo,   /* ttfacc info */
                 uint8 * pabKeepGlyphs, /* binary list of glyphs to keep - to be updated here */
                 uint16 usnGlyphs,    /* number of glyphs in list */
                 uint16 fKeepFlag) /* flag index (really contains a number) of what to set in the pabKeepGlyph list */
{
GSUBHEADER GSUBHeader;
GSUBLOOKUPLIST GSUBLookupList, *pGSUBLookupList = NULL;
JSTFHEADER JSTFHeader;
JSTFSCRIPTRECORD *ScriptRecordArray;
JSTFSCRIPT JSTFScript;
JSTFEXTENDERGLYPH JSTFExtenderGlyph;
uint16 *GlyphIDArray;
BASEHEADER BASEHeader;
BASEAXIS BASEAxis;
BASESCRIPTLIST BASEScriptList;
BASESCRIPTRECORD BASEScriptRecord;
BASESCRIPT BASEScript;
BASEVALUES BASEValues;
BASELANGSYSRECORD BASELangSysRecord;
uint16 BASECoordOffset;
uint16 AxisOffset;
uint32 ulHeaderOffset;
uint32 ulCurrentOffset;
uint32 ulLangSysOffset;
uint16 usMaxLookupCount;
uint32 ulOffset;
uint16 i, j, k;
uint16 usBytesRead;
uint32 ulBytesRead;
int16 errCode = NO_ERROR;


/* Process GSUB Table */
    while (1)  /* so we can break out on null offsets */
    {
        ulHeaderOffset = TTTableOffset( pInputBufferInfo, GSUB_TAG );

        if (ulHeaderOffset == 0)
            break;

        if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&GSUBHeader, SIZEOF_GSUBHEADER, GSUBHEADER_CONTROL, ulHeaderOffset, &usBytesRead ) )!= NO_ERROR)
            return errCode;

        /* now read the max number of lookups, for allocation of the lookup list */

        if (GSUBHeader.LookupListOffset == 0)
            break;
        if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&GSUBLookupList, SIZEOF_GSUBLOOKUPLIST, GSUBLOOKUPLIST_CONTROL, ulHeaderOffset + GSUBHeader.LookupListOffset, &usBytesRead) )!= NO_ERROR)
            return errCode;
        usMaxLookupCount = GSUBLookupList.LookupCount;

        if (usMaxLookupCount == 0)
            break;
    
        while (1) /* so we can break out and clean up on error */
        /* Now look at lookup table, and add to list from Context lookups */
        {
        GSUBLOOKUP GSUBLookup;
        uint16 *SubstTableOffsetArray = NULL;

            ulOffset = ulHeaderOffset + GSUBHeader.LookupListOffset;
            pGSUBLookupList = (GSUBLOOKUPLIST *)Mem_Alloc(SIZEOF_GSUBLOOKUPLIST + usMaxLookupCount * sizeof(uint16));
            if (pGSUBLookupList == NULL)
            {
                errCode = ERR_MEM;
                break;
            }
            /* read the first part */
             if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *) pGSUBLookupList, SIZEOF_GSUBLOOKUPLIST, GSUBLOOKUPLIST_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                break;
            /* now read the array */
            if ((errCode = ReadGenericRepeat( pInputBufferInfo,  (uint8 *) ((uint8 *)pGSUBLookupList + SIZEOF_GSUBLOOKUPLIST), WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, usMaxLookupCount, sizeof(uint16)) )!= NO_ERROR)
                break;

    /* now make sure all the referenced glyphs are in the keep table */

            for (i = 0; i < usMaxLookupCount; ++i)
            {
            uint16 usSubTableCount;

                if (pGSUBLookupList->LookupTableOffsetArray[i] == 0)
                    continue;
                ulOffset = ulHeaderOffset + 
                            GSUBHeader.LookupListOffset + 
                            pGSUBLookupList->LookupTableOffsetArray[i];
                if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBLookup, SIZEOF_GSUBLOOKUP, GSUBLOOKUP_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                    break;
                if (GSUBLookup.LookupType == GSUBContextLookupType)  /* not looking for context lookups */
                    continue;
                usSubTableCount = GSUBLookup.SubTableCount;
                SubstTableOffsetArray = (uint16 *)Mem_Alloc(sizeof(uint16) * usSubTableCount);
                if (SubstTableOffsetArray == NULL)
                {
                    errCode = ERR_MEM;
                    break;
                }
                if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *) SubstTableOffsetArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, usSubTableCount, sizeof(uint16)) )!= NO_ERROR)
                    break;
            
                for (j = 0; j < usSubTableCount; ++j)
                {
                uint16 Format;

                     if (SubstTableOffsetArray[j] == 0)
                        continue;
                     ulOffset = ulHeaderOffset + 
                                GSUBHeader.LookupListOffset + 
                                pGSUBLookupList->LookupTableOffsetArray[i] +
                                SubstTableOffsetArray[j];
                    if ((errCode = ReadWord( pInputBufferInfo, &Format, ulOffset) )!= NO_ERROR)
                        break;
                    switch(GSUBLookup.LookupType)
                    {
                    case GSUBSingleLookupType:
                    {
                        switch    (Format)
                        {
                        case 1:
                        {
                        GSUBSINGLESUBSTFORMAT1 GSUBSubstTable;

                            if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBSubstTable, SIZEOF_GSUBSINGLESUBSTFORMAT1, GSUBSINGLESUBSTFORMAT1_CONTROL, ulOffset, &usBytesRead) )== NO_ERROR)
                                errCode = UpdateKeepWithCoverage( pInputBufferInfo, pabKeepGlyphs, usnGlyphs, fKeepFlag, ulOffset, GSUBSubstTable.CoverageOffset , (uint16 *) &(GSUBSubstTable.DeltaGlyphID), GSUBLookup.LookupType, Format);
                            break;
                        }
                        case 2:
                        {
                        GSUBSINGLESUBSTFORMAT2 GSUBSubstTable;
                        uint16 usGlyphCount;
                        uint16 *pGlyphIDArray = NULL;

                            if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBSubstTable, SIZEOF_GSUBSINGLESUBSTFORMAT2, GSUBSINGLESUBSTFORMAT2_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                                break;
                            usGlyphCount = GSUBSubstTable.GlyphCount;
                            pGlyphIDArray = (uint16 *)Mem_Alloc(sizeof(uint16) * usGlyphCount);
                            if (pGlyphIDArray == NULL)
                            {
                                errCode = ERR_MEM;
                                break;
                            }
                            if ((errCode = ReadGenericRepeat( pInputBufferInfo,  (uint8 *)pGlyphIDArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, usGlyphCount, sizeof(uint16)) )== NO_ERROR)
                                errCode = UpdateKeepWithCoverage( pInputBufferInfo, pabKeepGlyphs, usnGlyphs, fKeepFlag, ulOffset, GSUBSubstTable.CoverageOffset , pGlyphIDArray, GSUBLookup.LookupType, Format);
                            Mem_Free(pGlyphIDArray);
                            break;
                        }

                        default:
                            errCode = ERR_INVALID_GSUB;
                            break;
                        }
                        break;
                    }
                    case GSUBMultipleLookupType:
                    {
                    GSUBMULTIPLESUBSTFORMAT1 GSUBSubstTable;
                    uint16 usCount;
                    uint16 *pOffsetArray = NULL;

                        if (Format != 1)
                            break;
                        if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBSubstTable, SIZEOF_GSUBMULTIPLESUBSTFORMAT1, GSUBMULTIPLESUBSTFORMAT1_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                            break;
                        usCount = GSUBSubstTable.SequenceCount;
                        pOffsetArray = (uint16 *)Mem_Alloc(sizeof(uint16) * usCount);
                        if (pOffsetArray == NULL)
                        {
                            errCode = ERR_MEM;
                            break;
                        }
                        if ((errCode = ReadGenericRepeat( pInputBufferInfo,  (uint8 *)pOffsetArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, usCount, sizeof(uint16)) )== NO_ERROR)
                            errCode = UpdateKeepWithCoverage( pInputBufferInfo, pabKeepGlyphs, usnGlyphs, fKeepFlag, ulOffset, GSUBSubstTable.CoverageOffset , pOffsetArray, GSUBLookup.LookupType, Format);
                        Mem_Free(pOffsetArray);
                        break;
                    }
                    case GSUBAlternateLookupType:
                    {
                    GSUBALTERNATESUBSTFORMAT1 GSUBSubstTable;
                    uint16 usCount;
                    uint16 *pOffsetArray = NULL;

                        if (Format != 1)
                            break;
                        if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBSubstTable, SIZEOF_GSUBALTERNATESUBSTFORMAT1, GSUBALTERNATESUBSTFORMAT1_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                            break;
                        usCount = GSUBSubstTable.AlternateSetCount;
                        pOffsetArray = (uint16 *)Mem_Alloc(sizeof(uint16) * usCount);
                        if (pOffsetArray == NULL)
                        {
                            errCode = ERR_MEM;
                            break;
                        }
                        if ((errCode = ReadGenericRepeat( pInputBufferInfo,  (uint8 *)pOffsetArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, usCount, sizeof(uint16)) )== NO_ERROR)
                            errCode = UpdateKeepWithCoverage( pInputBufferInfo, pabKeepGlyphs, usnGlyphs, fKeepFlag, ulOffset, GSUBSubstTable.CoverageOffset , pOffsetArray, GSUBLookup.LookupType, Format);
                        Mem_Free(pOffsetArray);
                        break;
                    }
                    case GSUBLigatureLookupType:
                    {
                    GSUBLIGATURESUBSTFORMAT1 GSUBSubstTable;
                    uint16 usCount;
                    uint16 *pOffsetArray = NULL;

                        if (Format != 1)
                            break;
                        if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&GSUBSubstTable, SIZEOF_GSUBLIGATURESUBSTFORMAT1, GSUBLIGATURESUBSTFORMAT1_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                            break;
                        usCount = GSUBSubstTable.LigatureSetCount;
                        pOffsetArray = (uint16 *)Mem_Alloc(sizeof(uint16) * usCount);
                        if (pOffsetArray == NULL)
                        {
                            errCode = ERR_MEM;
                            break;
                        }
                        if ((errCode = ReadGenericRepeat( pInputBufferInfo,  (uint8 *)pOffsetArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, usCount, sizeof(uint16)) )== NO_ERROR)
                             errCode = UpdateKeepWithCoverage( pInputBufferInfo, pabKeepGlyphs, usnGlyphs, fKeepFlag, ulOffset, GSUBSubstTable.CoverageOffset , pOffsetArray, GSUBLookup.LookupType, Format);
                        Mem_Free(pOffsetArray);
                        break;
                    }
                    default:
                        break;
                    }
                    if (errCode != NO_ERROR)
                        break;

                }
                Mem_Free(SubstTableOffsetArray);
                if (errCode != NO_ERROR)
                    break;
            }
            break; /* artificial while for error conditions */
        }    
        Mem_Free(pGSUBLookupList);
        break;
    }

    if (errCode != NO_ERROR)
        return errCode;
/* Process JSTF Table */
    while (1) /* so we can break out on NULL offsets */
    {
        ulHeaderOffset = TTTableOffset( pInputBufferInfo, JSTF_TAG );

        if (ulHeaderOffset == 0)
            break;

        if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&JSTFHeader, SIZEOF_JSTFHEADER, JSTFHEADER_CONTROL, ulHeaderOffset, &usBytesRead ) )!= NO_ERROR)
            break;
        ScriptRecordArray = (JSTFSCRIPTRECORD *)Mem_Alloc(SIZEOF_JSTFSCRIPTRECORD * JSTFHeader.ScriptCount);
        if (ScriptRecordArray == NULL)
        {
            errCode = ERR_MEM;
            break;
        }

        ulOffset = ulHeaderOffset;
        if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *)ScriptRecordArray, JSTFSCRIPTRECORD_CONTROL, ulOffset + usBytesRead, & ulBytesRead, JSTFHeader.ScriptCount, SIZEOF_JSTFSCRIPTRECORD ) )!= NO_ERROR)
            break;
    
        for (i = 0 ; i < JSTFHeader.ScriptCount; ++i)
        {
            if (ScriptRecordArray[i].JstfScriptOffset == 0)
                continue;
            ulOffset = ulHeaderOffset + ScriptRecordArray[i].JstfScriptOffset;
            if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&JSTFScript, SIZEOF_JSTFSCRIPT, JSTFSCRIPT_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                break;
            if (JSTFScript.ExtenderGlyphOffset == 0)
                continue;
            ulOffset = ulOffset = ulHeaderOffset + ScriptRecordArray[i].JstfScriptOffset + JSTFScript.ExtenderGlyphOffset;

            if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&JSTFExtenderGlyph, SIZEOF_JSTFEXTENDERGLYPH, JSTFEXTENDERGLYPH_CONTROL, ulOffset, &usBytesRead) )!= NO_ERROR)
                break;

            GlyphIDArray = (uint16 *)Mem_Alloc((sizeof(uint16) * JSTFExtenderGlyph.ExtenderGlyphCount));
            if (GlyphIDArray == NULL)
            {
                errCode = ERR_MEM;
                break;
            }
            if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *)GlyphIDArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, JSTFExtenderGlyph.ExtenderGlyphCount, sizeof(uint16)) )== NO_ERROR)
            {
                for (j = 0; j < JSTFExtenderGlyph.ExtenderGlyphCount; ++j)
                {
                    if (GlyphIDArray[j] < usnGlyphs && pabKeepGlyphs[GlyphIDArray[j]] == 0)
                        pabKeepGlyphs[GlyphIDArray[j]] =  (uint8)(fKeepFlag + 1);
                } 
            }
            Mem_Free(GlyphIDArray);
            if (errCode != NO_ERROR)
                break;
        }
        Mem_Free(ScriptRecordArray);
        break; 
    }
    if (errCode != NO_ERROR)
        return errCode;
 /* Process BASE Table */
    while (1)
    {
        ulHeaderOffset = TTTableOffset( pInputBufferInfo, BASE_TAG );

        if (ulHeaderOffset == 0)
            break;

        if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEHeader, SIZEOF_BASEHEADER, BASEHEADER_CONTROL, ulHeaderOffset, &usBytesRead ) )!= NO_ERROR)
            break;

        AxisOffset = BASEHeader.HorizAxisOffset;
        for (i = 0; i < 2; ++i, AxisOffset = BASEHeader.VertAxisOffset)  /* process the 2 axis */
        {
             if (AxisOffset == 0)
                continue;
             ulOffset = ulHeaderOffset + AxisOffset;
            if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEAxis, SIZEOF_BASEAXIS, BASEAXIS_CONTROL, ulOffset, &usBytesRead ) )!= NO_ERROR)
                break;
             if (BASEAxis.BaseScriptListOffset == 0)
                continue;
             ulOffset = ulHeaderOffset + AxisOffset + BASEAxis.BaseScriptListOffset;
            if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEScriptList, SIZEOF_BASESCRIPTLIST, BASESCRIPTLIST_CONTROL, ulOffset, &usBytesRead ) )!= NO_ERROR)
                break;
            ulCurrentOffset = ulOffset + usBytesRead;
            for (j = 0; j < BASEScriptList.BaseScriptCount; ++j)
            {
            uint32 ulLocalOffset;

                if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEScriptRecord, SIZEOF_BASESCRIPTRECORD, BASESCRIPTRECORD_CONTROL, ulCurrentOffset, &usBytesRead ) )!= NO_ERROR)
                    break;
                ulCurrentOffset += usBytesRead;
                if (BASEScriptRecord.BaseScriptOffset == 0)
                    continue;

                if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEScript, SIZEOF_BASESCRIPT, BASESCRIPT_CONTROL, ulOffset + BASEScriptRecord.BaseScriptOffset, &usBytesRead ) )!= NO_ERROR)
                    break;

                ulLangSysOffset = ulOffset + BASEScriptRecord.BaseScriptOffset + usBytesRead; 

                /* PROCESS BaseValuesOffset */
                if (BASEScript.BaseValuesOffset    != 0)
                {
                    ulLocalOffset = ulOffset + BASEScriptRecord.BaseScriptOffset + BASEScript.BaseValuesOffset ;
        
                    if ((errCode = ReadGeneric( pInputBufferInfo,   (uint8 *)&BASEValues, SIZEOF_BASEVALUES, BASEVALUES_CONTROL, ulLocalOffset, &usBytesRead ) )!= NO_ERROR)
                        break;
                    ulLocalOffset += usBytesRead;
                    for (k = 0; k < BASEValues.BaseCoordCount; ++k)
                    {
                        if ((errCode = ReadWord( pInputBufferInfo,  &BASECoordOffset, ulLocalOffset ) )!= NO_ERROR)
                            break;
                        ulLocalOffset += sizeof(uint16);
                        if ((errCode = ProcessBaseCoord( pInputBufferInfo, ulOffset + BASEScriptRecord.BaseScriptOffset + BASEScript.BaseValuesOffset + BASECoordOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
                            break;
                    }
                    if (errCode != NO_ERROR)
                        break;
                }
                /* Process MinMaxOffset */
                if (BASEScript.MinMaxOffset != 0)
                {
                    ulLocalOffset = ulOffset + BASEScriptRecord.BaseScriptOffset + BASEScript.MinMaxOffset ;
                    if ((errCode = ProcessMinMax( pInputBufferInfo, ulLocalOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
                        break;
                }
                /* Process BaseLangSysRecordArray */
                for (k = 0; k < BASEScript.BaseLangSysCount; ++k)
                {
                    if ((errCode = ReadGeneric( pInputBufferInfo,  (uint8 *)&BASELangSysRecord, SIZEOF_BASELANGSYSRECORD, BASELANGSYSRECORD_CONTROL, ulLangSysOffset, &usBytesRead ) )!= NO_ERROR)
                        break;
                    ulLangSysOffset += usBytesRead;
                    if (BASELangSysRecord.MinMaxOffset != 0)
                        if ((errCode = ProcessMinMax( pInputBufferInfo, ulOffset + BASEScriptRecord.BaseScriptOffset + BASELangSysRecord.MinMaxOffset, pabKeepGlyphs, usnGlyphs, fKeepFlag))!= NO_ERROR)
                            break;
                }
                if (errCode != NO_ERROR)
                    break;
            }
            if (errCode != NO_ERROR)
                break;
        }
        break;
    }
    return errCode;

}
#ifdef APPLE_AUTOMAP   /* this is not defined for now. */
/* ------------------------------------------------------------------- */
/* Entry Point for Module */
/* function to read all the glyph IDs from the Apple cmap, and add them*/
/* into the list of glyphs to keep */
/* ------------------------------------------------------------------- */
int16 AppleAutoMap( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                   uint8 * pabKeepGlyphs, 
                   uint16 usnGlyphs,
                   uint16 fKeepFlag)
{
   /* this routine adds any glyphs from the Macintosh (Apple) Cmap */
   /* into the list of glyphs to keep. */ 
CMAP_FORMAT0 CmapFormat0;
CMAP_FORMAT6 CmapFormat6;
uint16 usFoundEncoding;
uint16 *glyphIndexArray;
uint16 i;

   /* read the apple table, if present */

    if ( ReadCmapFormat0( pInputBufferInfo, TTFSUB_APPLE_PLATFORMID, TTFSUB_STD_MAC_CHAR_SET, &usFoundEncoding, &CmapFormat0 ) == NO_ERROR)
    {
        /* read the apple cmap data so that all glyphs in apple cmap will be */
        /* added to the list of glyphs to keep */

        for ( i = 0; i < CMAP_FORMAT0_ARRAYCOUNT; i++ )     /* these are byte values, so don't need to swap */
        {
               if (CmapFormat0.glyphIndexArray[i] < usnGlyphs && pabKeepGlyphs[CmapFormat0.glyphIndexArray[i]] == 0) 
                pabKeepGlyphs[CmapFormat0.glyphIndexArray[i]] = fKeepFlag + 1;  /* keep this one */
        }
    }
    if ( ReadAllocCmapFormat6( pInputBufferInfo, TTFSUB_APPLE_PLATFORMID, TTFSUB_STD_MAC_CHAR_SET, &usFoundEncoding, &CmapFormat6, &glyphIndexArray) == NO_ERROR)
    {
        /* read the apple cmap data so that all glyphs in apple cmap will be */
        /* added to the list of glyphs to keep */

       for ( i = 0; i < CmapFormat6.entryCount; i++ )     /* these are byte values, so don't need to swap */
       {
               if (glyphIndexArray[i] < usnGlyphs && pabKeepGlyphs[glyphIndexArray[i]] == 0)
                pabKeepGlyphs[glyphIndexArray[i]] = fKeepFlag + 1;  /* keep this one */
       }
       FreeCmapFormat6(glyphIndexArray);
    }

   return NO_ERROR;
}
#endif

/* ------------------------------------------------------------------- */
