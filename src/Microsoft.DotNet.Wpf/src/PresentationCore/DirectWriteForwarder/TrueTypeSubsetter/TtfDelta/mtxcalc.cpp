// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: MTXCALC.C
 *
 *
 * Routines to calc metrics from font file data.
 *
 **************************************************************************/

/* Inclusions ----------------------------------------------------------- */
#include <stdlib.h>

#include "typedefs.h"
#include "ttff.h"
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "ttftabl1.h"
#include "ttftable.h"
#include "ttferror.h" /* error codes */
#include "mtxcalc.h"


/* function definitions ---------------------------------------------- */
/* ------------------------------------------------------------------- */
PRIVATE int16 GetGlyphStats( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
uint16    usGlyphIdx,
int16 *   psnContours,
uint16 *  pusnPoints,
uint16 *  pusnInstructions,
uint16 indexToLocFormat,
uint32 ulLocaOffset,
uint32 ulGlyfOffset,
BOOL *bStatus )
{
uint32 ulOffset;
uint16 usLength;
GLYF_HEADER GlyfHeader;
uint32 ulLastPointOffset;
int16 errCode;

    *psnContours      = 0;
    *pusnPoints       = 0;
    *pusnInstructions = 0;
    *bStatus = FALSE;    /* assume no glyph there */
    if ((errCode = GetGlyphHeader( pInputBufferInfo, usGlyphIdx, indexToLocFormat, ulLocaOffset, ulGlyfOffset, &GlyfHeader, &ulOffset, &usLength )) != NO_ERROR)
        return errCode;
    if ( usLength == 0 )
        return( NO_ERROR );

    *psnContours = GlyfHeader.numberOfContours;
    if (*psnContours > 0)
    {
        /* calculate offset of last point */
        ulLastPointOffset = ulOffset + GetGenericSize( GLYF_HEADER_CONTROL ) + (*psnContours-1) * sizeof( uint16 );
        if ((errCode = ReadWord( pInputBufferInfo, pusnPoints, ulLastPointOffset)) != NO_ERROR)
            return errCode;
        (*pusnPoints)++;
        if ((errCode = ReadWord( pInputBufferInfo, pusnInstructions, ulLastPointOffset + sizeof(uint16))) != NO_ERROR)
            return errCode;
    }
    *bStatus = TRUE;
    return( NO_ERROR );
}


/* ------------------------------------------------------------------- */
/* NOT recursive, operates on "flat" tree */
PRIVATE int16 GetCompositeGlyphStats( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
             uint16 usGlyphIdx,
             int16 * psnContours,
             uint16 * pusnPoints,
             uint16 * pusnInstructions,
             uint16 * pusnComponentElements,
             uint16 * pusnComponentDepth,
             uint16 indexToLocFormat,
             uint32 ulLocaOffset,
             uint32 ulGlyfOffset,
             uint16 *  pausComponents, 
             uint16 usnMaxComponents)

{
int16    snContours;
uint16   usnPoints;
uint16   usnInstructions;
uint16   usnGlyphs;
uint16   i;
uint16   usTtlContours     = 0;
uint16   usTtlPoints       = 0;
uint16   usMaxInstructions = 0;
int16 errCode;
BOOL bStatus;

    
/* This call has changed to be recursive, will flatten out the tree */
    GetComponentGlyphList( pInputBufferInfo, usGlyphIdx, &usnGlyphs, pausComponents, usnMaxComponents, pusnComponentDepth, 0, indexToLocFormat, ulLocaOffset, ulGlyfOffset);

    /* track max number of components at any given level */
    *pusnComponentElements = max( *pusnComponentElements, usnGlyphs );

    for ( i = 0; i < usnGlyphs; i++ )
    {
        if ((errCode = GetGlyphStats( pInputBufferInfo, pausComponents[i], &snContours, &usnPoints, &usnInstructions, indexToLocFormat, ulLocaOffset, ulGlyfOffset, &bStatus )) != NO_ERROR)
            return errCode;
        if ((bStatus == TRUE) && ( snContours > 0 ))
        {
            usTtlContours     = (uint16)(usTtlContours + snContours);
            usTtlPoints       = (uint16)(usTtlPoints + usnPoints);
            usMaxInstructions = max( usMaxInstructions, usnInstructions );
        }
    }

    *psnContours      = usTtlContours;
    *pusnPoints       = usTtlPoints;
    *pusnInstructions = usMaxInstructions;
    return NO_ERROR;
}

/* ------------------------------------------------------------------- */
int16 ComputeMaxPStats( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
            uint16 *  pusMaxContours,
            uint16 *  pusMaxPoints,
            uint16 *  pusMaxCompositeContours,
            uint16 *  pusMaxCompositePoints,
            uint16 *  pusMaxInstructions,
            uint16 *  pusMaxComponentElements,
            uint16 *  pusMaxComponentDepth,
            uint16 *  pausComponents, 
            uint16 usnMaxComponents)
{
HEAD Head;
int16    snContours;
uint16   usnPoints;
uint16   usnInstructions;
uint16   usGlyphIdx;
uint16   prepLength;
uint16   fpgmLength;
uint16   usnCompElements;
uint16   usnCompDepth;
uint16 usGlyphCount;
uint32 ulLocaOffset;
uint32 ulGlyfOffset;
int16 errCode;
BOOL bStatus;

    *pusMaxContours          = 0;
    *pusMaxPoints            = 0;
    *pusMaxInstructions      = 0;
    *pusMaxCompositeContours = 0;
    *pusMaxCompositePoints   = 0;
    *pusMaxComponentElements = 0;
    *pusMaxComponentDepth    = 0;

    /* Build a Loca table that will be used to decide if a glyph has contours
    or not. There are g_usnGlyphs+1 Loca entries. */

    usGlyphCount = GetNumGlyphs(pInputBufferInfo);
    if (usGlyphCount == 0)
        return ERR_NO_GLYPHS;

    ulLocaOffset = TTTableOffset(pInputBufferInfo, LOCA_TAG);
    if ( ulLocaOffset == 0L )
        return ERR_MISSING_LOCA;

    ulGlyfOffset = TTTableOffset(pInputBufferInfo, GLYF_TAG);
    if ( ulGlyfOffset == 0L )
        return ERR_MISSING_GLYF;

    if (!GetHead(pInputBufferInfo, &Head))   /* for Head.indexToLocFormat */
        return ERR_MISSING_HEAD;

    for ( usGlyphIdx = 0; usGlyphIdx < usGlyphCount; usGlyphIdx++ )
    {
    /* get statistics on the glyph component */

        if ((errCode = GetGlyphStats(pInputBufferInfo, usGlyphIdx, &snContours, &usnPoints, &usnInstructions,
                    Head.indexToLocFormat, ulLocaOffset, ulGlyfOffset, &bStatus)) != NO_ERROR)
            return errCode;
        if (bStatus == FALSE) 
            continue;
        
        /* remember maxes for simple glyph */

        if ( snContours >= 0 )
        {
            *pusMaxContours     = max( *pusMaxContours, (uint16) snContours );
            *pusMaxPoints       = max( *pusMaxPoints, usnPoints );
            *pusMaxInstructions = max( *pusMaxInstructions, usnInstructions );
        }
        /* remember maxes for composite glyph */
        else if (snContours == -1)
        {
        /* start with usnInstructions at 0 for MAX test in fn call... */
            usnCompElements = usnCompDepth = usnInstructions = 0;
            GetCompositeGlyphStats( pInputBufferInfo, usGlyphIdx, &snContours, &usnPoints,
                 &usnInstructions, &usnCompElements, &usnCompDepth,
                 Head.indexToLocFormat, ulLocaOffset, ulGlyfOffset, pausComponents, usnMaxComponents );
            *pusMaxCompositeContours = max( *pusMaxCompositeContours, (uint16) snContours );
            *pusMaxCompositePoints   = max( *pusMaxCompositePoints, usnPoints );
            *pusMaxInstructions      = max( *pusMaxInstructions, usnInstructions );
            *pusMaxComponentElements = max( *pusMaxComponentElements, usnCompElements );
            *pusMaxComponentDepth    = max( *pusMaxComponentDepth, usnCompDepth );
        }
        else 
            return ERR_INVALID_GLYF;  /* what is it? */
    }

    prepLength = (uint16) TTTableLength( pInputBufferInfo, PREP_TAG );
    fpgmLength = (uint16) TTTableLength( pInputBufferInfo, FPGM_TAG );
    *pusMaxInstructions = max( max( prepLength, fpgmLength), *pusMaxInstructions );

    return NO_ERROR;
}

/* ------------------------------------------------------------------- */
