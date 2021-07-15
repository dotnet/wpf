// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: MODTABLE.C
 *
 * entry points:

        ModHead
        ModXmtxXhea
        ModKern
        ModMaxP
        ModName
        ModOS2
        ModPost
        ModLTSH
        ModHdmx
        ModVDMX

 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */
#include <stdlib.h>
#include <string.h> /* for memset */

#include "typedefs.h"
#include "ttff.h"
#include "ttfacc.h"
#include "ttfcntrl.h"
#include "ttftabl1.h"
#include "ttftable.h"
#include "util.h"
#include "modtable.h"
#include "mtxcalc.h"
#include "ttmem.h"
#include "ttfdelta.h"   /* for format */
#include "ttferror.h"   /* for error codes */

/* here's the deal:
This function may do one of many things.
1. When GlyphIndexCount is 0, it will try to create a subsetted and shortened hmtx (or vmtx) table. 
   a. It can do this only if the highest used glyph index is less than the current numLongMetrics. 
      In this case it will write zeros to any glyph locations that are not used, and set
      the numLongMetrics value to the last used glyph index + 1, then fill the short section with
      zeros. 
   b. If this highest used glyph index is in the short section, it cannot shorten the table
      because doing so would put an incorrect advanced width value in for some of the missing glyphs.
      In this case it will leave the table entirely alone, so that the hdmx table will not be modified either.
      Modifying one without the other causes agfacomp to create huge delta values.
2. When GlyphIndexCount is not 0, it will create a Compact hmtx which only includes entries for the 
   glyphs that are actually in the font. 


/* ------------------------------------------------------------------- */
int16 ModXmtxXhea( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                  TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                  CONST uint8 *puchKeepGlyphList, 
                  CONST uint16 usGlyphListCount,
                  CONST uint16 usDttfGlyphIndexCount,
                  CONST uint16 usMaxGlyphIndexUsed,
                  BOOL isHmtx,
                  uint32 *pulNewOutOffset)
{                         

XHEA XHea;
uint16 i,j;
uint32 ulXmtxOffset;
uint32 ulXheaOffset;
uint32 ulCrntOffset;
LONGXMETRIC ZeroLongMetric;
LONGXMETRIC CurrLongMetric;
LONGXMETRIC *LongMetricsArray;
uint16 LongMetricSize;
int16 errCode;
uint16 usBytesRead;
uint16 usBytesWritten;
uint16 nNewLongMetrics;
uint32 ulBytesWritten;
const char * xmtx_tag;
const char * xhea_tag;

   /* determine number of long metrics in hmtx table */

    if (isHmtx)
    {
        xmtx_tag = HMTX_TAG;
        xhea_tag = HHEA_TAG;
        if ((ulXheaOffset = GetHHea( pOutputBufferInfo, (HHEA *) &XHea )) == 0L)
        { /* hasn't been copied yet */
            if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, xhea_tag, pulNewOutOffset)) != NO_ERROR)
                return ERR_INVALID_HHEA;
            if ((ulXheaOffset = GetHHea( pOutputBufferInfo, (HHEA *) &XHea )) == 0L)
                return ERR_MISSING_HHEA;    /* required table */
        }
    }
    else
    {
        xmtx_tag = VMTX_TAG;
        xhea_tag = VHEA_TAG;
        ulXheaOffset = TTTableOffset( (TTFACC_FILEBUFFERINFO *)pInputBufferInfo, xhea_tag);
        ulXmtxOffset = TTTableOffset( (TTFACC_FILEBUFFERINFO *)pInputBufferInfo, xmtx_tag);
        if (ulXheaOffset != DIRECTORY_ERROR && ulXmtxOffset == DIRECTORY_ERROR)   /* this is bogus, get rid of the vhea table */
        {
            MarkTableForDeletion(pOutputBufferInfo, xhea_tag);  /* there is an entry in the output directory */
            return (NO_ERROR);
        } 

        if ((ulXheaOffset = GetVHea( pOutputBufferInfo, (VHEA *) &XHea )) == 0L)
        {
            if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, xhea_tag, pulNewOutOffset)) != NO_ERROR)
            {
                if (errCode == ERR_FORMAT)
                    return NO_ERROR;    /* not required */
                else
                    return errCode;
            }
            if ((ulXheaOffset = GetVHea( pOutputBufferInfo, (VHEA *) &XHea )) == 0L)
                return ERR_MISSING_VHEA;    /* */
        }
    }


    if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, xmtx_tag, pulNewOutOffset)) != NO_ERROR)
        return errCode;
    ulXmtxOffset = TTTableOffset( pOutputBufferInfo, xmtx_tag);

    if ((XHea.numLongMetrics == 0) || (XHea.numLongMetrics > usGlyphListCount))
        return ERR_INVALID_HHEA_OR_VHEA;        /* invalid values */
    
    if (ulXmtxOffset == DIRECTORY_ERROR )
        return ERR_MISSING_HMTX_OR_VMTX;        /* required table */
                                        
    ulCrntOffset = ulXmtxOffset;
    ZeroLongMetric.xsb = 0;
    ZeroLongMetric.advanceX = 0;
    LongMetricSize = GetGenericSize(LONGXMETRIC_CONTROL);

    if (usDttfGlyphIndexCount == 0)    /* not trying to make a compact table, just subsetting */
    {
    /* check to see if we will grow. We will grow with subsetting if our last good glyph index is beyond the current numLongMetrics */

        if ((XHea.numLongMetrics != usGlyphListCount) && /* if longmetrics is the same as number of glyphs, table won't be growing  */ 
            /* need for zero based to 1 base + 1 for the dummy 0 entry */
            (usMaxGlyphIndexUsed + 1 + 1 > XHea.numLongMetrics)) /* check if we may make the table grow */
            return (ERR_WOULD_GROW);
        nNewLongMetrics = min(usGlyphListCount, usMaxGlyphIndexUsed + 1 + 1);   /* + 1 again for dummy */
        /* process all the Long metrics (and perhaps some short when we won't be modifying the table */
        for (i = 0; i < nNewLongMetrics; ++i)
        {
            if (puchKeepGlyphList[i] == FALSE)/* else we don't want to keep this one, 0 metrics to write */
            {
                if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *)&ZeroLongMetric, SIZEOF_LONGXMETRIC, LONGXMETRIC_CONTROL, ulCrntOffset, &usBytesWritten)) != NO_ERROR)
                    return (errCode);
            }
            ulCrntOffset += LongMetricSize;
        }
        /* write out short metrics of 0 for the rest of them*/
        for (i = nNewLongMetrics; i < usGlyphListCount; ++i)
        {
            if ((errCode = WriteWord(pOutputBufferInfo, 0, ulCrntOffset)) != NO_ERROR)
                return errCode;
            ulCrntOffset += sizeof (uint16);
        }
        ulBytesWritten = ulCrntOffset - ulXmtxOffset;
    }
    else  /* we want to make a compact table */
    {
    /* now collapse the table if we are in Compact form for Subsetting and Delta fonts */
    /* we will use an interrum table for simplification */
        ulCrntOffset = ulXmtxOffset;
        LongMetricsArray = (LONGXMETRIC *)Mem_Alloc(sizeof(LONGXMETRIC) * usDttfGlyphIndexCount);
        if (LongMetricsArray == NULL)
            return ERR_MEM;
        nNewLongMetrics = 0;
        for (i = 0, j= 0; i < XHea.numLongMetrics && j < usDttfGlyphIndexCount && errCode == NO_ERROR; ++i)  /* need to read and copy up the values */
        {
            if (puchKeepGlyphList[i]) /* if we want to keep the glyph, or its the last special one */
            {
                if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *)&CurrLongMetric, SIZEOF_LONGXMETRIC, LONGXMETRIC_CONTROL, ulCrntOffset, &usBytesRead)) != NO_ERROR)
                    break;
                LongMetricsArray[j] = CurrLongMetric;
                ++j;
                ++nNewLongMetrics;
            }
            else if (i == XHea.numLongMetrics-1) /* its that special dummy "last" one, need AW value */
            {
                if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *)&CurrLongMetric, SIZEOF_LONGXMETRIC, LONGXMETRIC_CONTROL, ulCrntOffset, &usBytesRead)) != NO_ERROR)
                    break;
                ++nNewLongMetrics; /* we will need an extra one, but guarenteed to be <= XHea.numLongMetrics */
            }
            ulCrntOffset += LongMetricSize;
        }
        if (errCode != NO_ERROR)
        {
            Mem_Free(LongMetricsArray);
            return errCode;
        }
        for (; i < usGlyphListCount && j < usDttfGlyphIndexCount; ++i) /* copy the xsb from the long metrics */
        {
            if (puchKeepGlyphList[i])
            {
                if ((errCode = ReadWord( pOutputBufferInfo, (uint16 *)&(CurrLongMetric.xsb), ulCrntOffset)) != NO_ERROR)
                    break;
                LongMetricsArray[j] = CurrLongMetric;
                ++j;
            }
            ulCrntOffset += sizeof(uint16);
        }
        if (errCode != NO_ERROR)
        {
            Mem_Free(LongMetricsArray);
            return errCode;
        }

        if (j != usDttfGlyphIndexCount)
        {
            Mem_Free(LongMetricsArray);
            return ERR_GENERIC;
        }
        /* first write out the long metrics */
        errCode = WriteGenericRepeat(pOutputBufferInfo,(uint8 *)LongMetricsArray, LONGXMETRIC_CONTROL,
                ulXmtxOffset,&ulBytesWritten, nNewLongMetrics, SIZEOF_LONGXMETRIC);
        /* then write out the short metrics */
        if (errCode == NO_ERROR)
        {
            ulCrntOffset = ulXmtxOffset + ulBytesWritten;
            for (i = nNewLongMetrics; i < usDttfGlyphIndexCount; ++i)
            {
                if ((errCode = WriteWord( pOutputBufferInfo, LongMetricsArray[i].xsb, ulCrntOffset)) != NO_ERROR)
                    break;
                ulCrntOffset += sizeof(uint16);
            }
        }
    
        Mem_Free(LongMetricsArray);
        if (errCode != NO_ERROR)
            return errCode;
        ulBytesWritten = ulCrntOffset - ulXmtxOffset;
    }
  
        /* write out our new, shorter length... cleanup comes later */
    errCode = UpdateDirEntry( pOutputBufferInfo, xmtx_tag, ulBytesWritten );

    if (errCode == NO_ERROR && nNewLongMetrics != XHea.numLongMetrics) 
    {
        XHea.numLongMetrics = nNewLongMetrics;      /* leave these alone if the hmtx table will remain the same */
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &XHea, SIZEOF_XHEA, XHEA_CONTROL, ulXheaOffset, &usBytesWritten )) != NO_ERROR)
            return (errCode);
    }
    *pulNewOutOffset = ulCrntOffset;

    return errCode;
}

/* ------------------------------------------------------------------- */
int16 ModMaxP( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
              TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
              uint32 *pulNewOutOffset)
{
MAXP MaxP;
uint32 ulOffset;
uint16 usnMaxComponents;
uint16 *pausComponents; 
int16 errCode;
uint16 usBytesWritten;

   /* get old maxp record */
    if ((ulOffset = GetMaxp( pOutputBufferInfo, &MaxP)) == 0L)
    { /* not copied over yet */
        if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, MAXP_TAG, pulNewOutOffset)) != NO_ERROR)
            return ERR_MISSING_MAXP; /* required */

        if ((ulOffset = GetMaxp( pOutputBufferInfo, &MaxP)) == 0L)
            return ERR_GENERIC;
    }

   /* recompute maxp info */
    /* figure a conservative maximum total possible. 3x3 at minimum */
    usnMaxComponents = max(3,MaxP.maxComponentElements) * max(3,MaxP.maxComponentDepth);
    pausComponents = (uint16 *) Mem_Alloc(usnMaxComponents * sizeof(uint16));
    if (pausComponents == NULL)
        return ERR_MEM;

    errCode = ComputeMaxPStats( pOutputBufferInfo, &(MaxP.maxContours), &(MaxP.maxPoints), &(MaxP.maxCompositeContours),
                     &(MaxP.maxCompositePoints), &(MaxP.maxSizeOfInstructions),
                     &(MaxP.maxComponentElements), &(MaxP.maxComponentDepth), pausComponents, usnMaxComponents  );
   /* write out new maxp record with new maxp info */
    Mem_Free(pausComponents);

    if (errCode == NO_ERROR)
        errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &MaxP, SIZEOF_MAXP, MAXP_CONTROL, ulOffset, &usBytesWritten );

    return errCode;
}

/* NOTE: This function will work fine even if the OS/2 table becomes updated. 
   The version value is preserved, and the length of the table is not modified */
/* ------------------------------------------------------------------- */
int16 ModOS2( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
             uint16 usMinChr, 
             uint16 usMaxChr,
              CONST uint16 usFormat,
              uint32 *pulNewOutOffset)
{
/* read OS2 table, modify the max,min char field, and write out the
new table */

NEWOS2  Os2;
uint32   ulOffset;
BOOL bNewOS2 = FALSE;
uint16 usBytesWritten;
int16 errCode = NO_ERROR;

    if (usFormat == TTFDELTA_DELTA) /* only formats for which this is not valid */
    {
        MarkTableForDeletion(pOutputBufferInfo, OS2_TAG);
        return errCode;
    }

    if ((ulOffset = GetSmartOS2(pOutputBufferInfo,&Os2,&bNewOS2)) == 0L)
    {
        if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, OS2_TAG, pulNewOutOffset)) != NO_ERROR)
        {
            if (errCode == ERR_FORMAT)
                return NO_ERROR;    /* not required */
            return errCode;
        }
        if ((ulOffset = GetSmartOS2(pOutputBufferInfo,&Os2,&bNewOS2)) == 0L)
            return ERR_GENERIC;
    }

    if (usMinChr != 0 || usMaxChr != 0) /* couldn't set in modcmap because of growth */
    {
        if (Os2.usFirstCharIndex < 0xF000)   /* lcp 5/26/97 don't want to change this if it is a Symbol font */
            Os2.usFirstCharIndex = usMinChr;
        Os2.usLastCharIndex  = usMaxChr;
        
        if( bNewOS2 )  /* write out the new one */
        {
            if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Os2, SIZEOF_NEWOS2, NEWOS2_CONTROL, ulOffset, &usBytesWritten)) != NO_ERROR)
                return errCode;
        }
        else     /* write out the old one */
        {
            if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Os2, SIZEOF_OS2, OS2_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
                return errCode;
        }
    }
    return errCode;
}

/* ------------------------------------------------------------------- */



/* ------------------------------------------------------------------- */
/* this function changes all Post tables to format 3.0 for space savings */
/* ------------------------------------------------------------------- */
#define POST_FORMAT_3 0x0030000
int16 ModPost( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
              TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
              CONST uint16 usFormat,
              uint32 *pulNewOutOffset)
{
POST    Post;
int16 errCode = NO_ERROR;
uint16 usBytesWritten;
uint32 ulOffset;

    /* verify table needs to be modified */

    if (usFormat == TTFDELTA_DELTA) /* only formats for which this is not valid */
    {
        MarkTableForDeletion(pOutputBufferInfo, POST_TAG);
        return errCode;
    }
    if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, POST_TAG, pulNewOutOffset)) != NO_ERROR)
    {
        if (errCode == ERR_FORMAT)
            return NO_ERROR;    /* not required */
        return errCode;
    }

    if ((ulOffset = GetPost( pOutputBufferInfo, &Post )) == 0L)
        return ERR_GENERIC;

    if ( Post.formatType != POST_FORMAT_3 )
    {
        /* Not POST format 3.0, so change it to 3.0 */
        Post.formatType = POST_FORMAT_3;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Post,SIZEOF_POST, POST_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
            return errCode;
        /* update the directory entry with new length */

        errCode = UpdateDirEntry( pOutputBufferInfo, POST_TAG, (uint32) usBytesWritten );
        *pulNewOutOffset = ulOffset + usBytesWritten;
    }
    return errCode;
}

/* ------------------------------------------------------------------- */
/* ModName definitions */
/* ------------------------------------------------------------------- */
/* will modify the name table thusly:
   look for name entries. If the entry is for a platform other than 3, or
   the entry is for platform 3 and the specified language, copy it to the
   output table. Otherwise don't copy it (delete it) to the table. 
   Must be certain that the end result is that there be at least 1 table with
   platform 3, if there were any to begin with.
   
/* since it is possible that string data may be shared among the Name records, 
we have to do a whole bunch of junk to avoid copying duplicate entries, and 
overwriting data we already have */

/* ------------------------------------------------------------------- */

int16 ModName( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
              TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
              CONST uint16 usLanguage,
              CONST uint16 usFormat,
              uint32 *pulNewOutOffset)
{
PNAMERECORD pNameRecordArray; /* internal representation of NameRecord - from ttftable.h */
uint16 NameRecordCount;
int16 errCode = NO_ERROR;
uint16 i;
uint16 bKeptMSPlatformRecord = FALSE; /* has a MS Platform record been kept? */
uint16 bDeleteStrings = FALSE; /* should we delete strings when writing out table? */
uint32 ulBytesWritten = 0;
uint32 ulNameOffset;
uint32 ulNameLength;
TTFACC_FILEBUFFERINFO NameTableBufferInfo; /* needed by WriteNameRecords */

    if (usFormat == TTFDELTA_DELTA) /* only formats for which this is not valid */
    {
        MarkTableForDeletion(pOutputBufferInfo, NAME_TAG);
        return errCode;
    }
    if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, NAME_TAG, pulNewOutOffset))!= NO_ERROR)
        return errCode; /*  required */

    ulNameOffset = TTTableOffset( pOutputBufferInfo, NAME_TAG);
    ulNameLength = TTTableLength( pOutputBufferInfo, NAME_TAG);

/* Get info about Name table */

    if ((errCode = ReadAllocNameRecords(pOutputBufferInfo, &pNameRecordArray, &NameRecordCount, Mem_Alloc, Mem_Free)) != NO_ERROR)
        return errCode;
    
    if (usLanguage != TTFSUB_LANG_KEEP_ALL)
    {
        for (i = 0; i < NameRecordCount; ++i)
        {
            if (pNameRecordArray[i].platformID == TTFSUB_MS_PLATFORMID) 
            {
                if (pNameRecordArray[i].languageID == usLanguage) /* we want to keep this one */
                    bKeptMSPlatformRecord = TRUE;
                else  /* we don't want it */
                {
                    pNameRecordArray[i].bDeleteString = TRUE;  /* mark it for deletion */
                    bDeleteStrings = TRUE;
                }
            }
        }
        if (bDeleteStrings && !bKeptMSPlatformRecord)    /* if we asked to keep a language that wasn't found, don't delete others */
            bDeleteStrings = FALSE;
    }
    /* now fake up a bufferinfo so that WriteNameRecords will write to the actual file buffer */
    InitFileBufferInfo(&NameTableBufferInfo, pOutputBufferInfo->puchBuffer + ulNameOffset, ulNameLength, NULL /*cant reallocate!*/);

    errCode = WriteNameRecords(&NameTableBufferInfo, pNameRecordArray, NameRecordCount, bDeleteStrings, TRUE, &ulBytesWritten);
    FreeNameRecords(pNameRecordArray, NameRecordCount, Mem_Free);

    if (errCode == NO_ERROR)
    {
        *pulNewOutOffset = ulNameOffset + ulBytesWritten;
        UpdateDirEntry(pOutputBufferInfo, NAME_TAG, ulBytesWritten);
    }
    else /* ran out of room? restore it */
    {
        *pulNewOutOffset = ulNameOffset;
        errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, NAME_TAG, pulNewOutOffset);
    }

    return errCode;
}
        
/* ------------------------------------------------------------------- */
PRIVATE int16 AdjustKernFormat0(TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
                        CONST uint8 *puchKeepGlyphList, 
                        CONST uint16 usGlyphListCount, 
                        KERN_SUB_HEADER   KernSubHeader,
                        uint32 ulOffset,
                        uint16 usSubHeaderSize, /* size in file of usSubHeader */
                        uint16 * pusNewLength)
{
uint32 ulSourceOffset;
uint32 ulTargetOffset;
KERN_FORMAT_0 KernFormat0;
KERN_PAIR KernPair;
uint16 usKernFormat0Size;
uint16 i;
uint16 usUsedPairs;
uint16 usSearchRange;
uint16 usRangeShift;
uint16 usBytesRead;
uint16 usBytesWritten;
int16 errCode;
                         
    /* determine number of kern pairs */
    ulSourceOffset = ulOffset + usSubHeaderSize;
    if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *)&KernFormat0, SIZEOF_KERN_FORMAT_0, KERN_FORMAT_0_CONTROL, ulSourceOffset, &usBytesRead )) != NO_ERROR)
        return errCode;
    usKernFormat0Size = usBytesRead;
    ulSourceOffset += usKernFormat0Size;
    ulTargetOffset = ulSourceOffset;

    /* wade through list of pairs, copying those that do not include a
    deleted glyph and ignoring those that do */

    usUsedPairs = 0;
    for ( i = 0; i < KernFormat0.nPairs; i++ )
    {
        if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &KernPair, SIZEOF_KERN_PAIR, KERN_PAIR_CONTROL, ulSourceOffset, &usBytesRead )) != NO_ERROR)
        return errCode;

        if (( KernPair.left < usGlyphListCount && puchKeepGlyphList[KernPair.left] ) &&
            ( KernPair.right < usGlyphListCount && puchKeepGlyphList[KernPair.right] ))
        {
            if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &KernPair, SIZEOF_KERN_PAIR, KERN_PAIR_CONTROL, ulTargetOffset, &usBytesWritten)) != NO_ERROR)
                return errCode;
            ulTargetOffset += usBytesWritten;
            usUsedPairs++;
        }
        ulSourceOffset += usBytesRead;
    }

    /* calc and write out revised subtable header */
    if (usUsedPairs > 0)
    {
        *pusNewLength = (uint16) (ulTargetOffset - ulOffset);
        KernSubHeader.length = *pusNewLength ;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &KernSubHeader, SIZEOF_KERN_SUB_HEADER, KERN_SUB_HEADER_CONTROL, ulOffset, &usBytesWritten )) != NO_ERROR)
            return errCode;

        /* calc and write out revised format 0 header */

        usSearchRange = (0x0001 << log2( usUsedPairs )) * GetGenericSize( KERN_PAIR_CONTROL );
        usRangeShift  = (usUsedPairs * GetGenericSize( KERN_PAIR_CONTROL )) - usSearchRange;
        KernFormat0.nPairs      = usUsedPairs;
        KernFormat0.searchRange =  usSearchRange;
        KernFormat0.entrySelector = log2( usUsedPairs );
        KernFormat0.rangeShift    = usRangeShift;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &KernFormat0, SIZEOF_KERN_FORMAT_0, KERN_FORMAT_0_CONTROL, ulOffset + usBytesWritten, &usBytesWritten )) != NO_ERROR)
            return errCode;
    }
    else
        *pusNewLength = 0;
    return NO_ERROR;
}

/* ------------------------------------------------------------------- */
int16 ModKern(  CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
              CONST uint8 *puchKeepGlyphList, 
              CONST uint16 usGlyphListCount,
              CONST uint16 usFormat,
              uint32 *pulNewOutOffset)
{
uint32 ulOffset;
uint32 ulSourceOffset;
uint32 ulTargetOffset;
KERN_HEADER KernHeader;
KERN_SUB_HEADER  KernSubHeader;
uint16 i;
uint16 usSubtableLength;
uint16 usBytesRead;
int16 errCode = NO_ERROR;
    /* read kern table header */

    
    if (usFormat == TTFDELTA_DELTA) /* only formats for which this is valid */
    {
        MarkTableForDeletion(pOutputBufferInfo, KERN_TAG);
        return NO_ERROR;
    }
    if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, KERN_TAG, pulNewOutOffset)) != NO_ERROR)  
    {
        if (errCode == ERR_FORMAT)
            return NO_ERROR;    /* not required */
        return errCode;
    }

    if (usFormat == TTFDELTA_SUBSET1)   /* need to keep the full kern table as we will send only once */
        return NO_ERROR;

    ulOffset = TTTableOffset( pOutputBufferInfo, KERN_TAG );
    if ( ulOffset == 0L )
        return ERR_GENERIC;    /* should have been copied over */
    if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &KernHeader, SIZEOF_KERN_HEADER, KERN_HEADER_CONTROL, ulOffset, &usBytesRead )) != NO_ERROR)
        return errCode;

    /* read each subtable.  If it is a format 0 subtable, remove
    kern pairs involving deleted glyphs.  Otherwise, copy
    the table down to its new location */

    ulSourceOffset = ulOffset + usBytesRead;
    ulTargetOffset = ulSourceOffset;
    for ( i = 0; i < KernHeader.nTables; i++ )
    {
        /* read subtable header */
        if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *) &KernSubHeader, SIZEOF_KERN_SUB_HEADER, KERN_SUB_HEADER_CONTROL, ulSourceOffset, &usBytesRead )) != NO_ERROR)
            return errCode;

        /* copy data to new location to cover any gaps left by shortening the previous
        format 0 subtable. Nothing happens first time around. */

        if ((errCode = CopyBlock( pOutputBufferInfo, ulTargetOffset, ulSourceOffset, KernSubHeader.length )) != NO_ERROR)
            return errCode;
        ulSourceOffset += KernSubHeader.length;

        /* if subtable is format 0, shorten it by deleting kern pairs
        involving deleted glyphs */

        if ( KernSubHeader.format == 0 )
        {
            if ((errCode = AdjustKernFormat0( pOutputBufferInfo, puchKeepGlyphList, usGlyphListCount, KernSubHeader, ulTargetOffset, usBytesRead, &usSubtableLength)) != NO_ERROR)
                return errCode;
            ulTargetOffset += usSubtableLength;
        }
        else
            ulTargetOffset += KernSubHeader.length;
    }

    /* Write out revised table length */
    if (ulTargetOffset == ulOffset + GetGenericSize( KERN_HEADER_CONTROL )) /* no Kern data written */
        MarkTableForDeletion(pOutputBufferInfo, KERN_TAG);
    else
        errCode = UpdateDirEntry( pOutputBufferInfo, KERN_TAG, ulTargetOffset - ulOffset );
    *pulNewOutOffset = ulTargetOffset;
    return errCode;
}

/* ------------------------------------------------------------------- */
/* clear out any unused glyphs. Calculate new maxWidth value for each device record */
/* assumes that hhea table has been updated with info for the modified hmtx table */
/* ------------------------------------------------------------------- */
int16 ModHdmx( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
              CONST uint8 *puchKeepGlyphList, 
              CONST uint16 usGlyphListCount,
              CONST uint16 usDttfGlyphIndexCount,
              uint32 *pulNewOutOffset)
{
HDMX Hdmx;
HDMX_DEVICE_REC DevRecord;
uint8 Width;
uint8 maxWidth;
uint16 i;
uint16 j,k;
uint32 ulHdmxOffset;
uint32 ulOffset;
uint32 ulDevOffset;
uint32 ulInOffset;
uint32 ulOutOffset;
uint32 ulInDevOffset;
uint32 ulOutDevOffset;
int16 errCode;
uint16 usBytesRead;
uint16 usBytesWritten;
uint32 ulOutSizeDeviceRecord;

    if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, HDMX_TAG, pulNewOutOffset)) != NO_ERROR)
    {
        if (errCode == ERR_FORMAT)
            return NO_ERROR;    /* not required */
        return errCode;
    }

    ulHdmxOffset = GetHdmx(pOutputBufferInfo, &Hdmx);
    if ( !ulHdmxOffset )
        return ERR_GENERIC;

    ulOffset = ulHdmxOffset + GetGenericSize( HDMX_CONTROL );

    if (usDttfGlyphIndexCount) /* we want compact form */
    {
        ulInOffset = ulOutOffset = ulOffset;
        ulOutSizeDeviceRecord = RoundToLongWord(GetGenericSize(HDMX_DEVICE_REC_CONTROL) + (sizeof(uint8) * usDttfGlyphIndexCount));
        for( j = 0; j < Hdmx.numDeviceRecords; j++)
        {
            ulInDevOffset = ulInOffset;
            ulOutDevOffset = ulOutOffset;
            if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *)&DevRecord, SIZEOF_HDMX_DEVICE_REC, HDMX_DEVICE_REC_CONTROL, ulInDevOffset, &usBytesRead )) != NO_ERROR)
                return errCode;
            ulInOffset += usBytesRead;
            ulOutOffset += usBytesRead;
            maxWidth = 0;

            for(i = 0, k= 0; i < usGlyphListCount && k < usDttfGlyphIndexCount; i++) /* process each glyph entry */
            {
                if (puchKeepGlyphList[ i ])
                {
                    if ((errCode = ReadByte( pOutputBufferInfo, &Width, ulInOffset)) != NO_ERROR)
                        return errCode;
                    maxWidth = max( maxWidth, Width );
                    if ((errCode = WriteByte( pOutputBufferInfo, Width, ulOutOffset)) != NO_ERROR)
                        return errCode;
                    ulOutOffset += sizeof(uint8);
                    ++k;
                }
                ulInOffset += sizeof(uint8);
            }
            if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, ulOutOffset, &ulOutOffset)) != NO_ERROR)
                return errCode;
            DevRecord.maxWidth = maxWidth;
            if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *)&DevRecord, SIZEOF_HDMX_DEVICE_REC, HDMX_DEVICE_REC_CONTROL, ulOutDevOffset, &usBytesWritten )) != NO_ERROR)
                return errCode;
            ulInOffset = ulInDevOffset + Hdmx.sizeDeviceRecord;
            ulOutOffset = ulOutDevOffset + ulOutSizeDeviceRecord;
        }
        /* now need to update hdmx record */
        Hdmx.sizeDeviceRecord = ulOutSizeDeviceRecord;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *)&Hdmx, SIZEOF_HDMX, HDMX_CONTROL, ulHdmxOffset, &usBytesWritten )) != NO_ERROR)
            return errCode;
        if ((errCode = UpdateDirEntry( pOutputBufferInfo, HDMX_TAG, ulOutOffset - ulHdmxOffset )) != NO_ERROR)
            return errCode;
        *pulNewOutOffset = ulOutOffset;
    }
    else
    {

    /*      if (GetHHea(pOutputBufferInfo, &Hhea) == 0L)
           return ERR_FORMAT;  */

        for( j = 0; j < Hdmx.numDeviceRecords; j++)
        {
            ulDevOffset = ulOffset;
            if ((errCode = ReadGeneric( pOutputBufferInfo, (uint8 *)&DevRecord, SIZEOF_HDMX_DEVICE_REC, HDMX_DEVICE_REC_CONTROL, ulDevOffset, &usBytesRead )) != NO_ERROR)
                return errCode;
            ulOffset += usBytesRead;
            maxWidth = 0;

            for(i = 0; i < usGlyphListCount; i++) /* process each glyph entry */
            {
                if (puchKeepGlyphList[ i ])
                {
                    if ((errCode = ReadByte( pOutputBufferInfo, &Width, ulOffset)) != NO_ERROR)
                        return errCode;
                    maxWidth = max( maxWidth, Width );
                }
                else /* if (i != Hhea.numLongMetrics-1) clear the value in the file, so the compressor can do its work, except for any dummy entries in the hmtx table */
                {
                    if ((errCode = WriteByte( pOutputBufferInfo, (uint8) 0, ulOffset)) != NO_ERROR)
                        return errCode;
                }
                ulOffset += sizeof(uint8);
            }
            if (DevRecord.maxWidth != maxWidth) /* it's changed, we need to write it out again */
            {
                DevRecord.maxWidth = maxWidth;
                if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *)&DevRecord, SIZEOF_HDMX_DEVICE_REC, HDMX_DEVICE_REC_CONTROL, ulDevOffset, &usBytesWritten )) != NO_ERROR)
                    return errCode;
            }
            ulOffset = ulDevOffset + Hdmx.sizeDeviceRecord;
        }
        *pulNewOutOffset = ulOffset;
    }

    return NO_ERROR;
}


/* ------------------------------------------------------------------- */
/* Zero out any unused glyphs */
/* ------------------------------------------------------------------- */
int16 ModLTSH(  CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
              CONST uint8 *puchKeepGlyphList, 
              CONST uint16 usGlyphListCount,
              CONST uint16 usDttfGlyphIndexCount,
              uint32 *pulNewOutOffset)
{
LTSH  Ltsh;
uint32 ulLtshOffset;
uint32 ulInOffset;
uint32 ulOutOffset;
uint16 i,j;
int16 errCode;
uint16 GlyphCount;
uint8 uchValue;
uint16 usBytesWritten;

   /* read ltsh table header */

  if ((errCode = CopyTableOver(pOutputBufferInfo, pInputBufferInfo, LTSH_TAG, pulNewOutOffset)) != NO_ERROR)
  {
    if (errCode == ERR_FORMAT)
        return NO_ERROR;    /* not required */
    return errCode;
  }
  ulLtshOffset = GetLTSH( pOutputBufferInfo, &Ltsh );
   if ( ulLtshOffset == 0 )
      return ERR_GENERIC;

   if (usDttfGlyphIndexCount)
   {
       ulOutOffset = ulLtshOffset + GetGenericSize( LTSH_CONTROL );
       ulInOffset = ulLtshOffset + GetGenericSize( LTSH_CONTROL );
       GlyphCount = min(Ltsh.numGlyphs, usGlyphListCount); /* don't want to process too many if file is buggy */

        for( i=0, j= 0; i < GlyphCount && j < usDttfGlyphIndexCount; i++)
        {
            if (puchKeepGlyphList[ i ]) /* need keep this one out */
            {
                if ((errCode = ReadByte( pOutputBufferInfo, (uint8 *) &uchValue, ulInOffset)) != NO_ERROR)
                    return errCode;

                if ((errCode = WriteByte( pOutputBufferInfo, (uint8) uchValue, ulOutOffset)) != NO_ERROR)
                    return errCode;
                ulOutOffset += sizeof(uint8);
                ++j;
            }
            ulInOffset += sizeof(uint8);
        }
        /* now we need to update the count for the LTSH table */
        Ltsh.numGlyphs = usDttfGlyphIndexCount;
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *)&Ltsh, SIZEOF_LTSH, LTSH_CONTROL, ulLtshOffset, &usBytesWritten )) != NO_ERROR)
            return errCode;
        if ((errCode = UpdateDirEntry( pOutputBufferInfo, LTSH_TAG, ulOutOffset - ulLtshOffset )) != NO_ERROR)
            return errCode;
   }
   else
   {

       ulOutOffset = ulLtshOffset + GetGenericSize( LTSH_CONTROL );
       GlyphCount = min(Ltsh.numGlyphs, usGlyphListCount); /* don't want to process too many if file is buggy */

        for( i=0; i < GlyphCount; i++)
        {
            if (!puchKeepGlyphList[ i ])    /* need to zero out */
            {
                if ((errCode = WriteByte( pOutputBufferInfo, (uint8) 0, ulOutOffset)) != NO_ERROR)
                    return errCode;
            }
            ulOutOffset += sizeof(uint8);
        }
   }
   *pulNewOutOffset = ulOutOffset;
    return NO_ERROR;
}

/* ---------------------------------------------------------------------- */
/* Greatest Common Denominator */
/* recursive !! */
/* ---------------------------------------------------------------------- */
PRIVATE uint16 GCD (uint16 u, uint16 v)
{
    if (v == 0) 
        return (u);
    else 
        return GCD(v, (uint16) (u % v));
}
/* ---------------------------------------------------------------------- */
void ReduceRatio(uint16 *px, uint16 *py)
{
uint16 gcd; 

        gcd = GCD(*px, *py);
        if (gcd > 0) /* should never return 0, but check just in case */
        {
            *px = (*px) / gcd;
            *py = (*py) / gcd;
        }   
}
/* ------------------------------------------------------------------- */
typedef struct {
    uint16 usOldGroupOffset;
    uint16 usNewGroupOffset;
} GroupOffsetRecord;

/* ------------------------------------------------------------------- */
typedef struct groupoffsetrecordkeeper *PGROUPOFFSETRECORDKEEPER;    
typedef struct groupoffsetrecordkeeper GROUPOFFSETRECORDKEEPER;  

struct groupoffsetrecordkeeper    /* housekeeping structure */
{
    __field_ecount(usGroupOffsetArrayLen)     GroupOffsetRecord *pGroupOffsetArray;
                                              uint16             usGroupOffsetArrayLen;
    __field_range(0, usGroupOffsetArrayLen)   uint16             usNextArrayIndex;
};

/* ------------------------------------------------------------------- */
PRIVATE int16 InitGroupOffsetArray(PGROUPOFFSETRECORDKEEPER pKeeper, 
                                  uint16 usRecordCount)
{
    pKeeper->pGroupOffsetArray = (GroupOffsetRecord *) Mem_Alloc(usRecordCount * sizeof(*(pKeeper->pGroupOffsetArray)));
    if (pKeeper->pGroupOffsetArray == NULL)
        return ERR_MEM;
    pKeeper->usGroupOffsetArrayLen = usRecordCount;
    pKeeper->usNextArrayIndex = 0;
    return NO_ERROR;
}
/* ------------------------------------------------------------------- */
PRIVATE void FreeGroupOffsetArray(PGROUPOFFSETRECORDKEEPER pKeeper)
{
    Mem_Free(pKeeper->pGroupOffsetArray);
    pKeeper->pGroupOffsetArray = NULL;
    pKeeper->usGroupOffsetArrayLen = 0;
    pKeeper->usNextArrayIndex = 0;
}
/* ------------------------------------------------------------------- */
PRIVATE int16 RecordGroupOffset(PGROUPOFFSETRECORDKEEPER pKeeper, 
                                uint16 usOldGroupOffset,
                                uint16 usNewGroupOffset)
  /* record this block as being used */
{
    if (pKeeper->usNextArrayIndex >= pKeeper->usGroupOffsetArrayLen)
        return ERR_FORMAT;
    pKeeper->pGroupOffsetArray[pKeeper->usNextArrayIndex].usOldGroupOffset = usOldGroupOffset;
    pKeeper->pGroupOffsetArray[pKeeper->usNextArrayIndex].usNewGroupOffset = usNewGroupOffset ;
    ++pKeeper->usNextArrayIndex;
    return NO_ERROR;
}

/* ------------------------------------------------------------------- */
PRIVATE uint16 LookupGroupOffset(PGROUPOFFSETRECORDKEEPER pKeeper, 
                                uint16 usOldGroupOffset)
{
uint16 i;

    for (i = 0; i < pKeeper->usNextArrayIndex; ++i)
    {
        if (usOldGroupOffset == pKeeper->pGroupOffsetArray[i].usOldGroupOffset)
            return(pKeeper->pGroupOffsetArray[i].usNewGroupOffset);
    }
    return(0);
}
/* ------------------------------------------------------------------- */
#define EGA_X_RATIO 4
#define EGA_Y_RATIO 3   
/* ---------------------------------------------------------------------- */
/* need both input and output buffer info, because we don't want to overwrite blocks of data if not written in order */
/* need to remove 4:3 ratio and 0:0 ration (if a 1:1 already exists) */
/* don't have to copy the data over from the inputbuffer, as this function reads directly from there */
/* ------------------------------------------------------------------- */
int16 ModVDMX(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
              TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
              CONST uint16 usFormat,
              uint32 *pulNewOutOffset)
{
VDMX Vdmx;
uint32 ulSrcOffset; /* offset to Src VDMX table */
uint32 ulSrcLength; /* length of Src VDMX table */
uint32 ulDestOffset; /* offset to Dest VDMX table */
uint32 ulDestLength; /* length of Dest VDMX table */
uint32 ulSrcOffsetRatios;   /* absolute offset to Ratios in Src file */
uint32 ulSrcOffsetOffsets;  /* absolute offset to Offsets in Src file */
uint32 ulSrcOffsetGroups;   /* absolute offset to Groups in Src file */
uint32 ulDestOffsetRatios;  /* absolute offset to Ratios in Dest file */
uint32 ulDestOffsetOffsets; /* absolute offset to Offsets in Dest file */
uint32 ulDestOffsetGroups;  /* absolute offset to Groups in Dest file */
uint16 usCurrGroupSrcOffset; /* relative offset from beginning of VDMX table */
uint16 usCurrGroupDestOffset; /* relative offset from beginning of VDMX table */
uint32 ulCurrGroupDestOffset=0; /* relative offset from beginning of VDMX table - long value */
uint16 usGroupDestOffset; /* relative offset from beginning of VDMX table, local copy for writing */
uint16 usSrcRatioIndex,usDestRatioIndex;
uint16 i;
uint16 usBytesRead;
uint16 usBytesWritten;
uint32 ulBytesRead;
int16 errCode=NO_ERROR;
VDMXRatio *SrcRatioArray=NULL;
int8 *KeepSrcRatioArray = NULL;   /* parallel array to SrcRatioArray */
VDMXGroup GroupHeader;
uint8 * pGroupBuffer=NULL;
uint32 ulGroupBufferLength; /* total length of the Group Buffer (from source file) */
uint32 ulGroupLength; /* length of individual group to be read */
uint16 usGroupCount = 0; 
uint16 usKeepRatioCount = 0;
uint16 usRatioSize;
uint16 xRatio, yRatio; /* for reducing the ratios */
int16 Found1to1; 
GROUPOFFSETRECORDKEEPER keeper; 
TTFACC_FILEBUFFERINFO * pUnCONSTInputBufferInfo;

    if (usFormat == TTFDELTA_DELTA)  /* only formats for which this is not valid */
    {
        MarkTableForDeletion(pOutputBufferInfo, VDMX_TAG);
        return errCode;
    }

    pUnCONSTInputBufferInfo = (TTFACC_FILEBUFFERINFO *) pInputBufferInfo; /* used for Read functions ONLY. Not for Write */
/* get input buffer information */
    ulSrcOffset = TTTableOffset( pUnCONSTInputBufferInfo, VDMX_TAG );
    if ( ulSrcOffset == 0L )
        return NO_ERROR;
    ulSrcLength = TTTableLength( pUnCONSTInputBufferInfo, VDMX_TAG );
    if ( ulSrcLength == 0L )
    {
        MarkTableForDeletion(pOutputBufferInfo, VDMX_TAG);
        return NO_ERROR;
    }
    /* get output buffer information */
    if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, *pulNewOutOffset, &ulDestOffset)) != NO_ERROR)
        return errCode;

    if ((errCode = ReadGeneric( pUnCONSTInputBufferInfo, (uint8 *) &Vdmx, SIZEOF_VDMX, VDMX_CONTROL, ulSrcOffset, &usBytesRead )) != NO_ERROR)
        return errCode;
    if (Vdmx.numRatios == 0)
    {
        MarkTableForDeletion(pOutputBufferInfo, VDMX_TAG);
        return NO_ERROR;
    }


    ulSrcOffsetRatios = ulSrcOffset + usBytesRead;
    ulSrcOffsetOffsets = ulSrcOffsetRatios + GetGenericSize(VDMXRATIO_CONTROL) * Vdmx.numRatios;
    ulSrcOffsetGroups = ulSrcOffsetOffsets + sizeof(uint16) * Vdmx.numRatios;
    memset(&keeper, 0, sizeof(keeper));

    SrcRatioArray = (VDMXRatio *)Mem_Alloc(Vdmx.numRatios * sizeof(VDMXRatio));
    if (SrcRatioArray == NULL)
        errCode = ERR_MEM;
    else
    {
        KeepSrcRatioArray = (int8 *)Mem_Alloc(Vdmx.numRatios * sizeof(int8));
        if (KeepSrcRatioArray == NULL)
            errCode = ERR_MEM;
        else
            errCode = ReadGenericRepeat(pUnCONSTInputBufferInfo, (uint8 *) SrcRatioArray, VDMXRATIO_CONTROL, ulSrcOffsetRatios, &ulBytesRead, Vdmx.numRatios, SIZEOF_VDMXRATIO );
    }

    while (errCode == NO_ERROR)     /* while is so we can break out. Only go once through */
    {
        Found1to1 = FALSE;
        for (i = 0; i < Vdmx.numRatios ; ++i)    /* keep all 1:1 aspect ratios */
        {
            KeepSrcRatioArray[i] = 1; /* assume we'll keep it */
            xRatio = SrcRatioArray[i].xRatio;
            yRatio = SrcRatioArray[i].yStartRatio;
            ReduceRatio(&xRatio,&yRatio);
            if (xRatio == yRatio)
            {
                if (SrcRatioArray[i].xRatio == 0)   /* anything after 0:0 is ignored */
                {
                    if (!Found1to1) /* need to keep this one */
                        ++usKeepRatioCount;
                    break;
                }
                if (Found1to1)  /* already have one */
                    KeepSrcRatioArray[i] = 0;  /* don't keep this one */
                else
                {
                    Found1to1 = TRUE;
                    ++usKeepRatioCount;
                }
            }
            else if (xRatio == EGA_X_RATIO && yRatio == EGA_Y_RATIO)
                KeepSrcRatioArray[i] = 0;  /* don't keep this one */
            else
                ++usKeepRatioCount;
        }

        if ((usKeepRatioCount == 0) || (usKeepRatioCount == Vdmx.numRatios))
        {                       /* don't change a thing */
            Mem_Free(SrcRatioArray);
            Mem_Free(KeepSrcRatioArray);
            return CopyTableOver(pOutputBufferInfo, pInputBufferInfo, VDMX_TAG, pulNewOutOffset);
        }
        ulDestOffsetRatios = ulDestOffset + usBytesRead;
        /* figure out offset for the Offset array */
        ulDestOffsetOffsets = ulDestOffsetRatios + GetGenericSize(VDMXRATIO_CONTROL) * usKeepRatioCount;
        ulDestOffsetGroups = ulDestOffsetOffsets + sizeof(uint16) * usKeepRatioCount;
        usRatioSize = GetGenericSize(VDMXRATIO_CONTROL);
        ulCurrGroupDestOffset = ulDestOffsetGroups - ulDestOffset; /* calculate offset from start of VDMX table */
        if ((errCode = InitGroupOffsetArray(&keeper,usKeepRatioCount)) != NO_ERROR)  /* initialize structure to track offset re-use */ 
            break;
        ulGroupBufferLength = ulSrcLength - (ulSrcOffsetGroups - ulSrcOffset);  /* calculate the length of the group section */
        pGroupBuffer = (uint8 *)Mem_Alloc(ulGroupBufferLength); /* allocate buffer the size of the group buffer */
        if (pGroupBuffer == NULL)
        {
            errCode = ERR_MEM;
            break;
        }
        
        for (usSrcRatioIndex = usDestRatioIndex = 0; usSrcRatioIndex < Vdmx.numRatios && usDestRatioIndex < usKeepRatioCount; ++usSrcRatioIndex)     /* keep all 1:1 aspect ratios */
        {
            if (KeepSrcRatioArray[usSrcRatioIndex] == 1)
            {
                /* write out the Ratio to the proper location */
                if ((errCode = WriteGeneric(pOutputBufferInfo, (uint8 *) &(SrcRatioArray[usSrcRatioIndex]), SIZEOF_VDMXRATIO, VDMXRATIO_CONTROL, ulDestOffsetRatios + (usDestRatioIndex * usRatioSize), &usBytesWritten)) != NO_ERROR)
                    break;
                /* now read the offset to the group */
                if ((errCode = ReadWord(pUnCONSTInputBufferInfo, &usCurrGroupSrcOffset, ulSrcOffsetOffsets + (usSrcRatioIndex * sizeof(uint16)) )) != NO_ERROR)
                    break;
                /* check if offset already used */
                if ((usGroupDestOffset = LookupGroupOffset(&keeper, usCurrGroupSrcOffset)) == 0)  /* not there already */
                {
                    if (ulCurrGroupDestOffset > USHRT_MAX)  /* check if will fit in unsigned short */
                    {
                        errCode = ERR_INVALID_VDMX;
                        break;
                    }
                    usCurrGroupDestOffset = (uint16) ulCurrGroupDestOffset;  /* already checked if in range */
                    /* need to register the old and new group offsets */
                    if ((errCode = RecordGroupOffset(&keeper, usCurrGroupSrcOffset, usCurrGroupDestOffset)) != NO_ERROR)
                        break;

                    usGroupDestOffset = usCurrGroupDestOffset;
                    /* need to copy the group data over */
                    if ((errCode = ReadGeneric(pUnCONSTInputBufferInfo, (uint8 *) &GroupHeader, SIZEOF_VDMXGROUP, VDMXGROUP_CONTROL, ulSrcOffset + usCurrGroupSrcOffset, &usBytesRead)) != NO_ERROR)
                        break;
 
                    ulGroupLength =  usBytesRead + (GroupHeader.recs * GetGenericSize(VDMXVTABLE_CONTROL));
                    /* read the group data into a buffer */
                    if (ulGroupLength > ulGroupBufferLength)
                    {
                        errCode = ERR_INVALID_VDMX; /* error in data! */
                        break;
                    }
                    if ((errCode = ReadBytes(pUnCONSTInputBufferInfo, (uint8 *) pGroupBuffer, ulSrcOffset + usCurrGroupSrcOffset, ulGroupLength)) != NO_ERROR)
                        break;
                    /* and write them to the output buffer */
                    if ((errCode = WriteBytes(pOutputBufferInfo, (uint8 *) pGroupBuffer, ulDestOffset + usCurrGroupDestOffset, ulGroupLength)) != NO_ERROR)
                        break;
                    ++usGroupCount;
                    /* increment our CurrGroupDestOffset value for next time around */
                    ulCurrGroupDestOffset = usCurrGroupDestOffset + ulGroupLength;
                }
                /* now write out that relative offset value */
                if ((errCode = WriteWord(pOutputBufferInfo, usGroupDestOffset, ulDestOffsetOffsets + (usDestRatioIndex * sizeof(uint16)))) != NO_ERROR)
                    break;

                ++usDestRatioIndex; /* increment in dest array */
            }
        }
        break; /* out of while */
    }
    if (errCode == NO_ERROR)
    {
        Vdmx.numRatios = usKeepRatioCount;
        Vdmx.numRecs = usGroupCount;
        errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Vdmx, SIZEOF_VDMX, VDMX_CONTROL, ulDestOffset, &usBytesWritten );
    }
    
    if (errCode == NO_ERROR)
    {
        ulDestLength = ulCurrGroupDestOffset; /* this is the size of the table */
        errCode = UpdateDirEntryAll( pOutputBufferInfo, VDMX_TAG, ulDestLength, ulDestOffset );
        *pulNewOutOffset = ulDestOffset + ulDestLength;
    }
 
    FreeGroupOffsetArray(&keeper);  /* free up structure to track offset re-use */ 

    Mem_Free(pGroupBuffer);

    Mem_Free(KeepSrcRatioArray);
    Mem_Free(SrcRatioArray);
    return errCode;
}

/* ------------------------------------------------------------------- */

