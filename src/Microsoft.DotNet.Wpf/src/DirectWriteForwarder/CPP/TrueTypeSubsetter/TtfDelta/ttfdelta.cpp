// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFDelta.C
 *
 *
 * TTFSub library entry point SubsetTTF() and CreateDeltaTTF()
 * This library allows the subsetting of a font to prepare it for font 
 * embedding. Subsetting involves removing the data for glyphs not needed
 * but keeping Glyph Indices the same. Also, reducing table sizes for some
 * of the other tables if possible.
 *
 **************************************************************************/

/* Inclusions ----------------------------------------------------------- */
#include <stdlib.h>
#include <string.h> /* for memcpy */

#include "typedefs.h"
#include "ttff.h"
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "ttftabl1.h"
#include "ttfdelta.h"
#include "ttferror.h"
#include "ttmem.h"
#include "makeglst.h"
#include "ttftable.h"
#include "modtable.h"
#include "modglyf.h"
#include "modcmap.h"
#include "modsbit.h"

/* ---------------------------------------------------------------------- */
int16 TTCOffsetTableOffset(
    /* 0 */ CONST unsigned char * puchSrcBuffer, /* input TTF or TTC buffer */
    /* 1 */ CONST unsigned long ulSrcBufferSize, /* size of input TTF or TTC buffer data */
    /* 6 */ CONST unsigned short usTTCIndex,    /* TTC Index, only used if TTC bit set */
            uint32 *pulOffsetTableOffset)
{
CONST_TTFACC_FILEBUFFERINFO InputBufferInfo;
int16 errCode = NO_ERROR;
TTC_HEADER TTCHeader;
uint16 usBytesRead;
uint32 ulOffset;

    InputBufferInfo.puchBuffer = puchSrcBuffer;
    InputBufferInfo.ulBufferSize = ulSrcBufferSize;
    InputBufferInfo.ulOffsetTableOffset = *pulOffsetTableOffset = 0;
    InputBufferInfo.lpfnReAllocate = NULL; /* can't reallocate input buffer */

    if ((errCode = ReadGeneric((TTFACC_FILEBUFFERINFO *) &InputBufferInfo, (uint8 *) &TTCHeader, SIZEOF_TTC_HEADER, TTC_HEADER_CONTROL, 0, &usBytesRead)) != NO_ERROR)
        return(errCode);
    ulOffset = usBytesRead;

    if (TTCHeader.TTCTag != TTC_LONG_TAG) /* this isn't a ttc */
        return ERR_NOT_TTC;  /* offset set correctly for ttf */

    if (usTTCIndex >= TTCHeader.DirectoryCount)
        return ERR_INVALID_TTC_INDEX;

    ulOffset += GetGenericSize(LONG_CONTROL) * usTTCIndex;
    if ((errCode = ReadLong((TTFACC_FILEBUFFERINFO *) &InputBufferInfo, pulOffsetTableOffset, ulOffset)) != NO_ERROR)
        return(errCode);

    return errCode;
}
/* ---------------------------------------------------------------------- */
PRIVATE int16 ExitCleanup(int16 errCode)
{
    Mem_End();
    return(errCode);
}
/* ------------------------------------------------------------------- */

PRIVATE int16 CopyOffsetDirectoryTables(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                                       TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                                       uint16 usFormat, 
                                       uint32 * pulNewOutOffset )
{
DIRECTORY *aDirectory;
DIRECTORY Directory;
OFFSET_TABLE OffsetTable;
DTTF_HEADER DttfHeader;
uint16 usnTables;
uint16 usnNewTables;
uint32 ulOffset;
uint32 ulDttfOffset;
uint16 usTableIdx;
uint16 usBytesRead;
uint16 usBytesWritten;
uint32 ulBytesWritten;
int16 errCode;

    ulDttfOffset = TTTableOffset( (TTFACC_FILEBUFFERINFO *)pInputBufferInfo, DTTF_TAG); /* check to see if one there already */
    if (ulDttfOffset != DIRECTORY_ERROR)
    {
        if ((errCode = ReadGeneric((TTFACC_FILEBUFFERINFO *) pInputBufferInfo, (uint8 *) &DttfHeader, SIZEOF_DTTF_HEADER, DTTF_HEADER_CONTROL, ulDttfOffset, &usBytesRead)) != NO_ERROR)
            return(errCode);
        if (DttfHeader.format != TTFDELTA_MERGE) /* only acceptable delta font at this time */
            return(ERR_INVALID_DELTA_FORMAT);
    }

    /* read offset table and determine number of existing tables */
    ulOffset = pInputBufferInfo->ulOffsetTableOffset;
    if ((errCode = ReadGeneric((TTFACC_FILEBUFFERINFO *) pInputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
        return(errCode);
    usnTables = OffsetTable.numTables;
    ulOffset += usBytesRead;
    /* Create a list of valid tables */

    aDirectory = (DIRECTORY *) Mem_Alloc((usnTables + (ulDttfOffset == 0)) * sizeof(DIRECTORY));    /* one extra for possible private table */
    if (aDirectory == NULL)
        return(ERR_MEM);
    

    /* sort directories by offset */

    for ( usTableIdx = usnNewTables = 0; usTableIdx < usnTables; usTableIdx++ )
    {
        errCode = ReadGeneric((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesRead);
        ulOffset += usBytesRead;

        if (errCode != NO_ERROR)
        {
            Mem_Free(aDirectory);
            return errCode;
        }
        if (usFormat == TTFDELTA_DELTA) /* need to get rid of some of the tables */
        {
            switch(Directory.tag)/* only want to keep these */
            {
            /* tables sent each time */
            case HEAD_LONG_TAG:
            case MAXP_LONG_TAG:
            case HHEA_LONG_TAG:
            case VHEA_LONG_TAG:
                /* tables subsetted */
            case CMAP_LONG_TAG:
            case GLYF_LONG_TAG:
            case EBLC_LONG_TAG:
            case EBDT_LONG_TAG:
            case BLOC_LONG_TAG:
            case BDAT_LONG_TAG:
                /* tables compacted */
            case LTSH_LONG_TAG:
            case HMTX_LONG_TAG:
            case VMTX_LONG_TAG:
            case HDMX_LONG_TAG:
            case LOCA_LONG_TAG:
                /* private table - keep shell */
            case DTTF_LONG_TAG:
                break;
            default:  /* any others, just get rid of */
              continue; /* don't copy this over */
            }
        }

        /* empty out the entries */
        aDirectory[ usnNewTables ].length = 0;
        aDirectory[ usnNewTables ].offset = DIRECTORY_ERROR;
        aDirectory[ usnNewTables ].tag = Directory.tag; /* don't worry about the checksum */
        ++ usnNewTables;
    }
    /* add in dttf entry */
    if (ulDttfOffset == 0 && usFormat == TTFDELTA_SUBSET1 || usFormat == TTFDELTA_DELTA)
    {
        aDirectory[ usnNewTables].length = 0;
        aDirectory[ usnNewTables].offset = DIRECTORY_ERROR;
        aDirectory[ usnNewTables].tag = DTTF_LONG_TAG;
        ++usnNewTables;
        SortByTag( aDirectory, usnNewTables );    /* to insert the dttf table */
    }

    OffsetTable.numTables = usnNewTables; /* don't worry if other fields not ok, will be updated in compress tables */
    ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
    errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesWritten);
    /* write out the new directory info to the output buffer */
    ulOffset += usBytesWritten;
    if (errCode == NO_ERROR)
    {
        errCode = WriteGenericRepeat( pOutputBufferInfo, (uint8 *) aDirectory, DIRECTORY_CONTROL, ulOffset, &ulBytesWritten, usnNewTables, SIZEOF_DIRECTORY );
        if (errCode == NO_ERROR)
            *pulNewOutOffset = ulOffset+ulBytesWritten;  /* end of written to data */
    }
    Mem_Free(aDirectory);

    return(errCode);
}
/* ---------------------------------------------------------------------- */
PRIVATE int16 CopyForgottenTables( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                                 TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                                 uint32 * pulNewOutOffset )
{
DIRECTORY *aDirectory;
OFFSET_TABLE OffsetTable;
uint16 usnTables;
uint16 usTableIdx;
uint16 usBytesRead;
uint32 ulBytesRead;
uint32 ulOffset;
int16 errCode;
char szTag[5];

    /* read offset table and determine number of existing tables */
    ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
    if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
        return(ERR_MEM);
    ulOffset += usBytesRead;

    usnTables = OffsetTable.numTables;
    /* Create a list of valid tables */

    aDirectory = (DIRECTORY *) Mem_Alloc((usnTables) * sizeof(DIRECTORY));
    if (aDirectory == NULL)
        return(ERR_MEM);

    errCode = ReadGenericRepeat( pOutputBufferInfo, (uint8 *) aDirectory, DIRECTORY_CONTROL, ulOffset, &ulBytesRead, usnTables, SIZEOF_DIRECTORY );

    if (errCode != NO_ERROR)
    {
        Mem_Free(aDirectory);
        return errCode;
    }
    /* sort directories by offset */

    SortByOffset( aDirectory, usnTables );  /* will sort all the zero offsets to the beginning */
    
    for ( usTableIdx = 0; usTableIdx < usnTables; usTableIdx++ )
    {
        /* copy the forgotten table from the input file to the output file */
        if (aDirectory[ usTableIdx ].length == 0 &&
            aDirectory[ usTableIdx ].offset == DIRECTORY_ERROR)
        {
            if (aDirectory[ usTableIdx ].tag != DELETETABLETAG) /* it hasn't been marked for deletion */
            {
            /* Copy the table contents over, and update the directory */
                ConvertLongTagToString(aDirectory[ usTableIdx ].tag, szTag);
                if ((errCode = CopyTableOver( pOutputBufferInfo, pInputBufferInfo, szTag, pulNewOutOffset )) != NO_ERROR)
                    break;
            }
        }
        else
            break; /* we're done with all the forgotten tables */
    }

    Mem_Free(aDirectory);

    return(errCode);
}
/* ---------------------------------------------------------------------- */

/* ---------------------------------------------------------------------- */
PRIVATE void FillGlyphIndexArray(
                            __in_ecount(usGlyphListCount) CONST uint8 *puchKeepGlyphList, 
                            CONST uint16 usGlyphListCount,
                            __out_ecount(usDttfGlyphIndexCount) uint16 *pusGlyphIndexArray,
                            uint16 usDttfGlyphIndexCount)
{
uint16 i;
uint16 usGlyphIndex = 0;

    for (i = 0; i < usGlyphListCount && usGlyphIndex < usDttfGlyphIndexCount; ++i)
    {
        if (puchKeepGlyphList[i])
        {
            pusGlyphIndexArray[usGlyphIndex] = i;
            ++usGlyphIndex;
        }
    }
}
/* ------------------------------------------------------------------- */
/* call this at the very end, before tables. */
/* ------------------------------------------------------------------- */
PRIVATE int16 CompactMaxpLocaTable(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                                 uint8 *puchKeepGlyphList, 
                                 uint16 usGlyphListCount, 
                                 uint16 usDttfGlyphIndexCount)
{
uint16 i,j;
uint16 usOffset;
uint16 usBytesWritten;
int16 errCode = NO_ERROR;
uint32 * aulLoca;
uint32 ulLocaLength;
uint32 ulHeadOffset;
uint32 ulLocaOffset;
uint32 ulMaxpOffset;
MAXP MaxP;
HEAD Head;

    /* Check if we need to collapse loca into a compact format */
    if (!usDttfGlyphIndexCount) /* it means we have a shorter list than the full glyph list */
        return errCode;

    if ((ulHeadOffset = GetHead(pOutputBufferInfo, &Head)) == 0L)
        return ERR_MISSING_HEAD;
    aulLoca = (uint32 *)Mem_Alloc( (usGlyphListCount + 1) * sizeof( uint32 ));
    if ( aulLoca == NULL )
        return ERR_MEM;

    if ((ulLocaOffset = GetLoca(pOutputBufferInfo, aulLoca, usGlyphListCount + 1)) == 0L)
    {
        Mem_Free(aulLoca);
        return ERR_MISSING_LOCA;
    }

    /* write out the compact 'loca' table */
    /* Check to see what format to use */
    if (Head.indexToLocFormat == SHORT_OFFSETS)   /* maximum number stored here */
    {
        for ( i = 0, j= 0; i <= usGlyphListCount && j <= usDttfGlyphIndexCount; i++ )
        {
            if ((j == usDttfGlyphIndexCount) || (i < usGlyphListCount && puchKeepGlyphList[i]))
            {
                usOffset = (uint16) (aulLoca[ i ] / 2L);
                if ((errCode = WriteWord( pOutputBufferInfo,  usOffset, ulLocaOffset + j*sizeof(uint16) )) != NO_ERROR)
                    break;
                ++j;
            }
        }
        ulLocaLength = (uint32) (usDttfGlyphIndexCount+1) * sizeof(uint16);
    }
    else
    {
        for ( i = 0, j= 0; i <= usGlyphListCount && j <= usDttfGlyphIndexCount; i++ )
        {
            if ((j == usDttfGlyphIndexCount) || (i < usGlyphListCount && puchKeepGlyphList[i]))
            {
                if ((errCode = WriteLong( pOutputBufferInfo,  aulLoca[ i ], ulLocaOffset + j*sizeof(uint32) )) != NO_ERROR)
                    break;
                ++j;
            }
        }
        ulLocaLength = (uint32) (usDttfGlyphIndexCount+1) * sizeof(uint32);
    }

    Mem_Free(aulLoca);
    if ((errCode = UpdateDirEntry( pOutputBufferInfo, LOCA_TAG, ulLocaLength )) != NO_ERROR)
        return errCode;

    /* get old maxp record */
    if ((ulMaxpOffset = GetMaxp( pOutputBufferInfo, &MaxP)) == 0L)
        return ERR_MISSING_MAXP;

    MaxP.numGlyphs = usDttfGlyphIndexCount; /* set to fake value to save space in loca, hmtx, vmtx, hdmx, LTSH */
    if (errCode == NO_ERROR)
        errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &MaxP, SIZEOF_MAXP, MAXP_CONTROL, ulMaxpOffset, &usBytesWritten );
    return errCode;
}

/* ---------------------------------------------------------------------- */
/* if a private dttf table is needed, check to make sure there is room.
/* if there is:
  1. create the Directory entry
  2. Read in offset table
  3. Read in Directory entries
  4. If there is no dttf directory entry, 
     a   move all data in file forward by size of directory entry - yech
     b. Insert dttf entry in directory
     c. update all offset values by the incremented value
  5. Otherwise, write out new dttf directory entry data w/ length
  7. Write the dttf table to the end of the file
  8. Re-calculate file checksum and update head table

/* ---------------------------------------------------------------------- */
PRIVATE int16 UpdatePrivateTable(TTFACC_FILEBUFFERINFO *pOutputBufferInfo, 
                                uint32 *pulNewOutOffset, 
                                CONST uint16 * pusGlyphIndexArray, 
                                CONST uint16 usDttfGlyphIndexCount, 
                                CONST uint16 usNumGlyphs, 
                                CONST uint16 usFormat, 
                                CONST uint32 ulCheckSum)
{   
DTTF_HEADER dttf_header;
DIRECTORY DttfDirectory;
uint32 ulOffset;
int16 errCode;
uint16 i;
uint16 usBytesWritten;


    if (usFormat != TTFDELTA_SUBSET1 && usFormat != TTFDELTA_DELTA) /* formats with dttf tables */
        return NO_ERROR;

    ulOffset = GetTTDirectory( pOutputBufferInfo, DTTF_TAG, &DttfDirectory); 

    if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, *pulNewOutOffset, &(DttfDirectory.offset))) != NO_ERROR)
        return errCode;
    DttfDirectory.length = GetGenericSize(DTTF_HEADER_CONTROL) + usDttfGlyphIndexCount * sizeof(uint16);

    if (ulOffset == DIRECTORY_ERROR)  /* there wasn't one there - don't really need this code - its obsolete  */
        return ERR_GENERIC;
    if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &DttfDirectory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
        return errCode; /* update the length and offset */

    /* now write out that dttf table */
    dttf_header.version = CURRENT_DTTF_VERSION;
    dttf_header.checkSum = ulCheckSum;
    dttf_header.originalNumGlyphs = usNumGlyphs; 
    dttf_header.maxGlyphIndexUsed = pusGlyphIndexArray[usDttfGlyphIndexCount-1]; /* this is needed for format 1 fonts that become format 3 fonts after a merge */
    dttf_header.format = usFormat; 
    dttf_header.fflags = 0;
    dttf_header.glyphCount = usDttfGlyphIndexCount;
    ulOffset = DttfDirectory.offset;
    
    if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &dttf_header, SIZEOF_DTTF_HEADER, DTTF_HEADER_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
        return errCode;

    ulOffset += usBytesWritten;
    for (i = 0; i < usDttfGlyphIndexCount; ++i)
    {
        if ((errCode = WriteWord( pOutputBufferInfo, pusGlyphIndexArray[i], ulOffset)) != NO_ERROR)
            return errCode;
        ulOffset += sizeof(uint16);
    }
    if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, ulOffset, &ulOffset)) != NO_ERROR)
        return errCode;
    *pulNewOutOffset = ulOffset;

    return NO_ERROR;
}
/* Format Subset will keep all tables, but discard a percentage of the Glyf and EBDT tables */
/* Format Subset1 will keep all tables, but discard a percentage of the Glyf and EBDT tables */
/*                in addition any array tables (LTSH, loca, hmtx, hdmx, vmtx) will have a percentage discarded */
/* Format Delta will keep only a list of tables, and the Subset1 compacted and Glyf tables will keep only a portion */
/* ---------------------------------------------------------------------- */
PRIVATE void CalcOutputBufferSize(CONST_TTFACC_FILEBUFFERINFO *pInputBufferInfo,
                                 uint16 usGlyphListCount,
                                 uint16 usGlyphKeepCount,
                                 uint16 usFormat,
                                 uint32 ulSrcBufferSize,
                                 uint32 *pulOutputBufferLength)
{
int32 flDiscardPercent, flKeepPercent;
uint32 ulGlyphDependentDataLength = 0;
uint32 ulEBDTTableLength = 0, ulEBDTTableOffset = 0;
uint32 ulBdatTableLength= 0;  
uint32 ulAllGlyphsLength= 0;  /* glyf, EBDT, bloc length */
uint32 ulKeepTablesLength = 0;

        /* make a good guess as to how much memory we will need */
        /* first figure out percentage of glyph's being discarded */
        flDiscardPercent =  ((usGlyphListCount - usGlyphKeepCount) *100 )/usGlyphListCount;
        flDiscardPercent -= 10;  /* subtract in 10% to reduce unneccesary reallocing */
        if (flDiscardPercent < 0)
            flDiscardPercent = 0;
        flKeepPercent = 100-flDiscardPercent;

        ulEBDTTableLength = TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, EBDT_TAG);
        /* check if EBDT and bdat are the same table */
        ulBdatTableLength = TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, BDAT_TAG);
        if (ulEBDTTableLength != DIRECTORY_ERROR && ulEBDTTableLength == ulBdatTableLength)
        {
            ulEBDTTableOffset = TTTableOffset((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, EBDT_TAG);
            if (ulEBDTTableOffset != TTTableOffset((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, BDAT_TAG))
                ulBdatTableLength = 0;          
        }
        ulAllGlyphsLength = ulEBDTTableLength + ulBdatTableLength;
        ulAllGlyphsLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, GLYF_TAG);

        if (usFormat == TTFDELTA_DELTA || usFormat == TTFDELTA_SUBSET1)
        {  /* these formats will compact some tables, discarding a percentage of these tables as well */
                /* tables compacted */
            ulGlyphDependentDataLength = TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, LTSH_TAG);
            ulGlyphDependentDataLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, HMTX_TAG);
            ulGlyphDependentDataLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, VMTX_TAG);
            ulGlyphDependentDataLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, HDMX_TAG);
            ulGlyphDependentDataLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, LOCA_TAG);
        }
        ulGlyphDependentDataLength += ulAllGlyphsLength; /* all formats will discard a percentage of the glyph data */

        if (usFormat == TTFDELTA_DELTA) /* we're going to keep just a handfull of tables tables */
        {
            ulKeepTablesLength = TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, HEAD_TAG);
            ulKeepTablesLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, MAXP_TAG);
            ulKeepTablesLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, HHEA_TAG);
            ulKeepTablesLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, VHEA_TAG);
            ulKeepTablesLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, CMAP_TAG);
            if (ulEBDTTableLength > 0)
                ulKeepTablesLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, EBLC_TAG);
            if (ulBdatTableLength > 0)
                ulKeepTablesLength += TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, BLOC_TAG);
            
            *pulOutputBufferLength = ulKeepTablesLength + (uint32)(flKeepPercent * ulGlyphDependentDataLength/100);
        }
        else
        /* for straight subset, this will be: ulSrcBufferSize - (discard % * (Glyf table size + EBDT table size + bdat table size)) */
            *pulOutputBufferLength = ulSrcBufferSize - (uint32)(flDiscardPercent * ulGlyphDependentDataLength/100);
}


/* ---------------------------------------------------------------------- */
/* ENTRY POINT !!!!
/* ---------------------------------------------------------------------- */
/*  (CONST uint8 * puchSrcBuffer   is a pointer to buffer containing source TTF or TTC data
    CONST uint32 ulSrcBufferSize   is the size in bytes of puchSrcBuffer 
    uint8 ** ppuchDestBuffer       is a pointer to buffer pointer for destination TTFData. 
                                   If this is null, it will be set by this function by calling lpfnReAllocate below.
    uint32 *pulDestBufferSize      is a pointer to a long integer that will be set with the size 
                                   in bytes of puchDestBuffer 
    uint32 * pulBytesWritten       is a pointer to a long integer where the length in bytes 
                                   of the data written to the puchDestBuffer will be written.
    CONST uint16 usFormat          format of the subset font to create. 0 = Subset, 1 = Subset/Compact, 
                                   2 = Subset/Delta
    CONST uint16 usLanguage        is the language in the Name table to retain. Set to 0 
                                   if all languages should be retained.
    CONST uint16 usListType        0 means KeepCharCodeList represents character codes from the Platform Encoding
                                   cmap specified. 1 means the KeepCharCodeList represents raw Glyph indices from
                                   the font.
    CONST uint16 usPlatform        specifies, which usEncoding which Cmap to use. By using this 
                                   cmap and the pusKeepCharCodeList, a list of glyphs to retain 
                                   in the output font can be created. Ignored for usListType = 1. 
    CONST uint16 usEncoding        used with usPlatform. Set to TTFSUB_DONT_CARE if any encoding cmap will do.
                                   Ignored for usListType = 1. 
    CONST uint16 *pusSubsetCharCodeList  ignored for now. List of characters already subsetted. 
    CONST uint16 usSubsetListCount       ignored for now  count of List of characters already subsetted. 
    CONST uint16 *pusKeepCharCodeList is an array of integers which comprise a list of 
                                   character codes that should be retained in the output font. 
                                   This list may be unicode, if used with a unicode Platform-Encoding cmap,
                                   or it may be some other type of encoding.
    CONST uint16 usListCount       is the number of elements in the pusKeepCharCodeList
    CFP_REALLOCPROC lpfnReAllocate     function supplied to reallocate memory. Defined as
                                   typedef void *(CFP_REALLOCPROC) (void *, size_t );
    void *lpvReserved
/* ---------------------------------------------------------------------- */
int16 CreateDeltaTTF(CONST uint8 * puchSrcBuffer,
            CONST uint32 ulSrcBufferSize,
            uint8 ** ppuchDestBuffer,
            uint32 * pulDestBufferSize,
            uint32 * pulBytesWritten,
            CONST uint16 usFormat,
            CONST uint16 usLanguage,
            CONST uint16 usPlatform,
            CONST uint16 usEncoding,
            CONST uint16 usListType,
            CONST uint16 *pusKeepCharCodeList,
            CONST uint16 usListCount,
            CFP_REALLOCPROC lpfnReAllocate,   /* call back function to reallocate temp and output buffers */
            CFP_FREEPROC lpfnFree,    /* call back function to output buffers on error */
            uint32 ulOffsetTableOffset,   /* for .ttf this will be 0, for .ttc, this will be a value */
            void *lpvReserved)
{
    /* wrap the new 32-bit char array function */
    int16   errCode;

    uint16 usGlyphListCount = 0;   /* number of glyph spots in font */
    CONST_TTFACC_FILEBUFFERINFO InputBufferInfo;        
    uint8 *puchKeepGlyphList = NULL;  /* list of glyphs to keep (0 = don't keep, 1 = keep)  */
    uint16 usMaxGlyphIndexUsed;
    uint16 usGlyphKeepCount = 0; /* number of actual glyphs in font */
    uint16 i, j;

    CHAR_ID pulTempKeepCharCodeList[4] = {'d', 'r', 'M', '\"'};


    CHAR_ID *pulKeepCharCodeList = NULL;
    uint16  usCharCount = 0;

    if (pusKeepCharCodeList)
    {

        /**********************************************************
        // DevDiv 1157073. Backporting this fix from windows since is causing
        // trouble when printing xps documents generated by wpf
        //
        // ONE UGLY BIG HACK!!! for Windows7: 569703
        //
        // Basically, we need to make sure the sub-setted font font contains
        // glyphs for drM" as these are the chars the LPK checks in order
        // to make a decision whether to send it down to Uniscribe for fallback
        //
        // Re-using MakeKeepGlyphList for cmap mapping
        //
        //
        **********************************************************/

        if (usListType == TTFDELTA_GLYPHLIST)
        {
            InputBufferInfo.puchBuffer = puchSrcBuffer;
            InputBufferInfo.ulBufferSize = ulSrcBufferSize;
            InputBufferInfo.ulOffsetTableOffset = ulOffsetTableOffset; /* will be non 0 for ttc support */
            InputBufferInfo.lpfnReAllocate = NULL; /* can't reallocate input buffer */

            /* find out how many glyphs */
            usGlyphListCount = GetNumGlyphs((TTFACC_FILEBUFFERINFO *)&InputBufferInfo);
            if (usGlyphListCount == 0)
                return ExitCleanup(ERR_NO_GLYPHS);

            /* allocate array of glyphs to keep */
            puchKeepGlyphList = (uint8 *)Mem_Alloc(usGlyphListCount * sizeof(uint8));
            if (puchKeepGlyphList == NULL)
                return ExitCleanup(ERR_MEM);

            // get the glyph list for the drM" characters - re-using MakeKeepGlyphList       
            if( (errCode = MakeKeepGlyphList(
                (TTFACC_FILEBUFFERINFO *)&InputBufferInfo, 
                TTFDELTA_CHARLIST, 
                3, 
                1, 
                pulTempKeepCharCodeList, 
                4, 
                puchKeepGlyphList,
                usGlyphListCount,
                &usMaxGlyphIndexUsed,
                &usGlyphKeepCount,
                FALSE)) != NO_ERROR )
            {
                Mem_Free(puchKeepGlyphList);
                return ExitCleanup(errCode); 
            }

            // make room for the extra glyph list
            usCharCount = usListCount + usGlyphKeepCount;
            pulKeepCharCodeList = (CHAR_ID *)Mem_Alloc(usCharCount * sizeof(CHAR_ID));
            if (!pulKeepCharCodeList)
            {
                Mem_Free(puchKeepGlyphList);
                return ERR_MEM;
            }

            // copy the original glyphs
            for( i = 0; i < usListCount; i++ )
            {
                pulKeepCharCodeList[i] = pusKeepCharCodeList[i];
            }

            // iterate through the glyphs and add it to our original list
            for( i = 0, j = usListCount; 
                (i <= usMaxGlyphIndexUsed) && (j < usCharCount);  // j shouldn't ever be greater than usCharCount but let's just add this check anyway
                i++ )
                {
                    if( puchKeepGlyphList[i] != 0 )
                    {
                        pulKeepCharCodeList[j++] = i;
                    }
                }
            // don't need the buffer anylonger
            Mem_Free(puchKeepGlyphList);

        }
        else
        {
            // allocate for extra 4 chars
            pulKeepCharCodeList = (CHAR_ID *)Mem_Alloc((usListCount + 4) * sizeof(CHAR_ID));
            if (!pulKeepCharCodeList)
            {
                return ERR_MEM;
            }
            
            if (UTF16toUCS4((uint16*)pusKeepCharCodeList, usListCount, pulKeepCharCodeList, usListCount, &usCharCount)!=NO_ERROR)
            {
                Mem_Free(pulKeepCharCodeList);
                return ERR_MEM;
            }

            // add the chars - we know these aren't surrogates so we are ok
            for( i = 0; i < 4; i++ )
            {
                pulKeepCharCodeList[usCharCount + i] = pulTempKeepCharCodeList[i];
            }
            usCharCount += 4;
            
        }
    }
    
    errCode = CreateDeltaTTFEx(puchSrcBuffer,
                                ulSrcBufferSize,
                                ppuchDestBuffer,
                                pulDestBufferSize,
                                pulBytesWritten,
                                usFormat,
                                usLanguage,
                                usPlatform,
                                usEncoding,
                                usListType,
                                pulKeepCharCodeList,
                                usCharCount,
                                lpfnReAllocate,
                                lpfnFree,
                                ulOffsetTableOffset,
                                lpvReserved);
    
    if (pulKeepCharCodeList)
        Mem_Free(pulKeepCharCodeList);

    return errCode;
}

int16 CreateDeltaTTFEx(CONST uint8 * puchSrcBuffer,
            CONST uint32 ulSrcBufferSize,
            uint8 ** ppuchDestBuffer,
            uint32 * pulDestBufferSize,
            uint32 * pulBytesWritten,
            CONST uint16 usFormat,
            CONST uint16 usLanguage,
            CONST uint16 usPlatform,
            CONST uint16 usEncoding,
            CONST uint16 usListType,
            CONST CHAR_ID *pulKeepCharCodeList,
            CONST uint16 usListCount,
            CFP_REALLOCPROC lpfnReAllocate,   /* call back function to reallocate temp and output buffers */
            CFP_FREEPROC lpfnFree,    /* call back function to output buffers on error */
            uint32 ulOffsetTableOffset,   /* for .ttf this will be 0, for .ttc, this will be a value */
            void *lpvReserved)
{
uint16 usGlyphListCount = 0;   /* number of glyph spots in font */
uint16 usDttfGlyphIndexCount = 0;   /* number of actual glyphs in font used for GlyphIndexArray */
uint16 usGlyphKeepCount = 0; /* number of actual glyphs in font */
uint8 *puchKeepGlyphList = NULL;  /* list of glyphs to keep (0 = don't keep, 1 = keep)  */
uint16 *pusGlyphIndexArray = NULL;
int16 errCode = NO_ERROR;
uint16 usMaxGlyphIndexUsed;
uint16 OS2MinChr = USHRT_MAX;
uint16 OS2MaxChr = 0;
uint32 checkSumAdjustment = 0; /* to save in private dttf table */
ttBoolean Mod_HDMX = TRUE;
uint32 ulNewOutOffset = 0;
TTFACC_FILEBUFFERINFO OutputBufferInfo; /* used by ttfacc routines */
CONST_TTFACC_FILEBUFFERINFO InputBufferInfo;

    /* Check inputs */
    if (puchSrcBuffer == NULL) 
        return ERR_PARAMETER0;
    if (ulSrcBufferSize == 0)
        return ERR_PARAMETER1;
    if (ppuchDestBuffer == NULL)
        return ERR_PARAMETER2;
    if (pulDestBufferSize == NULL)
        return ERR_PARAMETER3;
    if (pulBytesWritten == NULL)
        return ERR_PARAMETER4;
    if (usFormat > TTFDELTA_DELTA)  /* biggest one we know */
        return ERR_PARAMETER5;

    if (Mem_Init() != MemNoErr)   /* initialize memory manager */
        return ERR_MEM;

    InputBufferInfo.puchBuffer = puchSrcBuffer;
    InputBufferInfo.ulBufferSize = ulSrcBufferSize;
    InputBufferInfo.ulOffsetTableOffset = ulOffsetTableOffset; /* will be non 0 for ttc support */
    InputBufferInfo.lpfnReAllocate = NULL; /* can't reallocate input buffer */

    /* initialize */
    *pulBytesWritten = 0;
    
    /* find out how many glyphs */
    usGlyphListCount = GetNumGlyphs((TTFACC_FILEBUFFERINFO *)&InputBufferInfo);
    if (usGlyphListCount == 0)
        return ExitCleanup(ERR_NO_GLYPHS);

    /* allocate array of glyphs to keep */
    puchKeepGlyphList = (uint8 *)Mem_Alloc(usGlyphListCount * sizeof(uint8));
    if (puchKeepGlyphList == NULL)
        return ExitCleanup(ERR_MEM);

    /* read list of char codes from input list. Enter intersection of list and specified cmap into pulKeepCharCodeList. */
    if ((errCode = MakeKeepGlyphList((TTFACC_FILEBUFFERINFO *)&InputBufferInfo, usListType, usPlatform, usEncoding, pulKeepCharCodeList, usListCount, 
            puchKeepGlyphList, usGlyphListCount, &usMaxGlyphIndexUsed, &usGlyphKeepCount, TRUE)) != NO_ERROR)
    {
        Mem_Free(puchKeepGlyphList);
        return ExitCleanup(errCode); 
    }
    /* You could calculate your DSIG table anytime now */
    /* and while you're at it why don't you calculate a size delta if it will */
    /* grow or shrink from its original size */

    if (*ppuchDestBuffer == NULL || *pulDestBufferSize == 0) /* need to allocate some memory */
    {
        CalcOutputBufferSize(&InputBufferInfo, usGlyphListCount, usGlyphKeepCount, usFormat, ulSrcBufferSize, pulDestBufferSize);
#ifdef _DEBUG
#if !defined(ARGITERATOR_SUPPORTED) || (defined(ARGITERATOR_SUPPORTED) && ARGITERATOR_SUPPORTED)
		printf("Allocating %lu bytes for output buffer.\n", *pulDestBufferSize);
#endif
#endif
        /* Before the allocation is done, why don't you update *pulDestBufferSize with */
        /* info from your DSIG table calculation */
        *ppuchDestBuffer = (uint8 *)lpfnReAllocate(NULL, *pulDestBufferSize);
        if (*ppuchDestBuffer == NULL)
        {
            errCode = ERR_MEM;
            Mem_Free(puchKeepGlyphList);
            return ExitCleanup(errCode);
        }
    }

    OutputBufferInfo.puchBuffer = *ppuchDestBuffer;
    OutputBufferInfo.ulBufferSize = *pulDestBufferSize;
    OutputBufferInfo.ulOffsetTableOffset = 0;
    OutputBufferInfo.lpfnReAllocate = lpfnReAllocate;  /* for reallocation */

    // If OutputBufferInfo.puchBuffer goes through a realloc call that moves it, the original buffer pointed to by
    // *ppuchDestBuffer will be de-allocated.  If there is then an error condition in the call-chain, we can end up
    // returning a pointer to the de-allocated buffer.  Callers may then double free the buffer as they are none the wiser.
    // Setting *ppuchDestBuffer to NULL allows us to return NULL in the error case and the non-error case still works as before.
    *ppuchDestBuffer = NULL;

    if (usFormat == TTFDELTA_SUBSET1 || usFormat == TTFDELTA_DELTA)   /* if we will be trying to compact the font */
        usDttfGlyphIndexCount = usGlyphKeepCount;
    
    /* now call routines to modify each of the tables we care about. */
    /* ModMaxp must happen after ModGlyfLoca */
    /* must be called first followed by ModCmap. Then other tables may be processed in any order */
    /* lcp 4-10-97 output table order change to optimized rasterizer table access - order found in */
    /* ttfmerge.c g_DirOptimizeTagArray */
    /* modify glyph and loca before maxp, */
    /* modify hmtx before hdmx */
    /* modify cmap before os2 */
    
    while (1)   /* while loop used for handy break out */
    {   
        /* need to copy over directories for and make room for dttf table  */
        /* keep syncronised with calculations above */
        if (errCode = CopyOffsetDirectoryTables(&InputBufferInfo, &OutputBufferInfo, usFormat, &ulNewOutOffset)) break;  /* sets pulNewOutOffset */
        /* this resulting font will have all the other tables and directory entries for the missing */
        /* tables with 0 length entries */ 
        /* now copy some static tables over to reserve space for them in the font */
        /* this is to conform to the table order for font access optimizations */
        if ((errCode = CopyTableOver( &OutputBufferInfo, &InputBufferInfo, HEAD_TAG, &ulNewOutOffset )) != NO_ERROR)
            break;
        if ((errCode = CopyTableOver( &OutputBufferInfo, &InputBufferInfo, HHEA_TAG, &ulNewOutOffset )) != NO_ERROR)
            break;
        if ((errCode = CopyTableOver( &OutputBufferInfo, &InputBufferInfo, MAXP_TAG, &ulNewOutOffset )) != NO_ERROR)
            break;
        /* don't care if these next tables aren't there */
        if (usFormat != TTFDELTA_DELTA)
        {
            CopyTableOver( &OutputBufferInfo, &InputBufferInfo, OS2_TAG, &ulNewOutOffset );
        }
        /* shorten hhea.numLongHorMetrics if possible. zero out unused entries */
        if (errCode = ModXmtxXhea(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, usDttfGlyphIndexCount, usMaxGlyphIndexUsed, TRUE, &ulNewOutOffset))
        {
            if (errCode == ERR_WOULD_GROW)
                Mod_HDMX = FALSE;       /* turn off this flag */
            else
                break;
        }
        /* set to 0 any entries that have been removed */
        if (errCode = ModLTSH(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, usDttfGlyphIndexCount, &ulNewOutOffset)) break;
        /* remove 4:3 ratio and 0:0 ratio (if a 1:1 already exists) */
        if (errCode = ModVDMX(&InputBufferInfo, &OutputBufferInfo, usFormat, &ulNewOutOffset)) break;
        /* set to 0 any entries that have been removed */
        if (Mod_HDMX == TRUE)   /* don't mod if hmtx was left alone */
        {
            if (errCode = ModHdmx(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, usDttfGlyphIndexCount, &ulNewOutOffset)) break;
        }
        else
            CopyTableOver( &OutputBufferInfo, &InputBufferInfo, HDMX_TAG, &ulNewOutOffset );

        /* update the Cmap to reflect changed glyph list. fragmented cmap subtables may grow */
        if (errCode = ModCmap(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, &OS2MinChr, &OS2MaxChr, &ulNewOutOffset)) break;  

        if (usFormat != TTFDELTA_DELTA)
        {
            CopyTableOver( &OutputBufferInfo, &InputBufferInfo, FPGM_TAG, &ulNewOutOffset );
            CopyTableOver( &OutputBufferInfo, &InputBufferInfo, PREP_TAG, &ulNewOutOffset );
            CopyTableOver( &OutputBufferInfo, &InputBufferInfo, CVT_TAG, &ulNewOutOffset );
        }
        /* may delete cvt, prep and fpgm if there are no instructions in glyf table */      
        /* copy up any glyphs that are to be kept, squeezing out unused glyphs - adds to &ulNewOutOffset */
        /* will copy over glyf, loca and head tables */
        /* Updates bounding box and clears file checksum */
        if (errCode = ModGlyfLocaAndHead(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount,  &checkSumAdjustment, &ulNewOutOffset)) break;
        /* glyph related maximums: contours, num glyphs... */
        if (errCode = ModMaxP(&InputBufferInfo, &OutputBufferInfo, &ulNewOutOffset)) break;
        /* metric related maximums (except bounding box);  */
        if (errCode = ModOS2(&InputBufferInfo, &OutputBufferInfo, OS2MinChr, OS2MaxChr, usFormat, &ulNewOutOffset)) break; 
        /* Modify Embedded bitmap tables - EBLC, EBDT, EBSC, as well as bloc, bdat, bsca */
        /* for Subset format remove any pairs where a member has been removed */
        /* for subset 1, copy entire table, not subset */
        /* for Delta format, don't copy table */
        if (errCode = ModKern(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, usFormat, &ulNewOutOffset)) break;
        /* remove any MS platform name entries that are not usLanguage */
        /* will optimize the table format - share strings */
        if (errCode = ModName(&InputBufferInfo, &OutputBufferInfo, usLanguage, usFormat, &ulNewOutOffset)) break;
        /* change to format 3.0 if not already */
        if (errCode = ModPost(&InputBufferInfo, &OutputBufferInfo, usFormat, &ulNewOutOffset)) break;
        CopyTableOver( &OutputBufferInfo, &InputBufferInfo, GASP_TAG, &ulNewOutOffset );
        CopyTableOver( &OutputBufferInfo, &InputBufferInfo, PCLT_TAG, &ulNewOutOffset );
        CopyTableOver( &OutputBufferInfo, &InputBufferInfo, VHEA_TAG, &ulNewOutOffset );
        /* shorten vhea.numLongVerMetrics if possible. zero out unused entries */
        if (errCode = ModXmtxXhea(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, usDttfGlyphIndexCount, usMaxGlyphIndexUsed, FALSE, &ulNewOutOffset))
            if (errCode != ERR_WOULD_GROW) /* the error we can live with, go on ahead */
                break;
        if (errCode = ModSbit(&InputBufferInfo, &OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, &ulNewOutOffset)) break;
        break;
    }

    if (errCode == NO_ERROR)
    {
        if (usDttfGlyphIndexCount)   /* Subset1 and Delta we will be trying to compact the font */
        {
            errCode = CompactMaxpLocaTable(&OutputBufferInfo, puchKeepGlyphList, usGlyphListCount, usDttfGlyphIndexCount); 
            if (errCode == NO_ERROR)
            {
                /* now we need to allocate an array to keep a list of the actual glyphs we are keeping in the font */
                pusGlyphIndexArray = (uint16 *)Mem_Alloc(usDttfGlyphIndexCount * sizeof(*pusGlyphIndexArray)); /* big as we would ever need */
                if (pusGlyphIndexArray == NULL)
                    errCode = ERR_MEM; 
                else
                {
                    FillGlyphIndexArray(puchKeepGlyphList, usGlyphListCount, pusGlyphIndexArray, usDttfGlyphIndexCount);
                    /* update dttf table with glyph list */
                    errCode = UpdatePrivateTable(&OutputBufferInfo, &ulNewOutOffset, pusGlyphIndexArray, usDttfGlyphIndexCount, usGlyphListCount, usFormat, checkSumAdjustment);
                    Mem_Free(pusGlyphIndexArray);
                }
            }
        }
        /* If a DSIG table were to be added, this might be a good time */

        if (errCode == NO_ERROR) /* for Subset and Subset1, copy any other unknown tables */
            errCode = CopyForgottenTables(&InputBufferInfo, &OutputBufferInfo, &ulNewOutOffset);
        /* now, squeeze out any data in file buffer that is no longer referenced */
        if (errCode == NO_ERROR)
            errCode = CompressTables(&OutputBufferInfo, &ulNewOutOffset );
        if (errCode == NO_ERROR)
            SetFileChecksum(&OutputBufferInfo, ulNewOutOffset);  /* include dttf directory */
    }

    /* free up memory used here */
    Mem_Free(puchKeepGlyphList);
    /* reset these in case they changed */
    if (errCode == NO_ERROR && ulNewOutOffset > ulSrcBufferSize) /* if the font grew!!! (because of format fixes, or fragmentation) */
        errCode = ERR_WOULD_GROW;   /* use the original font */
    if (errCode == NO_ERROR)
    {
        *ppuchDestBuffer = OutputBufferInfo.puchBuffer;
        *pulDestBufferSize = OutputBufferInfo.ulBufferSize;
        *pulBytesWritten = ulNewOutOffset;
    }
    else  /* lcp free this up on error, if we allocated it in here */
    {
        if (*ppuchDestBuffer == NULL && lpfnFree != NULL)  /* if we allocated it here */
            lpfnFree(OutputBufferInfo.puchBuffer);
    }

    return ExitCleanup(errCode);
}
