// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: MODGLYF.C
 *
 *
 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */
#include <stdlib.h> /* for max and min */

#include "ttfdcnfg.h"
#include "typedefs.h"
#include "ttff.h"
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "ttftabl1.h"
#include "ttftable.h"
#include "ttmem.h"
#include "util.h"
#include "modglyf.h"
#include "ttferror.h"    /* for error codes */

/* ------------------------------------------------------------------- */
/* this function modifies the glyf and loca tables by copying only glyfs
 from the glyf table that are to be kept and changing the references in the loca
table so that they indicate zero length entries in the glyf table.
The described action is taken here to reduce the size of the font file. */
/* this function will work if a glyf and or loca table already exist in the output */
/* file or not */
/* ------------------------------------------------------------------- */
int16 ModGlyfLocaAndHead( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                         TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
                         uint8 *puchKeepGlyphList, 
                         uint16 usGlyphCount,
                         uint32 *pCheckSumAdjustment,   /* this is returned to be saved with a subset1 or delta format font */
                         uint32 *pulNewOutOffset)
{

uint16 i;
uint16 usOffset;
uint16 usIdxToLocFmt;
uint16 usBytesWritten;
uint32 ulBytesWritten;
int16 errCode = NO_ERROR;
uint32 * aulLoca;
uint32 ulGlyphLength;
uint32 ulOutLoca;
uint32 ulGlyfOffset;
uint32 ulOutGlyfOffset;
uint32 ulOutGlyfDirectoryOffset;
uint32 ulHeadOffset;
uint32 ulOutLocaOffset;
uint32 ulOutLocaDirectoryOffset;
DIRECTORY LocaDirectory, GlyfDirectory;
HEAD Head;

/* allocate memory for and read loca table */

    aulLoca = (uint32 *)Mem_Alloc( (usGlyphCount + 1) * sizeof( uint32 ));
    if ( aulLoca == NULL )
        return ERR_MEM;

    if (GetLoca((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, aulLoca, usGlyphCount + 1) == 0L)
    {
        Mem_Free(aulLoca);
        return ERR_INVALID_LOCA;
    }

    if ((ulHeadOffset = GetHead(pOutputBufferInfo, &Head)) == 0L)
    {
        /* copy over head table. will update below */
        if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, HEAD_TAG, pulNewOutOffset))!=NO_ERROR)
        {
            Mem_Free(aulLoca);
            return errCode;
        }
        ulHeadOffset = GetHead(pOutputBufferInfo, &Head);
    }
    
    ulOutLoca     = 0L;
    ulGlyfOffset  = TTTableOffset( (TTFACC_FILEBUFFERINFO *)pInputBufferInfo, GLYF_TAG );
    if (ulGlyfOffset == DIRECTORY_ERROR) /* this should have been setup */
    {
        Mem_Free(aulLoca);
        return ERR_MISSING_GLYF; 
    }
    ulOutGlyfDirectoryOffset = GetTTDirectory( pOutputBufferInfo, GLYF_TAG, &GlyfDirectory); 
    /* make sure there is a directory entry */
    if (ulOutGlyfDirectoryOffset == DIRECTORY_ERROR) /* this should have been setup */
    {
        Mem_Free(aulLoca);
        return ERR_MISSING_GLYF; 
    }
    if (GlyfDirectory.offset == DIRECTORY_ERROR)
    {
        if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, *pulNewOutOffset, pulNewOutOffset)) != NO_ERROR)
        {
            Mem_Free(aulLoca);
            return errCode;
        }
        GlyfDirectory.offset = *pulNewOutOffset;
    }

    ulOutGlyfOffset = GlyfDirectory.offset;

    /* go thru the glyf table, copying up the glyphs to be saved */
    for ( i = 0; i < usGlyphCount; i++ )
    {
        ulGlyphLength = 0L;
        if (puchKeepGlyphList[i])   /* we want to keep this one */
        {
            /* copy existing glyph data to new location */

            if ( aulLoca[ i ] < aulLoca[ i+1 ] )
                ulGlyphLength = aulLoca[ i+1 ] - aulLoca[ i ];

            if ( ulGlyphLength )
            {
                if ((errCode = CopyBlockOver( pOutputBufferInfo, pInputBufferInfo, ulOutGlyfOffset + ulOutLoca, 
                        ulGlyfOffset + aulLoca[ i ], ulGlyphLength )) != NO_ERROR)
                    break;
            }
        }
        assert((ulOutLoca & 1) != 1);
        aulLoca[ i ] = ulOutLoca;
        ulOutLoca += ulGlyphLength;
        if (ulOutLoca & 1)
        {       /* the glyph offset is on an odd-byte boundry. get ready for next time */
            if ((errCode = WriteByte( pOutputBufferInfo, 0, ulOutGlyfOffset + ulOutLoca)) != NO_ERROR)
                break;
            ++ulOutLoca;
        }
    }
    if (errCode == NO_ERROR)
    {
    /* The last loca entry is the end of the last glyph! */
        *pulNewOutOffset += ulOutLoca;
        aulLoca[ usGlyphCount ] = ulOutLoca;
        GlyfDirectory.length = ulOutLoca;
        errCode = WriteGeneric( pOutputBufferInfo, (uint8 *)&GlyfDirectory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOutGlyfDirectoryOffset, &usBytesWritten );
    }

    if (errCode != NO_ERROR)
    {
        Mem_Free(aulLoca);
        return errCode;
    }

    /* write out the modified 'loca' table */
 
    ulOutLocaDirectoryOffset = GetTTDirectory( pOutputBufferInfo, LOCA_TAG, &LocaDirectory); 
    /* make sure there is a directory entry */
    if (ulOutLocaDirectoryOffset == DIRECTORY_ERROR) /* this should have been setup */
    {
        Mem_Free(aulLoca);
        return ERR_MISSING_LOCA;
    }

    if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, *pulNewOutOffset, pulNewOutOffset)) != NO_ERROR)
    {
        Mem_Free(aulLoca);
        return errCode;
    }
    ulOutLocaOffset = LocaDirectory.offset = *pulNewOutOffset;  /* where to write the loca */

    /* Check to see what format to use */
    if (ulOutLoca <= 0x1FFFC)   /* maximum number stored here (0xFFFE * 2) Chosen as conservative value over 0xFFFF * 2 */
    {
        usIdxToLocFmt = SHORT_OFFSETS;
        for ( i = 0; i <= usGlyphCount; i++ )
        {
            assert((aulLoca[i] & 1) != 1);   /* can't have this, would be truncated */
            usOffset = (uint16) (aulLoca[ i ] / 2L);
            if ((errCode = WriteWord( pOutputBufferInfo,  usOffset, ulOutLocaOffset + i*sizeof(uint16) )) != NO_ERROR)
                break;
        }
        ulOutLoca = (uint32) (usGlyphCount+1) * sizeof(uint16);
    }
    else
    {
        usIdxToLocFmt = LONG_OFFSETS;
        errCode = WriteGenericRepeat(pOutputBufferInfo,  (uint8 *) aulLoca, LONG_CONTROL,ulOutLocaOffset,&ulBytesWritten,(uint16) (usGlyphCount+1), sizeof(uint32)); 
        ulOutLoca = ulBytesWritten;
    }

    if (errCode == NO_ERROR)
    {
        /* update the length, etc. for the loca table as well */

        LocaDirectory.length = ulOutLoca;
        *pulNewOutOffset += ulOutLoca;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &LocaDirectory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOutLocaDirectoryOffset, &usBytesWritten )) == NO_ERROR)
        {
            *pCheckSumAdjustment = Head.checkSumAdjustment;/* for use by dttf table */
            Head.checkSumAdjustment = 0L;    /* needs to be 0 when setting the file checksum value */
            Head.indexToLocFormat = usIdxToLocFmt;
            errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Head, SIZEOF_HEAD, HEAD_CONTROL, ulHeadOffset, &usBytesWritten);
        }
    }

    /* clean up */

    Mem_Free( aulLoca );
    return errCode;

}

