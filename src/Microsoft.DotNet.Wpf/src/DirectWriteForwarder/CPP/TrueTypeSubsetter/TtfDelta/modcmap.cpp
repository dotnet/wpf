// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: modcmap.C
 *
 *
 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */
#include <string.h>     /* for memcpy */
#include <stdlib.h> /* for qsort */

#include "typedefs.h"
#include "ttff.h"
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "ttftabl1.h"
#include "ttftable.h"
#include "ttmem.h"
#include "util.h"
#include "modcmap.h"
#include "ttferror.h"     /* for error codes */

/* Definitions local to this file ---------------------------------------- */
/* ------------------------------------------------------------------- */
typedef struct {
    uint32 ulOldCmapOffset;
    uint32 ulNewCmapOffset;
} CmapOffsetRecord;

/* ------------------------------------------------------------------- */
typedef struct cmapoffsetrecordkeeper *PCMAPOFFSETRECORDKEEPER;     
typedef struct cmapoffsetrecordkeeper CMAPOFFSETRECORDKEEPER;     

struct cmapoffsetrecordkeeper      /* housekeeping structure */
{
    __field_ecount(usCmapOffsetArrayLen) CmapOffsetRecord * pCmapOffsetArray;
    uint16 usCmapOffsetArrayLen;
    uint16 usNextArrayIndex;
};

/* ------------------------------------------------------------------- */
PRIVATE int16 InitCmapOffsetArray(PCMAPOFFSETRECORDKEEPER pKeeper, 
                                  uint16 usRecordCount)
{
    pKeeper->pCmapOffsetArray = (CmapOffsetRecord *) Mem_Alloc(usRecordCount * sizeof(*(pKeeper->pCmapOffsetArray)));
    if (pKeeper->pCmapOffsetArray == NULL)
        return ERR_MEM;
    pKeeper->usCmapOffsetArrayLen = usRecordCount;
    pKeeper->usNextArrayIndex = 0;
    return NO_ERROR;
}
/* ------------------------------------------------------------------- */
PRIVATE void FreeCmapOffsetArray(PCMAPOFFSETRECORDKEEPER pKeeper)
{
    Mem_Free(pKeeper->pCmapOffsetArray);
    pKeeper->pCmapOffsetArray = NULL;
    pKeeper->usCmapOffsetArrayLen = 0;
    pKeeper->usNextArrayIndex = 0;
}
/* ------------------------------------------------------------------- */
PRIVATE int16 RecordCmapOffset(PCMAPOFFSETRECORDKEEPER pKeeper, 
                                uint32 ulOldCmapOffset,
                                uint32 ulNewCmapOffset)
  /* record this block as being used */
{
    if (pKeeper->usNextArrayIndex >= pKeeper->usCmapOffsetArrayLen)
        return ERR_INVALID_CMAP;
    pKeeper->pCmapOffsetArray[pKeeper->usNextArrayIndex].ulOldCmapOffset = ulOldCmapOffset;
    pKeeper->pCmapOffsetArray[pKeeper->usNextArrayIndex].ulNewCmapOffset = ulNewCmapOffset ;
    ++pKeeper->usNextArrayIndex;
    return NO_ERROR;
}

/* ------------------------------------------------------------------- */
PRIVATE uint32 LookupCmapOffset(PCMAPOFFSETRECORDKEEPER pKeeper, 
                                uint32 ulOldCmapOffset)
{
uint16 i;

    for (i = 0; i < pKeeper->usNextArrayIndex; ++i)
    {
         if (ulOldCmapOffset == pKeeper->pCmapOffsetArray[i].ulOldCmapOffset)
            return(pKeeper->pCmapOffsetArray[i].ulNewCmapOffset);
    }
    return(0L);
}

/* ------------------------------------------------------------------- */
/* ------------------------------------------------------------------- */
typedef struct {  /* used to sort and keep track of new offsets */
    uint16 usIndex;    /* index into the CMAP_TABLELOC array read from the original font */
    uint32 ulNewOffset;
} IndexOffset;

/* ------------------------------------------------------------------- */
/* Must sort subtables by offset, so that their data blocks may be moved in order */
/* output of this function is the IndexOffset array */
/* ------------------------------------------------------------------- */
PRIVATE void SortCmapSubByOffset(CMAP_TABLELOC *pCmapTableLoc, uint16 usSubTableCount, IndexOffset *pIndexArray)
{
uint16 i, j, k;

    for (i = 0; i < usSubTableCount; ++i)
    {
        for (j = 0; j < i; ++j)    /* look for where to insert this index in the Index array thingum */
        {
            if (pCmapTableLoc[i].offset < pCmapTableLoc[pIndexArray[j].usIndex].offset) /* need to insert it here */
            {  /* push down any that are ahead of this one */
                for (k = i; k > j; -- k)
                {
                      pIndexArray[k].usIndex = pIndexArray[k-1].usIndex;
                }
                break;
            }
        }
        pIndexArray[j].usIndex = i;      /* insert it */
    }
} 

/* ------------------------------------------------------------------- */
/* now compress out extra space between subtables */
/* when subtables are updated, they become smaller (or remain the same) */
/* what is left are shortened subtables in their original positions */
/* this function compresses them into one contiguous block of data */
/* once the subtables are moved, their new offsets must be written to the */
/* CMAP_TABLELOC array */
/* lcp change long word pad between subtables to short word pad. Caused tables */
/* to grow unnecessarily */
/* ------------------------------------------------------------------- */
PRIVATE int16 CompressCmapSubTables(TTFACC_FILEBUFFERINFO * pOutputBufferInfo,  /* ttfacc info */
                    CMAP_TABLELOC *pCmapTableLoc, /* array of CmapSubTable locators */
                    uint16 usSubTableCount, /* count of that array */
                    uint32 ulCmapOffset, /* offset to cmap table */
                    uint32 ulSubTableOffset, /* offset to beginning of Subtables */
                    uint32 ulCmapOldLength,    /* length of old cmap table - not to be exceeded */
                    uint32 *pulCmapNewLength)
{
IndexOffset *pIndexArray;  /* local array of structures to keep track of new offsets of sorted subtables */
CMAP_SUBHEADER_GEN CmapSubHeader;    
int16 errCode = NO_ERROR;
uint32 ulCurrentOffset;
uint32 ulLastOffset;
uint16 usIndex;
uint32 ulCmapTableLength = 0;
uint32 ulCmapSubTableDirOffset;
uint32 ulPadOffset;
uint16 i,j;
uint16 usBytesRead;
uint16 usPadBytes;

    pIndexArray = (IndexOffset *) Mem_Alloc(usSubTableCount * sizeof(*pIndexArray));
    if (pIndexArray == NULL)
        return ERR_MEM;
    
    /* sort them by old offsets, so we can move the blocks in order */
    SortCmapSubByOffset(pCmapTableLoc, usSubTableCount, pIndexArray); 

    ulCurrentOffset = ulSubTableOffset; /* end of the Cmap Directories */
    ulLastOffset = 0;
    for (i = 0; i < usSubTableCount; ++i)    /* process each subtable */
    {
        usIndex = pIndexArray[i].usIndex;
        /* check to see if this offset is the same as the last one copied. If so, ignore, as it has already been copied */
        if (i > 0 && pCmapTableLoc[usIndex].offset == ulLastOffset) /* we're pointing to some already copied data */
        {
            pIndexArray[i].ulNewOffset = pIndexArray[i-1].ulNewOffset;
            continue;
        }
        /* read the CmapSub Header */
         if ((errCode = ReadCmapLength(pOutputBufferInfo, &CmapSubHeader, ulCmapOffset + pCmapTableLoc[usIndex].offset, &usBytesRead)) != NO_ERROR)
            break;
        /* do we need to pad? */

        ulPadOffset = ulCurrentOffset;
        ulCurrentOffset = (ulPadOffset + 1) & ~1;      /* we may need to pad, but do it after we move data in case we would overwrite data */
        usPadBytes = (uint16) (ulCurrentOffset - ulPadOffset);

        if (ulCmapTableLength + usPadBytes + CmapSubHeader.length > ulCmapOldLength) /* if we are about to exceed the bounds */
        {
            errCode = ERR_WOULD_GROW;  /* can't do it. Bail and restore the old cmap table */
            break;
        }
        pIndexArray[i].ulNewOffset = ulCurrentOffset-ulCmapOffset;    /* calculate the new offset of the cmap subtable, and store in local structure */
        ulLastOffset = pCmapTableLoc[usIndex].offset;
        /* now copy the subtable to it's new locations */
        if ((errCode = CopyBlock(pOutputBufferInfo, ulCurrentOffset, ulCmapOffset + pCmapTableLoc[usIndex].offset,CmapSubHeader.length)) != NO_ERROR)
            break;
        for (j = 0; j < usPadBytes; ++j)
            WriteByte(pOutputBufferInfo,(uint8) 0, ulPadOffset+j);     /* now clear out those pad bytes */

        ulCurrentOffset += CmapSubHeader.length;
        ulCmapTableLength = ulCurrentOffset - ulCmapOffset;  /* to update the Font Directory values */
    } 
    if (errCode == NO_ERROR)
    {
        for (i = 0; i < usSubTableCount; ++i) /* now set the new offsets - retrieved from the local structure array */
            pCmapTableLoc[pIndexArray[i].usIndex].offset = pIndexArray[i].ulNewOffset;

        ulCmapSubTableDirOffset = ulCmapOffset + GetGenericSize( CMAP_HEADER_CONTROL );
        for (i = 0; i < usSubTableCount; ++i) /* now write the new offsets in their original order (Plat/encoding order) */
        {
              if ((errCode = WriteGeneric(pOutputBufferInfo, (uint8 *) &(pCmapTableLoc[i]), SIZEOF_CMAP_TABLELOC, CMAP_TABLELOC_CONTROL, ulCmapSubTableDirOffset, &usBytesRead)) != NO_ERROR)
                break; 
            ulCmapSubTableDirOffset += usBytesRead;  /* for next time around */
        }
    }
    Mem_Free(pIndexArray);
    if (errCode == NO_ERROR)
        /* now update the Directory Entry for the file */
        errCode = UpdateDirEntry(pOutputBufferInfo, CMAP_TAG, ulCmapTableLength);

    *pulCmapNewLength = ulCmapTableLength;

    return errCode;
}
/* ------------------------------------------------------------------- */
PRIVATE uint16 GetCmapSubtableCount( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
uint32 ulCmapOffset)
{
CMAP_HEADER CmapHdr;
uint16 usBytesRead;

    if (ReadGeneric( pInputBufferInfo, (uint8 *) &CmapHdr, SIZEOF_CMAP_HEADER, CMAP_HEADER_CONTROL, ulCmapOffset, &usBytesRead ) != NO_ERROR)
        return 0;

    return(CmapHdr.numTables);
} /* GetCmapSubtableCount() */
/* ------------------------------------------------------------------- */
/* modify format 0 cmap subtable - remove references to glyphs no longer with us */
   /* this routine modifies the apple cmap table so that characters 
      referencing deleted glyphs are mapped to the missing character. */ 
/* ------------------------------------------------------------------- */
PRIVATE int16 ModMacStandardCmap( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint32 ulOffset, uint8 *puchKeepGlyphList, uint16 usGlyphCount )
{
uint16 i;
uint8 GlyphIndex;
int16 errCode;

    for ( i = 0; i < CMAP_FORMAT0_ARRAYCOUNT; i++ )
    {
        if ((errCode = ReadByte(pOutputBufferInfo, &GlyphIndex, ulOffset)) != NO_ERROR)
            return errCode;
        if (GlyphIndex >= usGlyphCount || puchKeepGlyphList[GlyphIndex] == 0) /* not a glyph to be used */
        {
            if ((errCode = WriteByte(pOutputBufferInfo, (uint8) 0, ulOffset)) != NO_ERROR)
                return errCode;
        }
        ulOffset += sizeof(GlyphIndex);
    }
    return NO_ERROR;
}
/* ------------------------------------------------------------------- */
/* modify format 6 cmap subtable - remove references to glyphs no longer with us
   this routine modifies the apple cmap table so that characters 
   referencing deleted glphs are mapped to the missing character. It will also shorten
   the table if possible. */ 
/* ------------------------------------------------------------------- */
PRIVATE int16 ModMacTrimmedCmap( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                               uint32 ulOffset, 
                               uint8 *puchKeepGlyphList, 
                               uint16 usGlyphCount )
{
uint16 i;
uint16 GlyphIndex;
CMAP_FORMAT6 CmapFormat6;
int16 errCode;
uint16 usBytesRead;
uint16 usBytesWritten;
uint16 usNewFirstCode= 0xFFFF;
uint16 usNewLastCode=0;  /* saved to calc NewEntryCount */
uint32 ulInGlyphOffset;  /* to point to a glyph value to read */
uint32 ulOutGlyphOffset;  /* to point to a glyph value to write */


    if ((errCode = ReadGeneric(pOutputBufferInfo, (uint8 *)&CmapFormat6, SIZEOF_CMAP_FORMAT6, CMAP_FORMAT6_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
        return errCode; 
    ulInGlyphOffset = ulOutGlyphOffset = ulOffset + usBytesRead;
    /* first figure out where the start and end are */
    for ( i = 0; i < CmapFormat6.entryCount; i++ )
    {
        if ((errCode = ReadWord(pOutputBufferInfo, &GlyphIndex, ulInGlyphOffset)) != NO_ERROR)
            return errCode;
        if (GlyphIndex < usGlyphCount && puchKeepGlyphList[GlyphIndex] != 0) /* a glyph to be used */
        {
            if (usNewFirstCode == 0xFFFF)  /* default first code, set only if hasn't been set already */
                usNewFirstCode = CmapFormat6.firstCode + i;    /* may be zero */
            usNewLastCode = CmapFormat6.firstCode+i;
        }
        ulInGlyphOffset += sizeof(GlyphIndex);
    }
    if (usNewFirstCode == 0xFFFF) /* none were found */
    {
        CmapFormat6.firstCode = 0;
        CmapFormat6.entryCount = 0; 
    }
    else
    {
        /* now calculate the new table */
        CmapFormat6.firstCode = usNewFirstCode;
        CmapFormat6.entryCount = usNewLastCode - usNewFirstCode+1;
        
        ulInGlyphOffset = ulOutGlyphOffset + usNewFirstCode * sizeof(GlyphIndex); /* where to read the first code */
        for ( i = usNewFirstCode; i <= usNewLastCode; i++ )
        {
            if ((errCode = ReadWord(pOutputBufferInfo, &GlyphIndex, ulInGlyphOffset)) != NO_ERROR)
                return errCode;
            if (GlyphIndex >= usGlyphCount || puchKeepGlyphList[GlyphIndex] == 0) /* not a glyph to be used */
            {
                if ((errCode = WriteWord(pOutputBufferInfo, (uint16) 0, ulOutGlyphOffset)) != NO_ERROR)
                    return errCode;
            }
            else  /* write the glyph Index to the new location */
            {
                if ((errCode = WriteWord(pOutputBufferInfo, GlyphIndex, ulOutGlyphOffset)) != NO_ERROR)
                    return errCode;
            }
            ulInGlyphOffset += sizeof(GlyphIndex);
            ulOutGlyphOffset += sizeof(GlyphIndex);
        }
    }
    CmapFormat6.length = (uint16) (ulOutGlyphOffset - ulOffset);
    /* write out new cmap subtable header */
    if ((errCode = WriteGeneric(pOutputBufferInfo, (uint8 *)&CmapFormat6, SIZEOF_CMAP_FORMAT6, CMAP_FORMAT6_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
        return errCode; 

    return NO_ERROR;
}
/* ------------------------------------------------------------------- */
/* ENTRY POINT !!!                                                        */
/* This function will modify the CMAP tables Thusly:                    */
/* It will go through the list of Cmap Subtables. If it finds a format    */
/* 4 table, it will try to update it based on the list of glyph        */
/* codes to keep.                                                        */
/* if it finds a MAC cmap (format 0, or 6) it will set to zero any        */
/* glyphs referenced that have been removed using the puchKeepGlyphList */
/* if the resulting CMAP table will be larger than the original, then    */
/* the cmap will be restored the original                                */
/* ------------------------------------------------------------------- */
int16 ModCmap(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
                uint8 *puchKeepGlyphList, /* glyphs to keep - boolean */
                uint16 usGlyphCount,  /* count of puchKeepGlyphList */
                uint16 * pOS2MinChr,     /* for setting in the OS/2 table */
                uint16 * pOS2MaxChr,     /* for setting in the OS/2 table */
                uint32 *pulNewOutOffset)
{
FORMAT4_SEGMENTS *NewFormat4Segments = NULL;   /* used to create a Segments    array for format 4 subtables */
FORMAT12_GROUPS     *NewFormat12Groups = NULL;    
uint16 usnSegment;
GLYPH_ID *NewFormat4GlyphIdArray = NULL; /* used to create a GlyphID array for format 4 subtables */
uint16 snFormat4GlyphIdArray;
CMAP_FORMAT4 CmapFormat4;
CMAP_FORMAT12 CmapFormat12;
CMAP_TABLELOC *pCmapTableLoc=NULL;
uint16 usSubTableCount;
CMAP_SUBHEADER_GEN CmapSubHeader;
PCHAR_GLYPH_MAP_LIST pCharGlyphMapList = NULL;    /* sorted list of character codes to keep and their glyph indices */
uint16 usnCharGlyphMapListCount= 0;    /* length of pCharGlyphMapList array */
PCHAR_GLYPH_MAP_LIST_EX pCharGlyphMapListEx = NULL;    /* sorted list of character codes to keep and their glyph indices */
uint32 ulnCharGlyphMapListCount= 0;                    /* length of pCharGlyphMapListEx array */
CMAPOFFSETRECORDKEEPER CmapSubtableKeeper;
uint32 ulCmapOffset;
uint32 ulCmapLength;
uint32 ulCmapNewLength;
uint32 ulCmapSubTableDirOffset;
uint32 ulCmapSubtableNewOffset;
uint32 ulBytesWritten;
uint16 i;
int16 errCode= NO_ERROR;
uint16 usBytesRead;

    if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, CMAP_TAG, pulNewOutOffset)) != NO_ERROR)
        return errCode;

    ulCmapOffset = TTTableOffset( pOutputBufferInfo, CMAP_TAG );
    ulCmapLength = TTTableLength( pOutputBufferInfo, CMAP_TAG);
    *pOS2MinChr = 0;
    *pOS2MaxChr = 0;
    if (ulCmapOffset == 0L || ulCmapLength == 0L)
        return ERR_INVALID_CMAP;  /* huh?*/

    usSubTableCount = GetCmapSubtableCount(pOutputBufferInfo, ulCmapOffset);
    pCmapTableLoc = (CMAP_TABLELOC *)Mem_Alloc(SIZEOF_CMAP_TABLELOC * usSubTableCount);
    if (pCmapTableLoc == NULL)
        return ERR_MEM;
    ulCmapSubTableDirOffset  = ulCmapOffset + GetGenericSize( CMAP_HEADER_CONTROL );

    if (InitCmapOffsetArray(&CmapSubtableKeeper, usSubTableCount) != NO_ERROR)
    {
        return ERR_MEM;
    }
    
    for (i = 0; i < usSubTableCount; ++i)
    {
        /* read the cmap directory entry */
         if ((errCode = ReadGeneric(pOutputBufferInfo, (uint8 *) &(pCmapTableLoc[i]), SIZEOF_CMAP_TABLELOC, CMAP_TABLELOC_CONTROL, ulCmapSubTableDirOffset, &usBytesRead)) != NO_ERROR)
            break; 
        ulCmapSubTableDirOffset += usBytesRead;  /* for next time around */

        /* Check to see if this subtable is shared, and has been modified already */
        if ((ulCmapSubtableNewOffset = LookupCmapOffset(&CmapSubtableKeeper, pCmapTableLoc[i].offset)) != 0)
        {
            pCmapTableLoc[i].offset = ulCmapSubtableNewOffset;
            continue;
        }
        /* now read the CmapSub Header, to determine the format */
         if ((errCode = ReadCmapLength(pOutputBufferInfo, &CmapSubHeader, ulCmapOffset + pCmapTableLoc[i].offset, &usBytesRead)) != NO_ERROR)
            break; 

        /* Will subset: Format 0, Format 4 ,Format 6 and Format 12 Cmap Subtables */
        /* Otherwise, leave them alone */

        if (CmapSubHeader.format == FORMAT0_CMAP_FORMAT)
        {
            if ((errCode = ModMacStandardCmap(pOutputBufferInfo, ulCmapOffset + pCmapTableLoc[i].offset + usBytesRead, puchKeepGlyphList, usGlyphCount)) != NO_ERROR)
                break;
        }
        else if (CmapSubHeader.format == FORMAT6_CMAP_FORMAT)
        {
            if ((errCode = ModMacTrimmedCmap(pOutputBufferInfo, ulCmapOffset + pCmapTableLoc[i].offset, puchKeepGlyphList, usGlyphCount)) != NO_ERROR)
                break;
        }
        else if (CmapSubHeader.format == FORMAT4_CMAP_FORMAT)
        {
        
            /* process Format 4 Cmap Subtable */
             /*need to come up with a CharCodeList, from the puchKeepGlyphList  */
            errCode = ReadAllocFormat4CharGlyphMapList(pOutputBufferInfo, pCmapTableLoc[i].platformID, pCmapTableLoc[i].encodingID, puchKeepGlyphList, usGlyphCount, &pCharGlyphMapList, &usnCharGlyphMapListCount); 

            if (errCode != NO_ERROR)
                break;

            NewFormat4Segments = (FORMAT4_SEGMENTS *) Mem_Alloc( (usnCharGlyphMapListCount+1) * SIZEOF_FORMAT4_SEGMENTS ); /* add one for the extra dummy segment */
            NewFormat4GlyphIdArray = (GLYPH_ID *) Mem_Alloc( usnCharGlyphMapListCount * sizeof( *NewFormat4GlyphIdArray ) );

            if ( NewFormat4Segments == NULL || NewFormat4GlyphIdArray == NULL )
            {
                errCode = ERR_MEM;
                break;
            }

            /* compute new format 4 data */

            ComputeFormat4CmapData( &CmapFormat4, NewFormat4Segments, &usnSegment, 
                                    NewFormat4GlyphIdArray, &snFormat4GlyphIdArray, pCharGlyphMapList, usnCharGlyphMapListCount );


            /* Donald, if you don't care if the Cmap subtable grows, you could comment out the next line */
            
            if (CmapFormat4.length <= CmapSubHeader.length) /* if the new length is smaller than the old, we can write it in the old place */
            {
                if (pCmapTableLoc[i].platformID == MS_PLATFORMID)  /* only applies to this platform */
                {
                    *pOS2MinChr = pCharGlyphMapList[0].usCharCode;
                    *pOS2MaxChr = pCharGlyphMapList[usnCharGlyphMapListCount-1].usCharCode;
                }
                errCode = WriteOutFormat4CmapData( pOutputBufferInfo, &CmapFormat4, NewFormat4Segments, NewFormat4GlyphIdArray, usnSegment,
                             snFormat4GlyphIdArray, ulCmapOffset + pCmapTableLoc[i].offset, &ulBytesWritten );
            }
            /* else: leave cmap subtable alone */

            /* clean up */

            Mem_Free(NewFormat4Segments);
            Mem_Free(NewFormat4GlyphIdArray );
            FreeFormat4CharCodes(pCharGlyphMapList);
            
            NewFormat4Segments = NULL;
            NewFormat4GlyphIdArray = NULL;
            pCharGlyphMapList = NULL;
        }
        else if (CmapSubHeader.format == FORMAT12_CMAP_FORMAT)
        {
            uint32 ulnGroups = 0;

            /*need to come up with a CharCodeList, from the puchKeepGlyphList  */
            errCode = ReadAllocFormat12CharGlyphMapList(pOutputBufferInfo, ulCmapOffset + pCmapTableLoc[i].offset, puchKeepGlyphList, usGlyphCount, &pCharGlyphMapListEx, &ulnCharGlyphMapListCount); 
            if (errCode != NO_ERROR)
                break;

            NewFormat12Groups = (FORMAT12_GROUPS *) Mem_Alloc( (ulnCharGlyphMapListCount) * SIZEOF_FORMAT12_GROUPS );
            if ( NewFormat12Groups == NULL)
            {
                errCode = ERR_MEM;
                break;
            }

            /* compute new format 12 data */
            ComputeFormat12CmapData( &CmapFormat12, NewFormat12Groups, &ulnGroups, pCharGlyphMapListEx, ulnCharGlyphMapListCount );

            /* Donald, if you don't care if the Cmap subtable grows, you could comment out the next line */
            if (CmapFormat12.length <= CmapSubHeader.length) /* if the new length is smaller than the old, we can write it in the old place */
            {
                if (pCmapTableLoc[i].platformID == MS_PLATFORMID)  /* only applies to this platform */
                {
                    *pOS2MinChr = (uint16)pCharGlyphMapListEx[0].ulCharCode;
                    *pOS2MaxChr = (uint16)pCharGlyphMapListEx[ulnCharGlyphMapListCount-1].ulCharCode;
                }
                errCode = WriteOutFormat12CmapData( pOutputBufferInfo, &CmapFormat12, NewFormat12Groups, ulnGroups,
                             ulCmapOffset + pCmapTableLoc[i].offset, &ulBytesWritten );
            }
            /* else: leave cmap subtable alone */

            /* clean up */
            Mem_Free(NewFormat12Groups);
            FreeFormat12CharCodes(pCharGlyphMapListEx);
            
            NewFormat12Groups = NULL;
            pCharGlyphMapListEx = NULL;
        }
        RecordCmapOffset(&CmapSubtableKeeper, pCmapTableLoc[i].offset, pCmapTableLoc[i].offset); /* record the new offset (didn't change) */
    }
    /* now need to compress out empty bytes from ends of Cmap Subtables */
    if (errCode == NO_ERROR)
    {
        errCode = CompressCmapSubTables(pOutputBufferInfo, pCmapTableLoc, usSubTableCount, ulCmapOffset, ulCmapSubTableDirOffset, ulCmapLength, &ulCmapNewLength);
        *pulNewOutOffset = ulCmapOffset + ulCmapNewLength; /* hand back to caller */
    }
    else /* these weren't taken care of yet (necessarily) */
    {
        Mem_Free(NewFormat4Segments); /* may be non-null */
        Mem_Free(NewFormat4GlyphIdArray );
        FreeFormat4CharCodes(pCharGlyphMapList);
        if (errCode == ERR_WOULD_GROW)  /* fragmentation has caused a larger cmap table, copy table again */
        {
            *pulNewOutOffset = ulCmapOffset; /* reset */
            errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, CMAP_TAG, pulNewOutOffset);
        }
    }
    Mem_Free(pCmapTableLoc);
    FreeCmapOffsetArray(&CmapSubtableKeeper);

    return errCode;

} /* ModCmap() */

/* ------------------------------------------------------------------- */
