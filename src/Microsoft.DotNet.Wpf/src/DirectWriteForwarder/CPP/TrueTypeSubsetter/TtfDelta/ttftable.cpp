// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFTABLE.C
 *
 *
 * aRoutines to read true type tables and table information from 
 * a true type file buffer
 *
 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include "typedefs.h"
#include "ttff.h"                       /* true type font file def's */
#include "ttfacc.h"
#include "ttftable.h"
#include "ttftabl1.h"
#include "ttfcntrl.h"
#include "ttmem.h"
#include "util.h"
#include "ttfdelta.h" /* for Dont care info */
#include "ttferror.h"
#include "ttfdcnfg.h"

/* ---------------------------------------------------------------------- */
PRIVATE int CRTCB AscendingTagCompare( CONST void *arg1, CONST void *arg2 )
{
    if (((DIRECTORY *)(arg1))->tag == ((DIRECTORY *)(arg2))->tag) /* they're the same */
        return 0;
    if (((DIRECTORY *)(arg1))->tag < ((DIRECTORY *)(arg2))->tag)
        return -1;
    return 1;
}
/* ---------------------------------------------------------------------- */
PRIVATE int CRTCB AscendingOffsetCompare( CONST void *arg1, CONST void *arg2 )
{
    if (((DIRECTORY *)(arg1))->offset == ((DIRECTORY *)(arg2))->offset) /* they're the same */
        return 0;
    if (((DIRECTORY *)(arg1))->offset < ((DIRECTORY *)(arg2))->offset)
        return -1;
    return 1;
}

/* ---------------------------------------------------------------------- */
/* this routine sorts an array of directory entries by tag value using
  a qsort */
/* ---------------------------------------------------------------------- */
void SortByTag( DIRECTORY * aDirectory,
                uint16      usnDirs )
{
    if (aDirectory == NULL || usnDirs == 0)
        return;
    qsort (aDirectory, usnDirs, sizeof(*aDirectory),AscendingTagCompare); 
}

/* ---------------------------------------------------------------------- */
/* this routine sorts an array of directory entries by offset value using
  a qsort */
/* ---------------------------------------------------------------------- */
void SortByOffset( DIRECTORY * aDirectory,
                    uint16      usnDirs )
{
    if (aDirectory == NULL || usnDirs == 0)
        return;
    qsort (aDirectory, usnDirs, sizeof(*aDirectory),AscendingOffsetCompare); 
}


/* ---------------------------------------------------------------------- */
/* this routine marks a font file table for deletion.  To do so,
it sets the tag to something unrecognizable so it will be
filtered out by the compress tables operation at the end
of program execution. */
/* ---------------------------------------------------------------------- */
void MarkTableForDeletion( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, __in_bcount(4) const char *  szDirTag )
                   
{
DIRECTORY Directory;
uint32 ulOffset;
uint16 usBytesMoved;

    /* read existing directory entry */
    ulOffset = GetTTDirectory( pOutputBufferInfo, szDirTag, &Directory );
    if ( ulOffset == DIRECTORY_ERROR )
        return;
    
    /* set bad directory tag using an arbitrary, nonsensical value */

    Directory.tag = DELETETABLETAG;

    /* write new directory entry */
        if (WriteGeneric( pOutputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesMoved ) != NO_ERROR)
        {
            // We just readed from the very same place.
            assert(FALSE);
        };
    
} /* MarkTableForDeletion() */

/* ---------------------------------------------------------------------- */
uint32 FindCmapSubtable( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                        uint16   usDesiredPlatform,
                        uint16   usDesiredEncodingID, 
                        uint16 *pusFoundEncoding )
{
int16 i;
CMAP_HEADER CmapHeader;
CMAP_TABLELOC CmapTableLoc;
BOOL fFound;
int16 nCmapTables;
uint16 usBytesRead;
uint32 ulOffset;
uint32 ulCmapOffset;
uint32 ulFoundOffset;

   /* Read header of the 'cmap' table */

    if (!(ulCmapOffset = TTTableOffset( pOutputBufferInfo, CMAP_TAG )))
        return (0L);
    if (ReadGeneric( pOutputBufferInfo, (uint8 *) &CmapHeader, SIZEOF_CMAP_HEADER, CMAP_HEADER_CONTROL, ulCmapOffset, &usBytesRead) != NO_ERROR)
        return (0L);

   /* read directory entries to find the desired encoding table.
      The directory entries for subtables give the offset from
      the beginning of the 'cmap' table to where the desired table 
      begins. */

    fFound = FALSE;
    ulFoundOffset = 0;
    ulOffset = ulCmapOffset + usBytesRead;
    nCmapTables = CmapHeader.numTables;

    if (usDesiredPlatform == TTFSUB_MS_PLATFORMID && usDesiredEncodingID == TTFSUB_DONT_CARE)
    {
        for (i = 0; i < nCmapTables; ++i, ulOffset += usBytesRead)
        {
            if (ReadGeneric( pOutputBufferInfo, (uint8 *) &CmapTableLoc, SIZEOF_CMAP_TABLELOC, CMAP_TABLELOC_CONTROL, ulOffset, &usBytesRead) != NO_ERROR)
                return (0L);
            if ( CmapTableLoc.platformID == TTFSUB_MS_PLATFORMID )
            {
                if (CmapTableLoc.encodingID == TTFSUB_SURROGATE_CHAR_SET)
                {
                    ulFoundOffset = CmapTableLoc.offset;
                    *pusFoundEncoding = CmapTableLoc.encodingID;
                    fFound = TRUE;
                }
                else if (CmapTableLoc.encodingID == TTFSUB_UNICODE_CHAR_SET && (!fFound || *pusFoundEncoding!=TTFSUB_SURROGATE_CHAR_SET))
                {
                    ulFoundOffset = CmapTableLoc.offset;
                    *pusFoundEncoding = CmapTableLoc.encodingID;
                    fFound = TRUE;
                }
                else if (CmapTableLoc.encodingID == TTFSUB_SYMBOL_CHAR_SET && !fFound)
                {
                    ulFoundOffset = CmapTableLoc.offset;
                    *pusFoundEncoding = CmapTableLoc.encodingID;
                    fFound = TRUE;
                }
            }
        }
    }
    else
    {
        for (i = 0; i < nCmapTables && !fFound ; ++i, ulOffset += usBytesRead)
        {
            if (ReadGeneric( pOutputBufferInfo, (uint8 *) &CmapTableLoc, SIZEOF_CMAP_TABLELOC, CMAP_TABLELOC_CONTROL, ulOffset, &usBytesRead) != NO_ERROR)
                return (0L);
            if ( CmapTableLoc.platformID == usDesiredPlatform &&
                ( CmapTableLoc.encodingID == usDesiredEncodingID ||
                usDesiredEncodingID == TTFSUB_DONT_CARE ) )
            {
                ulFoundOffset = CmapTableLoc.offset;
                fFound = TRUE;
                *pusFoundEncoding = CmapTableLoc.encodingID;
            }
        } 
    }

    if ( fFound == FALSE )
        return( 0L );

    /* return address of cmap subtable relative to start of file */

    return( ulCmapOffset + ulFoundOffset );
   
} /* FindCmapSubtable() */

/* ---------------------------------------------------------------------- */
PRIVATE uint16 GuessNumCmapGlyphIds( uint16 usnSegments,
                             FORMAT4_SEGMENTS *  Format4Segments )
{
/* this routine guesses the approximate number of entries in the
    GlyphId array of the format 4 cmap table.  This guessing is
    necessary because there is nothing in the format 4 table that
    explicitly indicates the number of GlyphId entries, and it is
    not valid to assume any particular number, such as one based on
    the number of glyphs or the size of the format 4 cmap table. */

int32          sIdIdx;
uint16         usCharCode;
uint16         i;
uint16         usMaxGlyphIdIdx;

/* zip through cmap entries, checking each entry to see if it indexes
    into the GlyphId array.  If it does, determine the array index,
    and keep track of the maximum array index used.  The maximum used
    then becomes the guess as to the number of GlyphIds. */

    usMaxGlyphIdIdx = 0;
    for ( i = 0; i < usnSegments; i++ )
    {
        if ( Format4Segments[i].idRangeOffset == 0 )
            continue;

        for ( usCharCode = Format4Segments[ i ].startCount;
            usCharCode <= Format4Segments[ i ].endCount && Format4Segments[ i ].endCount != INVALID_CHAR_CODE;
            usCharCode++ )
        {
            sIdIdx  = (uint16) i - (uint16) usnSegments;
            sIdIdx += (uint16) (Format4Segments[i].idRangeOffset / 2) + usCharCode - Format4Segments[i].startCount;
            usMaxGlyphIdIdx = max( usMaxGlyphIdIdx, (uint16) (sIdIdx+1) );
        }
    }
    return( usMaxGlyphIdIdx );
}

 /* ---------------------------------------------------------------------- */
/* special case, need to read long or short repeatedly into long buffer */
/* buffer must have been allocated large enough for the number of glyphs */
uint32 GetLoca( TTFACC_FILEBUFFERINFO               *pInputBufferInfo, 
                     __out_ecount(ulAllocedCount) uint32 *pulLoca, 
                     __range(1, USHORT_MAX + 1)     uint32  ulAllocedCount
                  )
{
uint32 ulOffset = 0;
uint16 usOffset;
HEAD Head;
uint16 usIdxToLocFmt;
uint32 ulGlyphCount;
uint32 i;
uint32 ulBytesRead;

    if ( ! GetHead( pInputBufferInfo, &Head ))
        return( 0L );
    usIdxToLocFmt = Head.indexToLocFormat;

    ulGlyphCount = GetNumGlyphs(pInputBufferInfo );
    if (ulAllocedCount < ulGlyphCount + 1) /* not enough room to read this */
        return 0L;

    if (!(ulOffset = TTTableOffset( pInputBufferInfo, LOCA_TAG )))
        return 0L;

    if ( usIdxToLocFmt == SHORT_OFFSETS )
    {
        for (i = 0; i <= ulGlyphCount; ++i)
        {
            if (ReadWord( pInputBufferInfo, &usOffset, ulOffset + (i*sizeof(uint16))) != NO_ERROR)
                return 0L;
            pulLoca[i] = (int32) usOffset * 2L;
        }
    }
    else
    {
        if (ReadGenericRepeat(pInputBufferInfo, (uint8 *)pulLoca, LONG_CONTROL, ulOffset, &ulBytesRead, (uint16) (ulGlyphCount + 1), sizeof(uint32)) != NO_ERROR) 
            return 0L;
    }
    return( ulOffset );
}
PRIVATE int CRTCB CompareSegments(const void *elem1, const void *elem2)
{

    if ((((FORMAT4_SEGMENTS *)(elem1))->endCount <= ((FORMAT4_SEGMENTS *)(elem2))->endCount) /* it is within this range */
        && (((FORMAT4_SEGMENTS *)(elem1))->startCount >= ((FORMAT4_SEGMENTS *)(elem2))->startCount))
        return 0;
    if (((FORMAT4_SEGMENTS *)(elem1))->startCount < ((FORMAT4_SEGMENTS *)(elem2))->startCount)
        return -1;
    return 1;

}

/* ---------------------------------------------------------------------- */
uint16 GetGlyphIdx( uint16 usCharCode,
                    FORMAT4_SEGMENTS * Format4Segments,
                    uint16 usnSegments,
                    GLYPH_ID * GlyphId,
                    uint16 usnGlyphs)
{
uint16 usGlyphIdx;
int32 sIDIdx;
FORMAT4_SEGMENTS *pFormat4Segment;
FORMAT4_SEGMENTS KeySegment;

    KeySegment.startCount = usCharCode;
    KeySegment.endCount = usCharCode;
    /* find segment containing the character code */
    pFormat4Segment = (FORMAT4_SEGMENTS *)bsearch(&KeySegment, Format4Segments, usnSegments, sizeof(*Format4Segments), CompareSegments);

    if ( pFormat4Segment == NULL )
        return( INVALID_GLYPH_INDEX );

    /* calculate the glyph index */

    if ( pFormat4Segment->idRangeOffset == 0 )
        usGlyphIdx = usCharCode + pFormat4Segment->idDelta;
    else
    {
        sIDIdx = (int32)(pFormat4Segment - (Format4Segments + usnSegments));
        /* sIDIdx = (uint16) i - (uint16) usnSegments; */
        sIDIdx += (int32) (pFormat4Segment->idRangeOffset / 2) + usCharCode - pFormat4Segment->startCount;
        /* check against bounds */
        if (sIDIdx >= usnGlyphs)
            return INVALID_GLYPH_INDEX;
        usGlyphIdx = GlyphId[ sIDIdx ];
        if (usGlyphIdx)
            /* Only add in idDelta if we've really got a glyph! */
            usGlyphIdx = (uint16)(usGlyphIdx + pFormat4Segment->idDelta);
    }

    return( usGlyphIdx );
} /* GetGlyphIdx */


/* ---------------------------------------------------------------------- */
uint32 GetGlyphIdx12( uint32 ulCharCode,
                    FORMAT12_GROUPS * pFormat12Groups,
                    uint32 ulnGroups )
{
uint32 i;
uint32 ulGlyphIdx = INVALID_GLYPH_INDEX_LONG;


    /* should we do a binary search here, i.e. are the groups sorted? */
    for ( i=0; i < ulnGroups; i++)
    {
        if (pFormat12Groups[i].startCharCode <= ulCharCode &&
            pFormat12Groups[i].endCharCode >= ulCharCode)
            ulGlyphIdx = pFormat12Groups[i].startGlyphCode + (ulCharCode - pFormat12Groups[i].startCharCode); 
    }

    return( ulGlyphIdx );
} /* GetGlyphIdx12 */


/* ---------------------------------------------------------------------- */
void FreeCmapFormat4Ids( GLYPH_ID *GlyphId )
{
    Mem_Free( GlyphId );
}
/* ---------------------------------------------------------------------- */
void FreeCmapFormat4Segs( FORMAT4_SEGMENTS *Format4Segments)
{
    Mem_Free( Format4Segments );
}
/* ---------------------------------------------------------------------- */
void FreeCmapFormat4( FORMAT4_SEGMENTS *Format4Segments,
                 GLYPH_ID *GlyphId )
{
    FreeCmapFormat4Segs( Format4Segments );
    FreeCmapFormat4Ids( GlyphId );
}

/* ---------------------------------------------------------------------- */
int16 ReadAllocCmapFormat4Ids( TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint16 usSegCount,
                         FORMAT4_SEGMENTS * Format4Segments,
                         GLYPH_ID ** ppGlyphId,
                         uint16 * pusnIds,
                         uint32 ulOffset,
                         uint32 *pulBytesRead )
{
uint16 i;
int16 errCode;

    /* calc number of glyph indexes while making sure the start and end
    count numbers used to calc the number of indexes is reasonable */

    *ppGlyphId = NULL;
    for ( i=0; i < usSegCount; i++ )
    {                    
        /* check for reasonable start and end counts */
        if ( Format4Segments[i].endCount < Format4Segments[i].startCount )
            return(ERR_INVALID_CMAP);
    }

    /* set the return value for number of glyph Ids.  As of this writing
        (9/20/90), there was no reliable way to calculate the size of
        the ppGlyphId array, so here we just read a lot of values and assume
        that it will be enough. */

    *pusnIds = GuessNumCmapGlyphIds( usSegCount, Format4Segments );

    /* allocate memory for GlyphID array */

    if ( *pusnIds == 0 )
        return(NO_ERROR);

    *ppGlyphId = (GLYPH_ID *)Mem_Alloc(*pusnIds * sizeof( (*ppGlyphId)[0] ));
    if ( *ppGlyphId == NULL )
        return(ERR_MEM);

    /* read glyph index array */
    if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *) *ppGlyphId, WORD_CONTROL, ulOffset, pulBytesRead, *pusnIds, sizeof( (*ppGlyphId)[0] ))) != NO_ERROR)
    {
        Mem_Free(*ppGlyphId);
        *ppGlyphId = NULL;
        return (errCode);
    }
    return(NO_ERROR );

} /* ReadAllocCmapFormat4Ids() */

/* ---------------------------------------------------------------------- */
int16 ReadAllocCmapFormat4Segs( TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint16 usSegCount,
                          FORMAT4_SEGMENTS ** Format4Segments, 
                          uint32 ulOffset,
                          uint32 *pulBytesRead)
{
uint16 i;
uint16 usReservedPad;
uint16 usWordSize;
uint32 ulCurrentOffset = ulOffset;
int16 errCode;
uint32 ulBytesRead;

/* allocate memory for variable length part of table */

    *Format4Segments = (FORMAT4_SEGMENTS *)Mem_Alloc( usSegCount * SIZEOF_FORMAT4_SEGMENTS);
    if ( *Format4Segments == NULL )
        return( ERR_MEM );

    usWordSize = sizeof(uint16);

    // Check that we have no iteger overflow
    if (ulCurrentOffset + (4 * usSegCount + 1) * usWordSize < ulCurrentOffset)
    {
        return ERR_READOUTOFBOUNDS;
    }
    
    for ( i = 0; i < usSegCount; i++ )
        if ((errCode = ReadWord( pInputBufferInfo, &(*Format4Segments)[i].endCount, ulCurrentOffset+(i*usWordSize))) != NO_ERROR)
        {
            Mem_Free( *Format4Segments);
            *Format4Segments = NULL;
            return errCode; 
        }
    ulCurrentOffset += usSegCount * usWordSize;

    if ((errCode = ReadWord( pInputBufferInfo,  &usReservedPad,ulCurrentOffset)) != NO_ERROR)
    {
        Mem_Free( *Format4Segments);
        *Format4Segments = NULL;
        return errCode;
    }
    ulCurrentOffset += usWordSize;

    for ( i = 0; i < usSegCount; i++ )
        if ((errCode = ReadWord( pInputBufferInfo,  &(*Format4Segments)[i].startCount, ulCurrentOffset+(i*usWordSize))) != NO_ERROR)
        {
            Mem_Free( *Format4Segments);
            *Format4Segments = NULL;
            return errCode; 
        }
    ulCurrentOffset += usWordSize * usSegCount;

    for ( i = 0; i < usSegCount; i++ )
        if ((errCode = ReadWord( pInputBufferInfo,  (uint16 *)&(*Format4Segments)[i].idDelta, ulCurrentOffset+(i*usWordSize))) != NO_ERROR)
        {
            Mem_Free( *Format4Segments);
            *Format4Segments = NULL;
            return errCode; 
        }
    ulCurrentOffset += usWordSize * usSegCount;

    for ( i = 0; i < usSegCount; i++ )
        if ((errCode = ReadWord( pInputBufferInfo,  &(*Format4Segments)[i].idRangeOffset, ulCurrentOffset+(i*usWordSize))) != NO_ERROR)
        {
            Mem_Free( *Format4Segments);
            *Format4Segments = NULL;
            return errCode; 
        }
    ulCurrentOffset += usWordSize * usSegCount;

    ulBytesRead = ( ulCurrentOffset - ulOffset); /* this is defined to fit into an unsigned short */

// claudebe 2/25/00, we are shipping FE fonts with cmap subtable format 4 that have a lenght that doesn't fit in a USHORT
// commenting out the test
    *pulBytesRead = ulBytesRead;
//  if (*pulBytesRead != ulBytesRead)   /* overrun the unsigned short */
//      return(ERR_INVALID_CMAP);
    return( NO_ERROR );  

} /* ReadAllocCmapFormat4Segs( ) */



/* ---------------------------------------------------------------------- */
int16 ReadCmapLength( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                        CMAP_SUBHEADER_GEN * pCmapSubHeader,
                        uint32  ulStartOffset,
                        uint16 * pusBytesRead )
{
    int16 errCode = NO_ERROR;
    uint32 ulOffset;

    ulOffset = ulStartOffset;

    /* Read the format */
    if ((errCode = ReadWord( pInputBufferInfo, (uint16 *) &pCmapSubHeader->format, ulOffset)) != NO_ERROR)
        return(errCode);
    ulOffset += sizeof(uint16); 

    /* Read the length depending on format */
    switch (pCmapSubHeader->format)
    {
    case 0:
    case 1:
    case 2:
    case 3:
    case 4:
    case 5:
    case 6:
    case 7:
        {
            /* older cmap sub table header, with length as short */
            uint16 usLen;
            if ((errCode = ReadWord( pInputBufferInfo, (uint16 *) &usLen, ulOffset)) != NO_ERROR)
                return(errCode);
            pCmapSubHeader->length = (uint32)usLen;
            ulOffset += sizeof(uint16);
            /* skip version */
            ulOffset += sizeof(uint16);
        }
        break;

    case 14:
        {            
            /* newer cmap sub table header, w/ length as long  */
            /* Note: Format 14 does NOT have a USHORT reserved field before length as in the default case */
            if ((errCode = ReadLong( pInputBufferInfo, (uint32 *) &pCmapSubHeader->length, ulOffset)) != NO_ERROR)
                return(errCode);
            ulOffset += sizeof(uint32);
        }
        break;


    default:
        {
            /* newer cmap sub table header, w/ length as long  */
            ulOffset += sizeof(uint16);
            if ((errCode = ReadLong( pInputBufferInfo, (uint32 *) &pCmapSubHeader->length, ulOffset)) != NO_ERROR)
                return(errCode);
            ulOffset += sizeof(uint32);
        }
    }

    if (pusBytesRead)
        *pusBytesRead = (uint16)(ulOffset-ulStartOffset);

    return errCode;
}



/* ---------------------------------------------------------------------- */
int16 ReadAllocCmapFormat4( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                      CONST uint16 usPlatform,
                      CONST uint16 usEncoding,
                      uint16 *pusFoundEncoding,
                      CMAP_FORMAT4 * pCmapFormat4,
                      FORMAT4_SEGMENTS **  ppFormat4Segments,
                      GLYPH_ID ** ppGlyphId,
                      uint16 * pusnIds
                      )
{
uint32 ulOffset;
uint16 usSegCount;
uint16 usBytesRead;
uint32 ulBytesRead;
int16 errCode;
CMAP_SUBHEADER_GEN CmapSubHeader;

   /* find Format4 part of 'cmap' table */

    *ppFormat4Segments = NULL;  /* in case of error */
    *ppGlyphId = NULL;
    *pusnIds = 0;
    ulOffset = FindCmapSubtable( pInputBufferInfo, usPlatform, usEncoding, pusFoundEncoding );
    if ( ulOffset == 0 )          
        return( ERR_FORMAT );

    if ((errCode = ReadCmapLength( pInputBufferInfo, &CmapSubHeader, ulOffset, &usBytesRead)) != NO_ERROR)
        return errCode;

    if (CmapSubHeader.format != FORMAT4_CMAP_FORMAT)
        return( ERR_FORMAT );

    /* OK, it really is format 4, read the whole thing */
    if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) pCmapFormat4, SIZEOF_CMAP_FORMAT4, CMAP_FORMAT4_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
        return(errCode);

    usSegCount = pCmapFormat4->segCountX2 / 2;

   /* read variable length part */
    ulOffset += usBytesRead;

    if ((errCode = ReadAllocCmapFormat4Segs( pInputBufferInfo, usSegCount, ppFormat4Segments, ulOffset, &ulBytesRead )) != NO_ERROR)
        return(errCode);
    if ( ulBytesRead == 0)  /* 0 could mean okey dokey */
        return( NO_ERROR );                
   
    ulOffset += ulBytesRead;
    if ((errCode = ReadAllocCmapFormat4Ids( pInputBufferInfo, usSegCount, *ppFormat4Segments, ppGlyphId, pusnIds, ulOffset, &ulBytesRead )) != NO_ERROR)
    {
        FreeCmapFormat4( *ppFormat4Segments, *ppGlyphId );
        *ppFormat4Segments = NULL;
        *ppGlyphId = NULL;
        *pusnIds = 0;
        return( errCode );
    }

   return( NO_ERROR );

} /* ReadAllocCmapFormat4() */
/* ---------------------------------------------------------------------- */
void FreeCmapFormat6( uint16 *  glyphIndexArray)
{
    Mem_Free( glyphIndexArray );
}

/* ---------------------------------------------------------------------- */
int16 ReadAllocCmapFormat6( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                      CONST uint16 usPlatform,
                      CONST uint16 usEncoding,
                      uint16 *pusFoundEncoding,
                      CMAP_FORMAT6 * pCmap,
                      uint16 **  glyphIndexArray)
{
uint32 ulOffset;
uint16 usBytesRead;
uint32 ulBytesRead;
int16 errCode;

   /* locate the cmap subtable */
   
    ulOffset = FindCmapSubtable( pInputBufferInfo, usPlatform, usEncoding, pusFoundEncoding  );
    if ( ulOffset == 0 )
        return( ERR_FORMAT );

   /* Read cmap table */

    if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) pCmap, SIZEOF_CMAP_FORMAT6, CMAP_FORMAT6_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
        return (errCode);

    if (pCmap->format != FORMAT6_CMAP_FORMAT)
        return( ERR_FORMAT );

    *glyphIndexArray = (uint16 *)Mem_Alloc( pCmap->entryCount * sizeof( uint16 ));
    if ( *glyphIndexArray == NULL )
        return( ERR_MEM );

    if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *) *glyphIndexArray, WORD_CONTROL, ulOffset + usBytesRead, &ulBytesRead, pCmap->entryCount, sizeof(uint16))) != NO_ERROR)
    {
        Mem_Free(*glyphIndexArray);
        *glyphIndexArray = NULL;
        return(errCode); 
    }
    return( NO_ERROR );
}

/* ---------------------------------------------------------------------- */
int16 ReadCmapFormat0( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                      CONST uint16 usPlatform,
                      CONST uint16 usEncoding,
                      uint16 *pusFoundEncoding,
                      CMAP_FORMAT0 *pCmap)
{
uint32 ulOffset;
uint16 usBytesRead;
uint32 ulBytesRead;
int16 errCode;

   /* locate the cmap subtable */
   
    ulOffset = FindCmapSubtable( pInputBufferInfo, usPlatform, usEncoding, pusFoundEncoding  );
    if ( ulOffset == 0L )
        return( ERR_FORMAT );

   /* Read cmap table */

    if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) pCmap, SIZEOF_CMAP_FORMAT0, CMAP_FORMAT0_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
        return (errCode);

    if (pCmap->format != FORMAT0_CMAP_FORMAT)
        return( ERR_FORMAT );

    if ((errCode = ReadGenericRepeat( pInputBufferInfo, (uint8 *) &(pCmap->glyphIndexArray), BYTE_CONTROL, ulOffset + usBytesRead, &ulBytesRead, CMAP_FORMAT0_ARRAYCOUNT, sizeof(uint8))) != NO_ERROR)
        return(errCode); 
    return( NO_ERROR );

}


/* ---------------------------------------------------------------------- */
int16 ReadAllocCmapFormat12( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                      uint32 ulSubOffset,
                      CMAP_FORMAT12 * pCmapFormat12,
                      FORMAT12_GROUPS **  ppFormat12Groups
                      )
{
uint32 ulOffset;
uint32 ulGroups;
uint32 i;
uint16 usBytesRead;
int16 errCode;

    ulOffset = ulSubOffset;
    *ppFormat12Groups = NULL;   /* in case of error */

    if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) pCmapFormat12, SIZEOF_CMAP_FORMAT12, CMAP_FORMAT12_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
        return(errCode);

    ulOffset += usBytesRead; /* increment */
    ulGroups = pCmapFormat12->nGroups;


    /* allocate mem for the groups */

        if (ulGroups > (0xFFFFFFFF / SIZEOF_FORMAT12_GROUPS))
        {
            //Font is obviously broken. ulGroups * SIZEOF_FORMAT12_GROUPS should fit into font table.
            
            return ERR_MEM;
        }

    *ppFormat12Groups = (FORMAT12_GROUPS *)Mem_Alloc( ulGroups * SIZEOF_FORMAT12_GROUPS);
    if ( *ppFormat12Groups == NULL )
        return( ERR_MEM );

    /* read them */
    for ( i = 0; i < ulGroups; i++ )
    {
        if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) &((*ppFormat12Groups)[i]), SIZEOF_FORMAT12_GROUPS, FORMAT12_GROUPS_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
        {
            Mem_Free( *ppFormat12Groups);
            *ppFormat12Groups = NULL;
            return(errCode);        
        }
        ulOffset += usBytesRead;
    }
   return( NO_ERROR );

} /* ReadAllocCmapFormat12() */
/* ---------------------------------------------------------------------- */
void FreeCmapFormat12Groups( FORMAT12_GROUPS *pFormat12Groups)
{
    Mem_Free( pFormat12Groups );
}


/* ---------------------------------------------------------------------- */
int16 GetGlyphHeader( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                     uint16 GlyfIdx,
                     uint16 usIdxToLocFmt,
                     uint32 ulLocaOffset,
                     uint32 ulGlyfOffset,
                     GLYF_HEADER * pGlyfHeader,
                     uint32 * pulOffset,
                     uint16 * pusLength )
{
uint16 usOffset;
uint32 ulOffset;
uint32 ulNextOffset;
uint16 usNextOffset;
uint16 usBytesRead;
int16 errCode;

    /* determine location of glyph data */

    if ( usIdxToLocFmt == SHORT_OFFSETS )
    {
        ulLocaOffset += GlyfIdx * sizeof( uint16 );
        if ((errCode = ReadWord( pInputBufferInfo,  &usOffset, ulLocaOffset)) != NO_ERROR)
            return(errCode);
        if ((errCode = ReadWord( pInputBufferInfo,  &usNextOffset, ulLocaOffset + sizeof(uint16) )) != NO_ERROR)
            return(errCode);
        ulOffset = usOffset * 2L;
        ulNextOffset = usNextOffset * 2L;
    }
    else
    {
        ulLocaOffset += GlyfIdx * sizeof( uint32 );
        if ((errCode = ReadLong( pInputBufferInfo,  &ulOffset, ulLocaOffset)) != NO_ERROR)
            return(errCode);
        if ((errCode = ReadLong( pInputBufferInfo,  &ulNextOffset, ulLocaOffset + sizeof(uint32))) != NO_ERROR)
            return(errCode);
    }

    /* read glyph header, unless it's non-existent, in which case
    set GlyphHeader to null and return */

    *pusLength = (uint16) (ulNextOffset - ulOffset);
    if ( *pusLength == 0 )
    {
        memset( pGlyfHeader, 0, SIZEOF_GLYF_HEADER );
        *pulOffset = ulGlyfOffset;
        return NO_ERROR;
    }

    *pulOffset = ulGlyfOffset + ulOffset;
    return ReadGeneric( pInputBufferInfo,  (uint8 *) pGlyfHeader, SIZEOF_GLYF_HEADER, GLYF_HEADER_CONTROL, ulGlyfOffset + ulOffset, &usBytesRead );

} /* GetGlyphHeader() */

/* ---------------------------------------------------------------------- */
/* Recursive!! */
/* It is possible that this function could run out of stack */
/* if the font defines a VERY deep component tree. */
/* ---------------------------------------------------------------------- */
int16 GetComponentGlyphList( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                            uint16 usCompositeGlyphIdx,
                            uint16 * pusnGlyphs,
                            uint16 * ausGlyphIdxs,
                            uint16 cMaxGlyphs, /* number of elements allocated in ausGlyphIdx array */
                            uint16 *pusnComponentDepth,
                            uint16 usLevelValue, /* level of recursion we are at */
                            uint16 usIdxToLocFmt,
                            uint32 ulLocaOffset,
                            uint32 ulGlyfOffset)
{
uint16 usFlags;
uint16 usComponentGlyphIdx;
uint32 ulCrntOffset;
GLYF_HEADER GlyfHeader;
uint32 ulOffset;
uint16 usLength;
uint16 usnGlyphs;
int16 errCode;

/* check if this is a composite glyph */

    *pusnGlyphs = 0;  /* number of glyphs at this level and below */
    if ((errCode = GetGlyphHeader( pInputBufferInfo, usCompositeGlyphIdx, usIdxToLocFmt, ulLocaOffset, ulGlyfOffset, &GlyfHeader, &ulOffset, &usLength )) != NO_ERROR)
        return(errCode);
    if (*pusnComponentDepth < usLevelValue)
        *pusnComponentDepth = usLevelValue;  /* keep track of this */

    if ( GlyfHeader.numberOfContours >= 0 )
        return NO_ERROR;       /* this is not a composite, just a glyph */

    /* move to beginning of composite glyph description */

    ulCrntOffset = ulOffset + GetGenericSize( GLYF_HEADER_CONTROL );
    /* read composite glyph components, adding each component's
    reference */

    do
    {
        if (*pusnGlyphs >= cMaxGlyphs)   /* cannot do. the maxp table lied to us about maxdepth or maxelements! */
            return ERR_INVALID_MAXP;
     /* read flag word and glyph component */

        if ((errCode = ReadWord( pInputBufferInfo,   &usFlags, ulCrntOffset)) != NO_ERROR)
            return(errCode);

        ulCrntOffset += sizeof( uint16 );
        if ((errCode = ReadWord( pInputBufferInfo,  &usComponentGlyphIdx, ulCrntOffset)) != NO_ERROR)
            return(errCode);

        ulCrntOffset += sizeof( uint16 );

        ausGlyphIdxs[ *pusnGlyphs ] = usComponentGlyphIdx;
        (*pusnGlyphs)++;

        /* navigate through rest of entry to get to next glyph component */

        if ( usFlags & ARG_1_AND_2_ARE_WORDS )
            ulCrntOffset += 2 * sizeof( uint16 );
        else
            ulCrntOffset += sizeof( uint16 );

        if ( usFlags & WE_HAVE_A_SCALE )
            ulCrntOffset += sizeof( uint16 );
        else if ( usFlags & WE_HAVE_AN_X_AND_Y_SCALE )
            ulCrntOffset += 2 * sizeof( uint16 );
        else if ( usFlags & WE_HAVE_A_TWO_BY_TWO )
            ulCrntOffset += 4 * sizeof( uint16 );
        /* now deal with any components of this component */
        if ((errCode = GetComponentGlyphList(pInputBufferInfo, usComponentGlyphIdx, &usnGlyphs, &(ausGlyphIdxs[*pusnGlyphs]), (uint16) (cMaxGlyphs - *pusnGlyphs), pusnComponentDepth, 
                            (uint16) (usLevelValue + 1), usIdxToLocFmt, ulLocaOffset, ulGlyfOffset)) != NO_ERROR)
            return(errCode);
        if (usnGlyphs > 0)
            *pusnGlyphs = (uint16)(*pusnGlyphs + usnGlyphs);  /* increment count by number of sub components */

    }
    while ( usFlags & MORE_COMPONENTS);
    
    return(NO_ERROR);

} /* GetComponentGlyphList() */
/* ------------------------------------------------------------------- */
/* support for Cmap Modifying and merging */
/* ------------------------------------------------------------------- */
PRIVATE int CRTCB AscendingCodeCompare( CONST void *arg1, CONST void *arg2 )
{
    if (((PCHAR_GLYPH_MAP_LIST)(arg1))->usCharCode == ((PCHAR_GLYPH_MAP_LIST)(arg2))->usCharCode) /* they're the same */
        return 0;
    if (((PCHAR_GLYPH_MAP_LIST)(arg1))->usCharCode < ((PCHAR_GLYPH_MAP_LIST)(arg2))->usCharCode)
        return -1;
    return 1;
}

/* ------------------------------------------------------------------- */
PRIVATE void SortCodeList( PCHAR_GLYPH_MAP_LIST pCharGlyphMapList,
                          uint16 *pusnCharMapListLength )
{
uint16 i, j;

    /* sort list of character codes to keep using an insertion sort */ 

    if (pCharGlyphMapList == NULL || *pusnCharMapListLength == 0)
        return;

    qsort(pCharGlyphMapList, *pusnCharMapListLength, sizeof(*pCharGlyphMapList),  AscendingCodeCompare);
    
    /* now remove duplicates */

    for (i = 0, j = 1 ; j < *pusnCharMapListLength ; ++j)
    {
        if (pCharGlyphMapList[i].usCharCode != pCharGlyphMapList[j].usCharCode) /* not a duplicate, keep it */
        {
            if (j > i+1) /* we have removed one */
                pCharGlyphMapList[i+1] = pCharGlyphMapList[j];   /* copy it down */
            ++i; /* go to next one */
        } 
        /* otherwise, we stay where we are on i, and go look at the next j */
    }
    *pusnCharMapListLength = i+1;   /* the last good i value */
}
/* ------------------------------------------------------------------- */
PRIVATE int CRTCB AscendingCodeCompareEx( CONST void *arg1, CONST void *arg2 )
{
    if (((PCHAR_GLYPH_MAP_LIST_EX)(arg1))->ulCharCode == ((PCHAR_GLYPH_MAP_LIST_EX)(arg2))->ulCharCode) /* they're the same */
        return 0;
    if (((PCHAR_GLYPH_MAP_LIST_EX)(arg1))->ulCharCode < ((PCHAR_GLYPH_MAP_LIST_EX)(arg2))->ulCharCode)
        return -1;
    return 1;
}

PRIVATE void SortCodeListEx( PCHAR_GLYPH_MAP_LIST_EX pCharGlyphMapList,
                          uint32 *pulnCharMapListLength )
{
uint32 i, j;

    /* sort list of character codes to keep using an insertion sort */ 
    if (pCharGlyphMapList == NULL || *pulnCharMapListLength == 0)
        return;

    qsort(pCharGlyphMapList, *pulnCharMapListLength, sizeof(*pCharGlyphMapList),  AscendingCodeCompareEx);
    
    /* now remove duplicates */
    for (i = 0, j = 1 ; j < *pulnCharMapListLength ; ++j)
    {
        if (pCharGlyphMapList[i].ulCharCode != pCharGlyphMapList[j].ulCharCode) /* not a duplicate, keep it */
        {
            if (j > i+1) /* we have removed one */
                pCharGlyphMapList[i+1] = pCharGlyphMapList[j];   /* copy it down */
            ++i; /* go to next one */
        } 
        /* otherwise, we stay where we are on i, and go look at the next j */
    }
    *pulnCharMapListLength = i+1;   /* the last good i value */
}

/* ------------------------------------------------------------------- */
void FreeFormat4CharCodes(PCHAR_GLYPH_MAP_LIST pusCharCodeList)
{
    Mem_Free(pusCharCodeList);
}
/* ---------------------------------------------------------------------- */
/* create a list of character codes to keep, based on the glyph list */
int16 ReadAllocFormat4CharGlyphMapList(TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                                CONST uint16 usPlatform,
                                CONST uint16 usEncoding,
                                __in_ecount(usGlyphCount) uint8 *puchKeepGlyphList, /* glyphs to keep - boolean */
                                uint16 usGlyphCount,  /* count of puchKeepGlyphList */
                                PCHAR_GLYPH_MAP_LIST *ppCharGlyphMapList,
                                uint16 *pusnCharGlyphMapListCount)
{
uint16 usSegCount;
FORMAT4_SEGMENTS * pFormat4Segments; 
GLYPH_ID * pFormat4GlyphIdArray;
uint16 usnFormat4Glyphs;    /* count of elements in pFormat4GlyphIdArray */
CMAP_FORMAT4 CmapFormat4;
uint16 i;
uint16 usCharCodeValue;
uint16 usCharCodeCount;
uint16 usCharCodeIndex;
uint16 usGlyphIndex;
uint16 usFoundEncoding;
int32 sIDIdx;
int16 errCode = NO_ERROR;

/* allocate memory for variable length part of table */

    *ppCharGlyphMapList = NULL;
    *pusnCharGlyphMapListCount = 0;

    errCode = ReadAllocCmapFormat4( pInputBufferInfo, usPlatform, usEncoding, &usFoundEncoding, &CmapFormat4, &pFormat4Segments, &pFormat4GlyphIdArray, &usnFormat4Glyphs);
    if (errCode != NO_ERROR)
        return errCode;

    usSegCount = CmapFormat4.segCountX2 / 2;

/* zip through cmap entries,counting the char code entries */

    usCharCodeCount = 0;
    for ( i = 0; i < usSegCount; i++ )
    {
        if (pFormat4Segments[ i ].endCount == INVALID_CHAR_CODE)
            continue;
        if (pFormat4Segments[ i ].endCount < pFormat4Segments[ i ].startCount)
            continue;
        usCharCodeCount += (pFormat4Segments[ i ].endCount - pFormat4Segments[ i ].startCount + 1);
    }

    *ppCharGlyphMapList = (PCHAR_GLYPH_MAP_LIST)Mem_Alloc(usCharCodeCount * sizeof(**ppCharGlyphMapList));
    if (*ppCharGlyphMapList == NULL)
    {
        FreeCmapFormat4(pFormat4Segments, pFormat4GlyphIdArray);
        return ERR_MEM;
    }

    *pusnCharGlyphMapListCount = usCharCodeCount;

    usCharCodeIndex = 0;
    for ( i = 0; i < usSegCount; i++ )
    {
        if (pFormat4Segments[ i ].endCount == INVALID_CHAR_CODE)
            continue;
        if (pFormat4Segments[ i ].endCount < pFormat4Segments[ i ].startCount)
            continue;
#pragma warning (suppress : 22019) /* reviewed - safe to suppress this warning */
        for (usCharCodeValue = pFormat4Segments[ i ].startCount; usCharCodeValue <= pFormat4Segments[ i ].endCount; ++usCharCodeValue)
        {
           /* grab this from GetGlyphIndex to speed things up */
            if ( pFormat4Segments[ i ].idRangeOffset == 0 )
                usGlyphIndex = usCharCodeValue + pFormat4Segments[ i ].idDelta;
            else
            {
                sIDIdx = (uint16) i - (uint16) usSegCount; 
                sIDIdx += (uint16) (pFormat4Segments[i].idRangeOffset / 2) + usCharCodeValue - pFormat4Segments[i].startCount;
                /* check bounds */
                if (sIDIdx < usnFormat4Glyphs)
                {
                    usGlyphIndex = pFormat4GlyphIdArray[ sIDIdx ];
                    if (usGlyphIndex)
                        /* Only add in idDelta if we've really got a glyph! */
                        usGlyphIndex = (uint16)(usGlyphIndex + pFormat4Segments[i].idDelta);
                }
                else
                {
                    usGlyphIndex = INVALID_GLYPH_INDEX;
                }
            }
    /* check to see if this glyphIndex is supported */
    /*      usGlyphIndex = GetGlyphIdx( usCharCodeValue, pFormat4Segments, usSegCount, pFormat4GlyphIdArray );  */
            if (usGlyphIndex != 0 && usGlyphIndex != INVALID_GLYPH_INDEX && usGlyphIndex < usGlyphCount)
                if (puchKeepGlyphList[ usGlyphIndex ]) 
                {
                    (*ppCharGlyphMapList)[usCharCodeIndex].usCharCode = usCharCodeValue; /* assign the Character code */
                    (*ppCharGlyphMapList)[usCharCodeIndex++].usGlyphIndex = usGlyphIndex; /* assign the GlyphIndex */
                }
        }
    }
    *pusnCharGlyphMapListCount = usCharCodeIndex;

    FreeCmapFormat4(pFormat4Segments, pFormat4GlyphIdArray);

    SortCodeList(*ppCharGlyphMapList, pusnCharGlyphMapListCount);

    return( errCode );
}


/* ------------------------------------------------------------------- */
void FreeFormat12CharCodes(PCHAR_GLYPH_MAP_LIST_EX pulCharCodeList)
{
    Mem_Free(pulCharCodeList);
}
/* ---------------------------------------------------------------------- */
/* create a list of character codes to keep, based on the glyph list */
int16 ReadAllocFormat12CharGlyphMapList(TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                                uint32 ulOffset,
                                uint8 *puchKeepGlyphList, /* glyphs to keep - boolean */
                                uint16 usGlyphCount,  /* count of puchKeepGlyphList */
                                PCHAR_GLYPH_MAP_LIST_EX *ppCharGlyphMapList,
                                uint32 *pulnCharGlyphMapListCount)
{
uint32 ulnGroups;
uint32 ulCharCodeCount;
uint32 ulCharCodeValue;
uint32 ulCharCodeIndex;
uint32 ulGlyphIndex;
uint16 usGlyphIndex;
FORMAT12_GROUPS * pFormat12Groups; 
CMAP_FORMAT12 CmapFormat12;
uint32 i;
int16 errCode = NO_ERROR;

/* allocate memory for variable length part of table */
    *ppCharGlyphMapList = NULL;
    *pulnCharGlyphMapListCount = 0;

    errCode = ReadAllocCmapFormat12( pInputBufferInfo, ulOffset, &CmapFormat12, &pFormat12Groups);
    if (errCode != NO_ERROR)
        return errCode;

    ulnGroups = CmapFormat12.nGroups;

/* zip through cmap entries,counting the char code entries */

    ulCharCodeCount = 0;
    for ( i = 0; i < ulnGroups; i++ )
    {
        if (pFormat12Groups[ i ].endCharCode < pFormat12Groups[ i ].startCharCode)
            continue;
        ulCharCodeCount += (pFormat12Groups[ i ].endCharCode - pFormat12Groups[ i ].startCharCode + 1);
    }

    *ppCharGlyphMapList = (PCHAR_GLYPH_MAP_LIST_EX)Mem_Alloc(ulCharCodeCount * sizeof(**ppCharGlyphMapList));
    if (*ppCharGlyphMapList == NULL)
    {
        FreeCmapFormat12Groups( pFormat12Groups );
        return ERR_MEM;
    }

    *pulnCharGlyphMapListCount = ulCharCodeCount;

    ulCharCodeIndex = 0;
    for ( i = 0; i < ulnGroups; i++ )
    {
        if (pFormat12Groups[ i ].endCharCode < pFormat12Groups[ i ].startCharCode)
            continue;
        for (ulCharCodeValue = pFormat12Groups[ i ].startCharCode; ulCharCodeValue <= pFormat12Groups[ i ].endCharCode; ++ulCharCodeValue)
        {
            ulGlyphIndex = pFormat12Groups[ i ].startGlyphCode + (ulCharCodeValue - pFormat12Groups[ i ].startCharCode);
            usGlyphIndex = (uint16)ulGlyphIndex;
            if (usGlyphIndex != 0 && ulGlyphIndex < (uint32)usGlyphCount && puchKeepGlyphList[usGlyphIndex]) 
            {
                    ((*ppCharGlyphMapList)[ulCharCodeIndex]).ulCharCode = ulCharCodeValue; /* assign the Character code */
                    ((*ppCharGlyphMapList)[ulCharCodeIndex]).ulGlyphIndex = ulGlyphIndex; /* assign the GlyphIndex */
                    ulCharCodeIndex++;
            }
        }
    }
    *pulnCharGlyphMapListCount = ulCharCodeIndex;

    FreeCmapFormat12Groups( pFormat12Groups );

    SortCodeListEx(*ppCharGlyphMapList, pulnCharGlyphMapListCount);

    return( errCode );
}




/* ------------------------------------------------------------------- */
PRIVATE uint32 Format4CmapLength( uint16 usnSegments,
                    uint16 usnGlyphIdxs )
{
    return( GetGenericSize( CMAP_FORMAT4_CONTROL ) + usnSegments * GetGenericSize( FORMAT4_SEGMENTS_CONTROL ) +
               usnGlyphIdxs * sizeof(int16) + sizeof( uint16 ));   
}
/* ------------------------------------------------------------------- */
/* this routine computes new values for the format 4 cmap table 
based on a list of character codes and corresponding glyph 
indexes.  */ 
/* ------------------------------------------------------------------- */
void ComputeFormat4CmapData( 
            CMAP_FORMAT4 * pCmapFormat4, /* to be set by this routine */
            FORMAT4_SEGMENTS * NewFormat4Segments, /* to be set by this routine */
            uint16 * pusnSegment, /* count of NewFormat4Segments - returned */
            GLYPH_ID * NewFormat4GlyphIdArray, /* to be set by this routine */
            uint16 * psnFormat4GlyphIdArray, /* count of NewFormat4GlyphIdArray - returned */
            PCHAR_GLYPH_MAP_LIST pCharGlyphMapList, /* input - map of CharCode to GlyphIndex */
            uint16 usnCharGlyphMapListCount)     /* input */
{
uint16         i;
uint16         j;
uint16         usFormat4GlyphIdArrayIndex;
BOOL           bUseIdDelta;
uint16         usStartIndex;
uint16         usEndIndex;

    /* compute new format 4 data */

    i            = 0;
    *pusnSegment = 0;
    *psnFormat4GlyphIdArray = 0;

    while ( i < usnCharGlyphMapListCount )
    {
        /* find the number of consecutive entries */

        usStartIndex = i;
        while ( i < usnCharGlyphMapListCount-1 && pCharGlyphMapList[ i ].usCharCode + 1 == pCharGlyphMapList[ i+1 ].usCharCode )
            i++;
        usEndIndex = i;
        i++;

        /* determine whether to use idDelta or idRangeOffset representation. 
        Default is to use idDelta if all glyphId's are also consecutive
        because that is more space-efficient.  A second pass is made 
        through the data later to compute idRangeOffset values. */

        bUseIdDelta = TRUE;
        for ( j = usStartIndex; j < usEndIndex && bUseIdDelta; j++ )
        {
            if ( pCharGlyphMapList[ j ].usGlyphIndex + 1 != pCharGlyphMapList[ j + 1 ].usGlyphIndex )
            bUseIdDelta = FALSE;
        }

        /* save cmap data */

        NewFormat4Segments[ *pusnSegment ].startCount = pCharGlyphMapList[ usStartIndex ].usCharCode;
        NewFormat4Segments[ *pusnSegment ].endCount   = pCharGlyphMapList[ usEndIndex ].usCharCode;
        if ( bUseIdDelta )
        {
            NewFormat4Segments[ *pusnSegment ].idDelta = pCharGlyphMapList[ usStartIndex ].usGlyphIndex - 
                                         pCharGlyphMapList[ usStartIndex ].usCharCode;
            NewFormat4Segments[ *pusnSegment ].idRangeOffset = FALSE; /* This is mis-used temporarily, but will be set below to the proper value */
        }
        else
        {
            NewFormat4Segments[ *pusnSegment ].idDelta = 0;
            NewFormat4Segments[ *pusnSegment ].idRangeOffset = TRUE; /* This is mis-used temporarily, but will be set below to the proper value */
        }
        (*pusnSegment)++;
    }

    /* make pass through data to compute idRangeOffset info.  This is 
    deferred to this point because we need to know the number of 
    segments before we can compute the idRangeOffset entries. */ 

    usFormat4GlyphIdArrayIndex = 0;
    for ( i = 0; i < *pusnSegment; i++ )
    {
        if ( NewFormat4Segments[ i ].idRangeOffset == FALSE )
        {
            /* We've done it all with sequential glyph ranges... */
            usFormat4GlyphIdArrayIndex += NewFormat4Segments[ i ].endCount - NewFormat4Segments[ i ].startCount + 1;
            NewFormat4Segments[ i ].idRangeOffset = 0;
        }
        else    /* Non-sequential glyph range: compute idRangeOffset for the segment */
        {
            NewFormat4Segments[ i ].idRangeOffset = (*pusnSegment + 1 - i + *psnFormat4GlyphIdArray) * 2;

            /* insert glyph indices into the GlyphId array */
            for ( j = NewFormat4Segments[ i ].startCount ; j <= NewFormat4Segments[ i ].endCount ; j++ )
                NewFormat4GlyphIdArray[ (*psnFormat4GlyphIdArray)++ ] = pCharGlyphMapList[ usFormat4GlyphIdArrayIndex++ ].usGlyphIndex;
        }
    }

    /* add the final, required 0xFFFF entry */

    NewFormat4Segments[ *pusnSegment ].idRangeOffset = 0;
    NewFormat4Segments[ *pusnSegment ].idDelta       = 1;
    NewFormat4Segments[ *pusnSegment ].endCount      = INVALID_CHAR_CODE;
    NewFormat4Segments[ *pusnSegment ].startCount    = INVALID_CHAR_CODE;
    (*pusnSegment)++;

    /* modify format 4 header data and write out the data */
    pCmapFormat4->format        = FORMAT4_CMAP_FORMAT;
    pCmapFormat4->revision      = 0;
    pCmapFormat4->length        = (uint16) Format4CmapLength( *pusnSegment, *psnFormat4GlyphIdArray );
    pCmapFormat4->segCountX2    = *pusnSegment * 2;
    pCmapFormat4->searchRange   = 0x0001 << ( log2( *pusnSegment ) + 1 );
    pCmapFormat4->entrySelector = log2( (uint16)(pCmapFormat4->searchRange / 2) );
    pCmapFormat4->rangeShift    = 2 * *pusnSegment - pCmapFormat4->searchRange;
}


/* ------------------------------------------------------------------- */
/* this routine writes out the cmap data after it has been 
reconstructed around the missing glyphs.  It assumes that there 
is already enough space allocated to hold the new format 4 
subtable. */ 
/* ------------------------------------------------------------------- */
int16 WriteOutFormat4CmapData( 
               TTFACC_FILEBUFFERINFO * pOutputBufferInfo,         
               CMAP_FORMAT4 *pCmapFormat4,  /* created by ComputeNewFormat4Data */
               FORMAT4_SEGMENTS * NewFormat4Segments, /* created by ComputeNewFormat4Data */
               GLYPH_ID * NewFormat4GlyphIdArray, /* created by ComputeNewFormat4Data */
               uint16 usnSegment, /* number of NewFormat4Segments elements */ 
               uint16 snFormat4GlyphIdArray, /* number of NewFormat4GlyphIdArray elements */
               uint32 ulNewOffset,  /* where to write the table */
               uint32 *pulBytesWritten)  /* number of bytes written to table */
{
uint16 i;
uint16 usBytesWritten;
int16 errCode;
uint32 ulOffset;

    ulOffset = ulNewOffset;
    if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) pCmapFormat4, SIZEOF_CMAP_FORMAT4, CMAP_FORMAT4_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
        return errCode;
    ulOffset += usBytesWritten;

    /* write out modified cmap arrays */
    for ( i = 0; i < usnSegment; i++ )
        if ((errCode = WriteWord( pOutputBufferInfo, NewFormat4Segments[ i ].endCount, ulOffset + (i * sizeof(uint16)) )) != NO_ERROR)
            return errCode;
    ulOffset += usnSegment*sizeof(uint16);
    
    if ((errCode = WriteWord( pOutputBufferInfo, (uint16) 0, ulOffset )) != NO_ERROR)  /* pad word */
        return errCode;
    ulOffset += sizeof(uint16); 

    for ( i = 0; i < usnSegment; i++ )
        if ((errCode = WriteWord( pOutputBufferInfo, NewFormat4Segments[ i ].startCount, ulOffset + (i * sizeof(uint16)) )) != NO_ERROR)
            return errCode;
    ulOffset += usnSegment*sizeof(uint16);

    for ( i = 0; i < usnSegment; i++ )
        if ((errCode = WriteWord( pOutputBufferInfo, NewFormat4Segments[ i ].idDelta, ulOffset + (i * sizeof(uint16)) )) != NO_ERROR)
            return errCode;
    ulOffset += usnSegment*sizeof(uint16);

    for ( i = 0; i < usnSegment; i++ )
        if ((errCode = WriteWord( pOutputBufferInfo, NewFormat4Segments[ i ].idRangeOffset, ulOffset + (i * sizeof(uint16)) )) != NO_ERROR)
            return errCode;
    ulOffset += usnSegment*sizeof(uint16);

    /* write out glyph id array */
    for ( i = 0; i < snFormat4GlyphIdArray; i++ )
        if ((errCode = WriteWord( pOutputBufferInfo, NewFormat4GlyphIdArray[ i ], ulOffset + (i * sizeof(uint16)) )) != NO_ERROR)
            return errCode;
    ulOffset += snFormat4GlyphIdArray * sizeof(uint16);

    *pulBytesWritten = ulOffset - ulNewOffset;
            
    return NO_ERROR;

}

/* ------------------------------------------------------------------- */
/* this routine computes new values for the format 12 cmap table 
based on a list of character codes and corresponding glyph 
indexes.  */ 
/* ------------------------------------------------------------------- */
void ComputeFormat12CmapData( CMAP_FORMAT12 * pCmapFormat12, /* to be set by this routine */
                            FORMAT12_GROUPS * NewFormat12Groups, /* to be set by this routine */
                            uint32 * pulnGroups,            /* count of NewFormat12Groups - returned */
                            PCHAR_GLYPH_MAP_LIST_EX pCharGlyphMapList, /* input - map of CharCode to GlyphIndex */
                            uint32 ulnCharGlyphMapListCount)     /* input */
{
uint32         i;
uint32         ulStartIndex;
uint32         ulEndIndex;

    /* compute new format 12 data */
    i           = 0;
    *pulnGroups = 0;

    while ( i < ulnCharGlyphMapListCount )
    {
        /* find the number of consecutive entries */
        ulStartIndex = i;
        while ( i < ulnCharGlyphMapListCount-1 && 
            pCharGlyphMapList[ i ].ulCharCode + 1 == pCharGlyphMapList[ i+1 ].ulCharCode &&
            pCharGlyphMapList[ i ].ulGlyphIndex+1 == pCharGlyphMapList[ i+1 ].ulGlyphIndex )
            i++;

        ulEndIndex = i;
        i++;

        /* Set this group and move on to next */
        NewFormat12Groups[*pulnGroups].startCharCode  = pCharGlyphMapList[ulStartIndex].ulCharCode;
        NewFormat12Groups[*pulnGroups].endCharCode    = pCharGlyphMapList[ulEndIndex].ulCharCode;
        NewFormat12Groups[*pulnGroups].startGlyphCode = pCharGlyphMapList[ulStartIndex].ulGlyphIndex;
        (*pulnGroups)++;
    }


    /* modify format 12 header data */
    pCmapFormat12->format       = FORMAT12_CMAP_FORMAT;
    pCmapFormat12->revision     = 0;
    pCmapFormat12->length       = GetGenericSize( CMAP_FORMAT12_CONTROL ) + (*pulnGroups) * GetGenericSize( FORMAT12_GROUPS_CONTROL );
    pCmapFormat12->nGroups      = *pulnGroups;
}
/* ------------------------------------------------------------------- */
/* this routine writes out the cmap data after it has been 
reconstructed around the missing glyphs.  It assumes that there 
is already enough space allocated to hold the new format 12 
subtable. */ 
/* ------------------------------------------------------------------- */
int16 WriteOutFormat12CmapData( 
               TTFACC_FILEBUFFERINFO * pOutputBufferInfo,         
               CMAP_FORMAT12 *pCmapFormat12,        /* created by ComputeNewFormat12Data */
               FORMAT12_GROUPS * NewFormat12Groups, /* created by ComputeNewFormat12Data */
               uint32 ulnGroups,                    /* number of NewFormat12Groups elements */ 
               uint32 ulNewOffset,                  /* where to write the table */
               uint32 *pulBytesWritten)             /* number of bytes written to table */
{
uint32 i;
uint16 usBytesWritten;
int16 errCode;
uint32 ulOffset;

    ulOffset = ulNewOffset;
    if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) pCmapFormat12, SIZEOF_CMAP_FORMAT12, CMAP_FORMAT12_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
        return errCode;
    ulOffset += usBytesWritten;

    /* write out modified cmap arrays */
    for ( i = 0; i < ulnGroups; i++ )
    {
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &NewFormat12Groups[i], SIZEOF_FORMAT12_GROUPS, FORMAT12_GROUPS_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
            return errCode;
        ulOffset += usBytesWritten;
    }

    /* set number of bytes written */
    *pulBytesWritten = ulOffset - ulNewOffset;
            
    return NO_ERROR;
}





/* ---------------------------------------------------------------------- */
/* This function will allocate memory and read into that memory an array of NAMERECORD structures. */
/* note this structure is defined in the .h file for this module, and is similar, but not identical */
/* to the NAME_RECORD structure as described in TTFF.H. The first 6 elements are the same, but extra */
/* data is needed for these functions. When this function returns NO_ERROR, the *ppNameRecordArray */
/* will point to the allocated array and the *pNameRecordCount value will be set to the number of */
/* records in the array. */
/* ---------------------------------------------------------------------- */
int16 ReadAllocNameRecords(TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                           PNAMERECORD *ppNameRecordArray, /* allocated by this function */
                           uint16 *pNameRecordCount, /* number of records in array */
                           CFP_ALLOCPROC lfpnAllocate,  /* how to allocate array, and strings */
                           CFP_FREEPROC lfpnFree) /* how to free in case of error */
{
NAME_HEADER NameHeader;
int16 errCode;
uint32 ulNameOffset;
uint32 ulNameLength;
uint32 ulOffset;
uint16 usBytesRead;
uint16 i;

    *ppNameRecordArray = NULL;
    *pNameRecordCount = 0;
    ulNameOffset = TTTableOffset( pInputBufferInfo, NAME_TAG );
    if ( ulNameOffset == DIRECTORY_ERROR )
        return ERR_MISSING_NAME;    /* no table there */
    ulNameLength = TTTableLength( pInputBufferInfo, NAME_TAG );
    if ( ulNameLength == DIRECTORY_ERROR )
        return ERR_INVALID_NAME;
    if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) &NameHeader, SIZEOF_NAME_HEADER, NAME_HEADER_CONTROL, ulNameOffset, &usBytesRead )) != NO_ERROR)
        return errCode;
    ulOffset = ulNameOffset + usBytesRead;

    *ppNameRecordArray = (PNAMERECORD) lfpnAllocate(NameHeader.numNameRecords * sizeof(**ppNameRecordArray));
    if (*ppNameRecordArray == NULL)
        return ERR_MEM;
    *pNameRecordCount = NameHeader.numNameRecords; 

    for (i = 0; i < *pNameRecordCount; ++i)
    {
        /* This read into NAMERECORD instead of NAME_RECORD works because the first 6 elements are the same */
        if ((errCode = ReadGeneric( pInputBufferInfo, (uint8 *) &((*ppNameRecordArray)[i]), SIZEOF_NAME_RECORD, NAME_RECORD_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
            break;
        ulOffset += usBytesRead;
        if ((*ppNameRecordArray)[i].stringLength == INVALID_NAME_STRING_LENGTH) /* will get removed upon write */
            continue;
        (*ppNameRecordArray)[i].pNameString = (char *)lfpnAllocate((*ppNameRecordArray)[i].stringLength);
        if ((*ppNameRecordArray)[i].pNameString == NULL)
        {
            errCode = ERR_MEM;
            break;
        }
        if ((errCode = ReadBytes( pInputBufferInfo, (uint8 *)((*ppNameRecordArray)[i].pNameString), ulNameOffset + NameHeader.offsetToStringStorage + (*ppNameRecordArray)[i].stringOffset, (*ppNameRecordArray)[i].stringLength)) != NO_ERROR)
            break;
        (*ppNameRecordArray)[i].pNewNameString = NULL;
        (*ppNameRecordArray)[i].bStringWritten = FALSE;
        (*ppNameRecordArray)[i].bDeleteString = FALSE;
    }
    if (errCode != NO_ERROR)  /* need to free up allocated stuff */
    {
        FreeNameRecords(*ppNameRecordArray, *pNameRecordCount, lfpnFree);
        *ppNameRecordArray = NULL;
        *pNameRecordCount = 0;
    }

    return errCode;
}

/* ---------------------------------------------------------------------- */
uint32 CalcMaxNameTableLength(PNAMERECORD pNameRecordArray, 
                              uint16 NameRecordCount)
{
uint16 i;
uint32 ulNameTableLength= 0;
uint16 ValidNameRecordCount = 0;

    if (pNameRecordArray == NULL || NameRecordCount == 0)
        return 0L;

    /*  add in all the space for the strings */
    for (i = 0; i < NameRecordCount; ++i)
    {
        if (pNameRecordArray[i].stringLength != INVALID_NAME_STRING_LENGTH)
            ++ValidNameRecordCount;
        ulNameTableLength += pNameRecordArray[i].stringLength;
    }
    /* now add in the array */
    ulNameTableLength += (GetGenericSize(NAME_HEADER_CONTROL) + GetGenericSize(NAME_RECORD_CONTROL) * ValidNameRecordCount);

    return ulNameTableLength;
}

/* ---------------------------------------------------------------------- */
/* Local structure to be made into an array with a 1 to 1 correspondence with the NameRecordArray */
/* this array will be used to sort the records by string length, and then later by NameRecordIndex */
/* without actually changing the order of the NameRecordArray elements */
/* ---------------------------------------------------------------------- */
typedef struct namerecordstrings NAMERECORDSTRINGS;

struct namerecordstrings
{
    uint16 usNameRecordIndex; /* index into name record array */
    uint16 usNameRecordStringLength;  /* length of that string */
    uint16 usNameRecordStringIndex; /* index of Name record who's string this should use*/
    uint16 usNameRecordStringCharIndex; /* index into string referenced by StringIndex of where this string starts */
};
/* ---------------------------------------------------------------------- */
PRIVATE int CRTCB DescendingStringLengthCompare( CONST void *arg1, CONST void *arg2 )
{
    if (((NAMERECORDSTRINGS *)(arg1))->usNameRecordStringLength == ((NAMERECORDSTRINGS *)(arg2))->usNameRecordStringLength) /* they're the same */
        return 0;
    if (((NAMERECORDSTRINGS *)(arg1))->usNameRecordStringLength < ((NAMERECORDSTRINGS *)(arg2))->usNameRecordStringLength)
        return 1; /* reversed because we want descending order */
    return -1;  
}
/* ---------------------------------------------------------------------- */
PRIVATE int CRTCB AscendingRecordIndexCompare( CONST void *arg1, CONST void *arg2 )
{
    if (((NAMERECORDSTRINGS *)(arg1))->usNameRecordIndex == ((NAMERECORDSTRINGS *)(arg2))->usNameRecordIndex) /* they're the same */
        return 0;
    if (((NAMERECORDSTRINGS *)(arg1))->usNameRecordIndex < ((NAMERECORDSTRINGS *)(arg2))->usNameRecordIndex)
        return -1;
    return 1;
}

/* ---------------------------------------------------------------------- */
/* sort largest first */
PRIVATE void SortNameRecordsByStringLength(NAMERECORDSTRINGS *pNameRecordStrings,uint16 NameRecordCount)
{
    if (pNameRecordStrings == NULL || NameRecordCount == 0)
        return;

    qsort (pNameRecordStrings, NameRecordCount, sizeof(*pNameRecordStrings),DescendingStringLengthCompare); 

}
/* ---------------------------------------------------------------------- */
/* sorts by index */
PRIVATE void SortNameRecordsByNameRecordIndex(NAMERECORDSTRINGS *pNameRecordStrings,uint16 NameRecordCount)
{
    if (pNameRecordStrings == NULL || NameRecordCount == 0)
        return;

    qsort (pNameRecordStrings, NameRecordCount, sizeof(*pNameRecordStrings),AscendingRecordIndexCompare); 

}
/* ---------------------------------------------------------------------- */
/* sorts by platformID, then encodingID, then languageID, then nameID */
PRIVATE int CRTCB AscendingNameRecordCompare( CONST void *arg1, CONST void *arg2 )
{

    if (((PNAMERECORD)(arg1))->platformID == ((PNAMERECORD)(arg2))->platformID) /* they're the same */
    {
        if (((PNAMERECORD)(arg1))->encodingID == ((PNAMERECORD)(arg2))->encodingID) /* they're the same */
        {
            if (((PNAMERECORD)(arg1))->languageID == ((PNAMERECORD)(arg2))->languageID) /* they're the same */
            {
                if (((PNAMERECORD)(arg1))->nameID == ((PNAMERECORD)(arg2))->nameID) /* they're the same */
                {
                    return 0;
                }
                if (((PNAMERECORD)(arg1))->nameID < ((PNAMERECORD)(arg2))->nameID) /* they're the same */
                    return -1;
                return 1;
            }
            if (((PNAMERECORD)(arg1))->languageID < ((PNAMERECORD)(arg2))->languageID) /* they're the same */
                return -1;
            return 1;
        }
        if (((PNAMERECORD)(arg1))->encodingID < ((PNAMERECORD)(arg2))->encodingID) /* they're the same */
            return -1;
        return 1;
    }
    if (((PNAMERECORD)(arg1))->platformID < ((PNAMERECORD)(arg2))->platformID) /* they're the same */
        return -1;
    return 1;
}
/* ---------------------------------------------------------------------- */
/* This module will take as input the pNameRecordArray that has been created by ReadAllocNameRecords and possible modified */
/* by the client, and write the records out the the OutputBuffer in and optimized fashion. That is if there is possible */
/* sharing of string data among NameRecords, it is written that way. */
/* note, this function is not the inverse of ReadAllocNameRecords because it writes the name table directly to the buffer at */
/* offset 0. It is up to the client to place that data in a TrueType file and update the directory that points to it */
/* To get a maximum size for the buffer to pass in, call CalcMaxNameTableLength. This will return the size of an unoptimized */
/* name table. */
/* ---------------------------------------------------------------------- */
int16 WriteNameRecords(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, /* bufferInfo for a NAME table, not a TrueType file */
                       PNAMERECORD pNameRecordArray, 
                       uint16 NameRecordCount,
                       BOOL bDeleteStrings,   /* if true don't write out strings marked for deletion. if false, write out all strings */
                       BOOL bOptimize, /* lcp 4/8/97, if True optimize the string storage for smallest space */
                       uint32 *pulBytesWritten)
{
NAME_HEADER NameHeader;
NAMERECORDSTRINGS *pNameRecordStrings; /* structure array to allow sorting by string length */
int16 errCode = NO_ERROR;
uint32 ulNameOffset;/* an absolute offset from beginning of buffer */
uint16 usStringsOffset;   /* a relative offset from beginning of the name table */
uint32 ulOffset;   /* current absolute offset used for writing out Name_Records */
uint16 usBytesWritten;
uint16 i,j,k;
uint16 index;
uint16 BaseIndex;
uint16 ValidNameRecordCount = 0;
char *pStr1, *pStr2; /* temps to point to either new or old string from PNAMERECORD Array */
    
    
    *pulBytesWritten = 0;
    if (pNameRecordArray == NULL || NameRecordCount == 0)
        return ERR_GENERIC;

    /* before we start this, need to sort the actual NameRecords by platformID etc */
    qsort (pNameRecordArray, NameRecordCount, sizeof(*pNameRecordArray),AscendingNameRecordCompare); 
    
    ulNameOffset = 0L;
    NameHeader.formatSelector = 0;

    ulOffset = ulNameOffset + GetGenericSize(NAME_HEADER_CONTROL);

    /* first create the NameRecordStrings array to sort */
    pNameRecordStrings = (NAMERECORDSTRINGS *)Mem_Alloc(NameRecordCount * sizeof(*pNameRecordStrings));
    if (pNameRecordStrings == NULL)
        return ERR_MEM;

    for (i = ValidNameRecordCount = 0; i < NameRecordCount; ++i)
    {
        if (bDeleteStrings && pNameRecordArray[i].bDeleteString)  
            continue;
        pNameRecordStrings[ValidNameRecordCount].usNameRecordIndex = i;
        pNameRecordStrings[ValidNameRecordCount].usNameRecordStringLength = pNameRecordArray[i].stringLength;
        pNameRecordStrings[ValidNameRecordCount].usNameRecordStringIndex = i;
        pNameRecordStrings[ValidNameRecordCount].usNameRecordStringCharIndex = 0;
        pNameRecordArray[i].stringOffset = 0; /* initialize for the Write activity below */
        ++ValidNameRecordCount;
    }

    usStringsOffset = 0;
    NameHeader.offsetToStringStorage = GetGenericSize(NAME_HEADER_CONTROL) + (GetGenericSize(NAME_RECORD_CONTROL)*ValidNameRecordCount);

    if (bOptimize)
    {
    uint16 maxCharIndex;
        /* need to sort these babies by length  */
        SortNameRecordsByStringLength(pNameRecordStrings,ValidNameRecordCount);

         /* now look for identical lengths, and compare, if the same, mark them */
        for (i = 1; i < ValidNameRecordCount; ++i)
        {
            if ((pStr1 = pNameRecordArray[pNameRecordStrings[i].usNameRecordIndex].pNewNameString) == NULL) /* if we're just using the old string */
                pStr1 = pNameRecordArray[pNameRecordStrings[i].usNameRecordIndex].pNameString;

            if ((pStr2 = pNameRecordArray[pNameRecordStrings[i-1].usNameRecordIndex].pNewNameString) == NULL)
                pStr2 = pNameRecordArray[pNameRecordStrings[i-1].usNameRecordIndex].pNameString;
            
            if (  (pNameRecordStrings[i].usNameRecordStringLength == pNameRecordStrings[i-1].usNameRecordStringLength) 
                 && (memcmp(pStr1, pStr2,pNameRecordStrings[i].usNameRecordStringLength) == 0)) /* they are the same */
            {
                pNameRecordStrings[i].usNameRecordStringIndex = pNameRecordStrings[i-1].usNameRecordStringIndex; /* set the index the same */   
                pNameRecordStrings[i].usNameRecordStringCharIndex = pNameRecordStrings[i-1].usNameRecordStringCharIndex;
            }
            else /* i string is shorter (or the same), because of sort */
                /* now look for subsets (smaller within larger), if found, mark with array index and char index */
            {
                for (j = 0; j < i - 1; ++j) /* check if contained in any the string before */
                {
                    if ((pStr2 = pNameRecordArray[pNameRecordStrings[j].usNameRecordStringIndex].pNewNameString) == NULL)
                        pStr2 = pNameRecordArray[pNameRecordStrings[j].usNameRecordStringIndex].pNameString;
                    /* Calculate the maximum index beyond which the string compare would go off the end of Str2 */
                    maxCharIndex = pNameRecordStrings[j].usNameRecordStringLength - pNameRecordStrings[i].usNameRecordStringLength;
                    for (k = 0; k <= maxCharIndex; ++k) /* move along string to look for compare */
                    {
                        if (memcmp(pStr1, pStr2 + k,pNameRecordStrings[i].usNameRecordStringLength) == 0) /* one is contained in the other */
                        {
                            pNameRecordStrings[i].usNameRecordStringIndex = pNameRecordStrings[j].usNameRecordStringIndex; /* set the index the same */ 
                            pNameRecordStrings[i].usNameRecordStringCharIndex = k; /* set the char index */ 
                            break;
                        }
                    }
                    if (k <= maxCharIndex) /* we found a match and we set the values */
                        break;
                }
            }
        }
        /* now put it back the way it was. NOTE, we are only sorting ValidNameRecordCount records */
        SortNameRecordsByNameRecordIndex(pNameRecordStrings,ValidNameRecordCount);
    }
    /* now, we have an array of structures we can output */
    for (i = 0; i < ValidNameRecordCount; ++i)
    {
        index = pNameRecordStrings[i].usNameRecordIndex; 
        BaseIndex = pNameRecordStrings[i].usNameRecordStringIndex;
            
        if (!pNameRecordArray[index].bStringWritten) /* this baby has not been written */
        {
            if (index != BaseIndex) /* if this points to another string, that has not been written */
            {
                if (!pNameRecordArray[BaseIndex].bStringWritten) /* base string hasn't been written */
                {
                    pNameRecordArray[BaseIndex].stringOffset = usStringsOffset;  /* set this one too */
                    pNameRecordArray[BaseIndex].bStringWritten = TRUE;
                    if ((pStr1 = pNameRecordArray[BaseIndex].pNewNameString) == NULL)
                        pStr1 = pNameRecordArray[BaseIndex].pNameString;
                    if ((errCode = WriteBytes(pOutputBufferInfo, (uint8 *)pStr1, ulNameOffset + NameHeader.offsetToStringStorage + usStringsOffset,pNameRecordArray[BaseIndex].stringLength)) != NO_ERROR)
                        break;
                    usStringsOffset = (uint16)(usStringsOffset + pNameRecordArray[BaseIndex].stringLength);
                }
                pNameRecordArray[index].stringOffset = pNameRecordArray[BaseIndex].stringOffset + 
                                                       pNameRecordStrings[i].usNameRecordStringCharIndex;    
            }
            else
            {
                pNameRecordArray[index].stringOffset = usStringsOffset + pNameRecordStrings[i].usNameRecordStringCharIndex;
                if ((pStr1 = pNameRecordArray[index].pNewNameString) == NULL)
                    pStr1 = pNameRecordArray[index].pNameString;
                if ((errCode = WriteBytes(pOutputBufferInfo, (uint8 *)pStr1, ulNameOffset + NameHeader.offsetToStringStorage + usStringsOffset,pNameRecordArray[index].stringLength)) != NO_ERROR)
                    break;
                usStringsOffset = (uint16)(usStringsOffset + pNameRecordArray[index].stringLength);
            }
            pNameRecordArray[index].bStringWritten = TRUE;
        }
        /* now write that NameRecord thing */
        if ((errCode = WriteGeneric(pOutputBufferInfo,(uint8 *) &(pNameRecordArray[index]), SIZEOF_NAME_RECORD, NAME_RECORD_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
            break;
        ulOffset += usBytesWritten;
    }
    if (errCode == NO_ERROR)
    {
        NameHeader.numNameRecords = ValidNameRecordCount;
        *pulBytesWritten = NameHeader.offsetToStringStorage + usStringsOffset;
        errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &NameHeader, SIZEOF_NAME_HEADER, NAME_HEADER_CONTROL, ulNameOffset, &usBytesWritten );
    }
    Mem_Free(pNameRecordStrings);
    return errCode;
}
/* ---------------------------------------------------------------------- */
/* will free up both strings, as well as the array. NOTE: the NewNameString must */
/* have been allocated with the same function as was handed to the ReadAllocNameRecords function */
/* or something compatible with the lpfnFree function */
/* ---------------------------------------------------------------------- */
void FreeNameRecords(PNAMERECORD pNameRecordArray, uint16 NameRecordCount, CFP_FREEPROC lfpnFree)
{
uint16 i;
    
    if (pNameRecordArray == NULL)
        return;

    for (i = 0; i < NameRecordCount; ++i)
    {
        if (pNameRecordArray[i].pNameString != NULL)
            lfpnFree(pNameRecordArray[i].pNameString); 
        if (pNameRecordArray[i].pNewNameString != NULL)
            lfpnFree(pNameRecordArray[i].pNewNameString); 
    }
    lfpnFree(pNameRecordArray);
}
/* ---------------------------------------------------------------------- */
/* next three functions only used by Name Wizard and Embedding .dll, not by CreateFontPackage */
/* or MergeFontPackage */
/* ---------------------------------------------------------------------- */
int16 InsertTable(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, __in_bcount(4) const char *  szTag, uint8 * puchTableBuffer, uint32 ulTableBufferLength)
{
uint32 ulTableOffset;
uint32 ulTableLength;
uint32 ulOffset;
int16 errCode = NO_ERROR;
uint16 usnTables;
uint16 i;
uint16 usBytesRead;
uint16 usBytesWritten;
OFFSET_TABLE OffsetTable;
DIRECTORY Directory;
uint32 ulTag; 
int32 lCopySize;

    if (puchTableBuffer == NULL || ulTableBufferLength == 0)
        return ERR_GENERIC;
    
    ulTableOffset = TTTableOffset( pOutputBufferInfo, szTag);
    ulTableLength = TTTableLength( pOutputBufferInfo, szTag);

    ConvertStringTagToLong(szTag,&ulTag);

    if (ulTableOffset == DIRECTORY_ERROR)
    {
        DIRECTORY *aDirectory;
        uint16 usnNewTables, usLastTable;
        uint32 ulBytesRead, ulBytesWritten, ulNewSize, ulTableDirSize, ulNewTableDirSize;       
        
        /* read offset table and determine number of existing tables */
        ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
        if ((errCode = ReadGeneric((TTFACC_FILEBUFFERINFO *) pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
            return(errCode);
        usnTables = OffsetTable.numTables;

        usnNewTables = usnTables + 1;

        // Check for ushort overflow, number of tables becomes more than 0xFFFF
        if (usnNewTables <= usnTables)
        {
            return ERR_FORMAT;
        }

        usLastTable = usnTables;


        ulOffset += usBytesRead;
        
        aDirectory = (DIRECTORY *) Mem_Alloc(((int32)usnNewTables) * sizeof(DIRECTORY));    /* one extra for new table */
        if (aDirectory == NULL)
            return(ERR_MEM);

        /* read directory entries */
        if ((errCode = ReadGenericRepeat((TTFACC_FILEBUFFERINFO *) pOutputBufferInfo, (uint8 *)aDirectory, DIRECTORY_CONTROL, ulOffset, &ulBytesRead, usnTables, SIZEOF_DIRECTORY)) != NO_ERROR)
        {
            Mem_Free(aDirectory);
            return(errCode);
        }
        ulOffset += ulBytesRead;

        /* update existing offsets to account for new entry */
        for(i = 0; i < usLastTable; i++)
            aDirectory[ i ].offset += SIZEOF_DIRECTORY; 
        
        ulNewSize = SIZEOF_DIRECTORY + RoundToLongWord(pOutputBufferInfo->ulBufferSize);

        /* setup new entry point to end of file with zero size */
        aDirectory[ usLastTable ].length = 0;
        aDirectory[ usLastTable ].offset = ulNewSize;
        aDirectory[ usLastTable ].tag = ulTag;
        aDirectory[ usLastTable ].checkSum = 0;

        SortByTag( aDirectory, usnNewTables );
        OffsetTable.numTables =  usnNewTables;

        OffsetTable.searchRange  = (uint16)((0x0001 << ( log2( usnNewTables ))) << 4 );
        OffsetTable.entrySelector = (uint16)(log2((uint16)(0x0001 << ( log2( usnNewTables )))));
        OffsetTable.rangeShift   = (uint16)((usnNewTables << 4) - ((0x0001 << ( log2( usnNewTables ))) * 16 ));

        /* allocate space for new font image */     
        pOutputBufferInfo->puchBuffer = (uint8 *)pOutputBufferInfo->lpfnReAllocate(pOutputBufferInfo->puchBuffer,ulNewSize);
        if (pOutputBufferInfo->puchBuffer == NULL)
        {
            Mem_Free(aDirectory);
            return ERR_MEM;
        }       
        pOutputBufferInfo->ulBufferSize = ulNewSize;

        if ((errCode = ZeroLongWordAlign(pOutputBufferInfo,pOutputBufferInfo->ulBufferSize + SIZEOF_DIRECTORY, NULL)) != NO_ERROR)
        {
            Mem_Free(aDirectory);
            return errCode;
        }

        ulTableDirSize = SIZEOF_OFFSET_TABLE + (usnTables * SIZEOF_DIRECTORY);
        ulNewTableDirSize = SIZEOF_OFFSET_TABLE + (usnNewTables * SIZEOF_DIRECTORY);
        lCopySize = pOutputBufferInfo->ulBufferSize - ulNewTableDirSize;

        if (lCopySize>0 && (errCode = CopyBlock(pOutputBufferInfo,ulNewTableDirSize,ulTableDirSize,lCopySize)) != NO_ERROR)
        {
            Mem_Free(aDirectory);
            return errCode;
        }       

        /* copy in directory header */
        ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
        if ((errCode = WriteGeneric((TTFACC_FILEBUFFERINFO *) pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
        {
            Mem_Free(aDirectory);
            return(errCode);
        }
        ulOffset += usBytesWritten;

        /* copy in directory entries */
        if ((errCode = WriteGenericRepeat((TTFACC_FILEBUFFERINFO *) pOutputBufferInfo, (uint8 *)aDirectory, DIRECTORY_CONTROL, ulOffset, &ulBytesWritten, usnNewTables, SIZEOF_DIRECTORY)) != NO_ERROR)
        {
            Mem_Free(aDirectory);
            return(errCode);
        }
        ulOffset += ulBytesWritten;

        Mem_Free(aDirectory);

        ulTableOffset = TTTableOffset( pOutputBufferInfo, szTag);
        ulTableLength = TTTableLength( pOutputBufferInfo, szTag);
        if (ulTableOffset == DIRECTORY_ERROR)
            return ERR_GENERIC;     
    }

    if(ulTableLength == 0)
    {
        uint32 ulNewOffset = RoundToLongWord(pOutputBufferInfo->ulBufferSize);      
        
        pOutputBufferInfo->puchBuffer = (uint8 *)pOutputBufferInfo->lpfnReAllocate(pOutputBufferInfo->puchBuffer,ulNewOffset + RoundToLongWord(ulTableBufferLength));
        if (pOutputBufferInfo->puchBuffer == NULL)
        {
            return ERR_MEM;
        }
        pOutputBufferInfo->ulBufferSize = ulNewOffset + RoundToLongWord(ulTableBufferLength);

        if ((errCode = ZeroLongWordAlign(pOutputBufferInfo,pOutputBufferInfo->ulBufferSize, NULL)) != NO_ERROR)
            return errCode;

        ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
        if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
            return(errCode);
        usnTables = OffsetTable.numTables;
        ulOffset += usBytesRead;  /* where to start reading the Directory entries */
        for (i = 0; i < usnTables; ++i )
        {
            if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
                break;
            if (Directory.tag == ulTag) /* need to update the offset */
            {
                Directory.offset = ulNewOffset;
                if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
                    break;
            }
            ulOffset += usBytesRead; /* increment for next time */
        }
        if (errCode != NO_ERROR)
            return errCode;

        ulTableOffset = ulNewOffset;
    }else
    {
        int32 lShift;
        uint32 ulStartShiftOffset;      

        lShift = RoundToLongWord(ulTableBufferLength) - RoundToLongWord(ulTableLength);  /* number of bytes to shift the tables forward */
        ulStartShiftOffset =  ulTableOffset + RoundToLongWord(ulTableLength); /* the offset of the table after this one */
        lCopySize = pOutputBufferInfo->ulBufferSize - ulStartShiftOffset;
      /* need to move everything forward, or back, then insert this one */
        if (lShift > 0)  /* need more room */
        {
            pOutputBufferInfo->puchBuffer = (uint8 *)pOutputBufferInfo->lpfnReAllocate(pOutputBufferInfo->puchBuffer,pOutputBufferInfo->ulBufferSize + lShift);
            if (pOutputBufferInfo->puchBuffer == NULL)
                return ERR_MEM;
            pOutputBufferInfo->ulBufferSize += lShift;
        }
        if (lCopySize>0 && (errCode = CopyBlock(pOutputBufferInfo,ulStartShiftOffset + lShift, ulStartShiftOffset,lCopySize)) != NO_ERROR)
                return errCode;
        if (lShift < 0)    /* it shrank */
            pOutputBufferInfo->ulBufferSize += lShift; 

        /* now we need to update all of the offsets in the directory */
        ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
        if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
            return(errCode);
        usnTables = OffsetTable.numTables;
        ulOffset += usBytesRead;  /* where to start reading the Directory entries */
        for (i = 0; i < usnTables; ++i )
        {
            if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
                break;
            if (Directory.offset >= ulStartShiftOffset) /* need to update the offset */
            {
                Directory.offset += lShift;
                if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
                    break;
            }
            ulOffset += usBytesRead; /* increment for next time */
        }
        if (errCode != NO_ERROR)
            return errCode;
    }
    
    if ((errCode = WriteBytes(pOutputBufferInfo, puchTableBuffer, ulTableOffset, ulTableBufferLength)) != NO_ERROR)
        return errCode;
    if ((errCode = UpdateDirEntry(pOutputBufferInfo, szTag, ulTableBufferLength)) != NO_ERROR)
        return errCode;
    SetFileChecksum(pOutputBufferInfo, pOutputBufferInfo->ulBufferSize); /* lcp add this 5/20/97 */
    return errCode;
}

/* ---------------------------------------------------------------------- */
int16 WriteNameTable(TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
                     PNAMERECORD pNameRecordArray,  /* internal representation of NameRecord - from ttftable.h */
                     uint16 NameRecordCount,
                     BOOL bOptimize)  /* lcp 4/8/97, optimize string storage for size */
{
uint8 * puchBuffer;
int16 errCode = NO_ERROR;
uint32 ulBytesWritten = 0;
uint32 ulMaxNewNameTableLength;
TTFACC_FILEBUFFERINFO NameTableBufferInfo; /* needed by WriteNameRecords */

    
    ulMaxNewNameTableLength = CalcMaxNameTableLength(pNameRecordArray, NameRecordCount);

    if ((errCode = Mem_Init()) != NO_ERROR) /* need to initialize for debug mode, but make sure we're not stomping already initiated stuff */
        return errCode;
    puchBuffer = (uint8 *) Mem_Alloc(ulMaxNewNameTableLength);
    if (puchBuffer == NULL)
        return ERR_MEM;
    /* now fake up a bufferinfo so that WriteNameRecords will write to the actual file buffer */
    InitFileBufferInfo(&NameTableBufferInfo, puchBuffer, ulMaxNewNameTableLength, NULL /* can't reallocate!!! */);

    if ((errCode = WriteNameRecords(&NameTableBufferInfo, pNameRecordArray, NameRecordCount, TRUE, bOptimize, &ulBytesWritten)) == NO_ERROR)
    /* insert the Name table here, shifting other tables forward if necessary */
        errCode = InsertTable(pOutputBufferInfo, NAME_TAG, puchBuffer, ulBytesWritten); 

    Mem_Free(puchBuffer);
    Mem_End();  /* need free up the track for debug mode */

    return errCode;
}

/* ---------------------------------------------------------------------- */
int16 WriteSmartOS2Table(TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
                         MAINOS2 * pOS2)  
{
uint8 * puchBuffer;
int16 errCode = NO_ERROR;
uint16 usBytesWritten = 0;
uint16 usMaxOS2Len;
DIRECTORY Directory;
MAINOS2 OldOS2; 
TTFACC_FILEBUFFERINFO OS2TableBufferInfo;
BOOL bWritten = FALSE;

    /* look at what is in the font already to make sure it is a version we understand */
    if ( (GetTTDirectory( pOutputBufferInfo, OS2_TAG, &Directory ) != DIRECTORY_ERROR) &&
         (GetSmarterOS2( pOutputBufferInfo, &OldOS2) != 0)
       )
    {
        /* if the version is beyond what we understand */
        if(OldOS2.usVersion > 2)
        {
            /* make sure there is enough room to write what we do understand */
            if(Directory.length >= GetGenericSize(VERSION2OS2_CONTROL))
            {
                pOS2->usVersion = OldOS2.usVersion;
                if((errCode = WriteGeneric(pOutputBufferInfo, (uint8 *)pOS2, SIZEOF_VERSION2OS2, VERSION2OS2_CONTROL, 
                                Directory.offset, &usBytesWritten)) != NO_ERROR)
                    return errCode;
                bWritten = TRUE;
            }else
            {
                return ERR_FORMAT;
            }
        }
    }

    if(!bWritten)
    {
        /* if we have gotton here that means the font contains a OS2 table who's format we undestand */

        if ((errCode = Mem_Init()) != NO_ERROR) /* need to initialize for debug mode, but make sure we're not stomping already initiated stuff */
            return errCode;

        usMaxOS2Len = GetGenericSize(VERSION2OS2_CONTROL);
        puchBuffer = (uint8 *) Mem_Alloc(usMaxOS2Len);
        if (puchBuffer == NULL)
            return ERR_MEM;
        /* now fake up a bufferinfo so that we will write to the actual file buffer */
        InitFileBufferInfo(&OS2TableBufferInfo, puchBuffer, usMaxOS2Len, NULL /* can't reallocate!!! */);

        if(pOS2->usVersion == 0)
            errCode = WriteGeneric(&OS2TableBufferInfo, (uint8 *)pOS2, SIZEOF_OS2, OS2_CONTROL, 0, &usBytesWritten);
        else if(pOS2->usVersion == 1)
            errCode = WriteGeneric(&OS2TableBufferInfo, (uint8 *)pOS2, SIZEOF_NEWOS2, NEWOS2_CONTROL, 0, &usBytesWritten);
        else if(pOS2->usVersion == 2)
            errCode = WriteGeneric(&OS2TableBufferInfo, (uint8 *)pOS2, SIZEOF_VERSION2OS2, VERSION2OS2_CONTROL, 0, &usBytesWritten);
        
        if(errCode == NO_ERROR)
            errCode = InsertTable(pOutputBufferInfo, OS2_TAG, puchBuffer, usBytesWritten); 
        
        Mem_Free(puchBuffer);
        Mem_End();  /* need free up the track for debug mode */
    }

    return errCode;
}

/* ---------------------------------------------------------------------- */
int16 CompressTables( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint32 * pulBytesWritten )
{
/* this routine compresses the tables present in a font file by removing
space between them which is unused.  It follows four basic steps:

1.  Make a list of tables to keep.
2.  Sort the list of tables by offset so that the gaps between
    them are easy to detect and fill in.
3.  Move the tables to eliminate gaps.  Clean up by
    recalculating checksums and putting in zero pad bytes for
    long word alignment at the same time.
4.  Sort the list of tables by tag (as required for the table
    directory) and write out a new table directory.
*/
DIRECTORY *aDirectory;
DIRECTORY CandDirectory;
uint16 i;
OFFSET_TABLE  OffsetTable;
uint16 usnTables;
uint16 usnNewTables;
uint32 ulOffset;
uint16 usTableIdx;
uint16 usBytesRead;
uint16 usBytesWritten;
uint32 ulSaveBytesWritten = 0;
uint16 DoTwo;
int16 errCode;

    /* read offset table and determine number of existing tables */

    ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
    if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesRead)) != NO_ERROR)
        return(ERR_MEM);
    usnTables = OffsetTable.numTables;
    ulOffset += usBytesRead;
    /* Create a list of valid tables */

    aDirectory = (DIRECTORY *) Mem_Alloc((usnTables) * sizeof(DIRECTORY));
    if (aDirectory == NULL)
        return(ERR_MEM);

    usnNewTables = 0;
    for (i = 0; i < usnTables; ++i )
    {
        if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &CandDirectory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
            break;
        ulOffset += usBytesRead;
        if (CandDirectory.tag != DELETETABLETAG && 
            CandDirectory.length != 0 && 
            CandDirectory.offset != 0)
        {
            aDirectory[ usnNewTables ] = CandDirectory;
            usnNewTables++;
        }
    }

    if (errCode != NO_ERROR)
    {
        Mem_Free(aDirectory);
        return errCode;
    }
    /* sort directories by offset */

    SortByOffset( aDirectory, usnNewTables );
    
    /* compress table data and adjust directory entries to reflect
    the changes */

    ulOffset = pOutputBufferInfo->ulOffsetTableOffset + GetGenericSize( OFFSET_TABLE_CONTROL ) + (usnNewTables) * GetGenericSize( DIRECTORY_CONTROL );
    if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, ulOffset, &ulOffset)) != NO_ERROR)
    {
        Mem_Free(aDirectory);
        return errCode;
    }
    
    DoTwo = FALSE;
    for ( usTableIdx = 0; usTableIdx < usnNewTables; usTableIdx++ )
    {
        /* copy the table from where it currently is to the lowest available
        spot, thus filling in any existing gaps */
        if (!DoTwo)   /* if not the 2nd of two directories pointing to the same data */
        {
            if ((errCode = CopyBlock( pOutputBufferInfo, ulOffset, aDirectory[ usTableIdx ].offset, aDirectory[ usTableIdx ].length )) != NO_ERROR)
                break;

            if (usTableIdx + 1 < usnNewTables)
            {  /* special case for bloc and bdat tables */
                if ( (aDirectory[ usTableIdx ].offset == aDirectory[ usTableIdx + 1 ].offset) &&
                     (aDirectory[ usTableIdx ].length != 0) 
                   )
                {
                    DoTwo = TRUE;  /* need to proccess 2 directories pointing to same data */
                    aDirectory[ usTableIdx + 1 ].offset = ulOffset;
                    aDirectory[ usTableIdx + 1 ].length = aDirectory[ usTableIdx ].length;
                }
            }
            aDirectory[ usTableIdx ].offset = ulOffset;
            /* calc offset for next table */

        /* zero out any pad bytes and determine the checksum for the entry */
            if ((errCode = ZeroLongWordGap( pOutputBufferInfo, aDirectory[ usTableIdx ].offset, aDirectory[ usTableIdx ].length, &ulOffset)) != NO_ERROR)
                break;
        }
        else
        {
            DoTwo = FALSE; /* so next time we'll perform the copy */
        }
        
        if ((errCode = CalcChecksum( pOutputBufferInfo, aDirectory[ usTableIdx ].offset, aDirectory[ usTableIdx ].length, &aDirectory[ usTableIdx ].checkSum )) != NO_ERROR)
            break;
    }


    while (errCode == NO_ERROR) /* so we can break out on error */
    {
        ulSaveBytesWritten = ulOffset;
    /* write out the new directory info */

        SortByTag( aDirectory, usnNewTables );
        OffsetTable.numTables =  usnNewTables ;

        OffsetTable.searchRange  = (uint16)((0x0001 << ( log2( usnNewTables ))) << 4 );
        OffsetTable.entrySelector = (uint16)(log2((uint16)(0x0001 << ( log2( usnNewTables )))));
        OffsetTable.rangeShift   = (uint16)((usnNewTables << 4) - ((0x0001 << ( log2( usnNewTables ))) * 16 ));

        ulOffset = pOutputBufferInfo->ulOffsetTableOffset;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &OffsetTable, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
            break;
        ulOffset += usBytesWritten;
        for ( i = 0; i < usnNewTables; i++ )
        {
            if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &(aDirectory[ i ]), SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
                break;
            ulOffset += usBytesWritten;
        }
        if (errCode != NO_ERROR)
            break;

        *pulBytesWritten = ulSaveBytesWritten;
        break;
    }

    Mem_Free(aDirectory);

    return(errCode);
}
/* ---------------------------------------------------------------------- */
