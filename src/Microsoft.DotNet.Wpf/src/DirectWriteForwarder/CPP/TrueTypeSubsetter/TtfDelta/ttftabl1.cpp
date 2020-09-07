// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFTABL1.C
 *
 *
 * aRoutines to read TrueType tables and table information from 
 * a TrueType file buffer
 *
 **************************************************************************/


/* Inclusions ----------------------------------------------------------- */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef _MAC
#include <Memory.h>
#endif // _MAC

#include "typedefs.h"
#include "ttff.h"                       /* TrueType font file def's */
#include "ttfacc.h"
#include "ttftabl1.h"
#include "ttfcntrl.h"
#include "ControlTableInit.h"

/* if the _INDEX defines are changed, the Control_Table array below must be updated to match */

#define HEAD_INDEX  0
#define HHEA_INDEX  1
#define VHEA_INDEX  2
#define MAXP_INDEX  3
#define POST_INDEX  4
#define OS2_INDEX   5
#define NEWOS2_INDEX 6
#define VERSION2OS2_INDEX 7
#define HDMX_INDEX  8
#define LTSH_INDEX  9

#define TAG_INDEX_COUNT 10  /* this is tied to the list of _INDEX #defines above */

typedef struct CONTROL_TABLE {
    char * Tag;
    uint16 StructSize;
    uint8  *Control;
} CONTROL_TABLE;

/* Definitions local to this file ---------------------------------------- */

/* if the Control_Table array is changed, the _INDEX defines above must be updated to match */

static CONTROL_TABLE Control_Table[TAG_INDEX_COUNT];  

// We use this method to initialize some global data. This is done because if we left
// the global initializations to the compiler it will generate some static methods that
// are not properly annotated with security tags. This was causing these complier generated 
// methods to fail NGEN and be Jitted causing significant startup perf regressions.
// This method has to be made SecurityCritical so that NGEN can process it!
// It contains safe code.
void ControlTableInit::Init()
{
    if (!_isInitialized)
    {
        System::Threading::Monitor::Enter(_staticLock);
        try
        {
            if (!_isInitialized)
            {
                int i = 0;
                
                Control_Table[i].Tag        = HEAD_TAG;
                Control_Table[i].StructSize = SIZEOF_HEAD;
                Control_Table[i].Control    = HEAD_CONTROL;

                i++;
                Control_Table[i].Tag        = HHEA_TAG;
                Control_Table[i].StructSize = SIZEOF_HHEA;
                Control_Table[i].Control    = HHEA_CONTROL;

                i++;
                Control_Table[i].Tag        = VHEA_TAG;
                Control_Table[i].StructSize = SIZEOF_VHEA;
                Control_Table[i].Control    = VHEA_CONTROL;

                i++;
                Control_Table[i].Tag        = MAXP_TAG;
                Control_Table[i].StructSize = SIZEOF_MAXP;
                Control_Table[i].Control    = MAXP_CONTROL;

                i++;
                Control_Table[i].Tag        = POST_TAG;
                Control_Table[i].StructSize = SIZEOF_POST;
                Control_Table[i].Control    = POST_CONTROL;

                i++;
                Control_Table[i].Tag        = OS2_TAG;
                Control_Table[i].StructSize = SIZEOF_OS2;
                Control_Table[i].Control    = OS2_CONTROL;

                i++;
                Control_Table[i].Tag        = OS2_TAG;
                Control_Table[i].StructSize = SIZEOF_NEWOS2;
                Control_Table[i].Control    = NEWOS2_CONTROL;

                i++;
                Control_Table[i].Tag        = OS2_TAG;
                Control_Table[i].StructSize = SIZEOF_VERSION2OS2;
                Control_Table[i].Control    = VERSION2OS2_CONTROL;

                i++;
                Control_Table[i].Tag        = HDMX_TAG;
                Control_Table[i].StructSize = SIZEOF_HDMX;
                Control_Table[i].Control    = HDMX_CONTROL;

                i++;
                Control_Table[i].Tag        = LTSH_TAG;
                Control_Table[i].StructSize = SIZEOF_LTSH;
                Control_Table[i].Control    = LTSH_CONTROL;
                _isInitialized = true;
            }
        }
        finally 
        {
            System::Threading::Monitor::Exit(_staticLock);
        }
    }
}

/* ---------------------------------------------------------------------- */
void ConvertLongTagToString(uint32 ulTag, __in_bcount(5) char *szTag)     /* convert a tag, as it has been read from the font, to a string */
{
uint32 ulSwappedTag;

    ulSwappedTag = SWAPL(ulTag); /* on intel, this will swap back to motorola. On motorola, this will leave alone */
    memcpy(szTag, (char *) &ulSwappedTag, 4);
    szTag[4] = '\0';
}
/* ---------------------------------------------------------------------- */
void ConvertStringTagToLong(__in_bcount(4) const char *szTag, uint32 *pulTag)
{
        memcpy((char *)pulTag, szTag, 4); 
        *pulTag = SWAPL(*pulTag); /* on intel, this will swap back to intel format. On motorola, this will leave alone */
}
/* functions to read font file data ------------------------------------- */
/* ---------------------------------------------------------------------- */
uint32 TTDirectoryEntryOffset( 
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char * szTagName 
)
{
uint16 usBytesRead;
OFFSET_TABLE Offset_Table;
DIRECTORY Directory;
uint32 ulCurrOffset = pInputBufferInfo->ulOffsetTableOffset;
uint16 i;
BOOL bFound = FALSE;
const uint32 *pulTag = (const uint32 *) szTagName;

   /* read offset table to determine number of tables in file. */

    if (ReadGeneric(pInputBufferInfo, (uint8 *) &Offset_Table, SIZEOF_OFFSET_TABLE, OFFSET_TABLE_CONTROL, ulCurrOffset, &usBytesRead) != 0)
        return(DIRECTORY_ENTRY_OFFSET_ERR);
    ulCurrOffset += usBytesRead; 

   /* read table directory until proper tag is found, or until
      all tags have been read */
    for (i = 0; i < Offset_Table.numTables; ++i)  /* don't want any translation done - read raw data */
    { 
        if (ReadGeneric(pInputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_NO_XLATE_CONTROL, ulCurrOffset, &usBytesRead) != 0)
            return(DIRECTORY_ENTRY_OFFSET_ERR);
        bFound = ( *pulTag == Directory.tag );
        if (bFound)
            break;
        ulCurrOffset += usBytesRead; 
    }

   if ( ! bFound )
      return( DIRECTORY_ERROR );
   return( ulCurrOffset );

}


/* ---------------------------------------------------------------------- */
uint32 GetTTDirectory( 
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char * szTagName,
    DIRECTORY * pDirectory )
{
uint16 usBytesRead;
uint32 ulOffset;

    ulOffset = TTDirectoryEntryOffset( pInputBufferInfo, szTagName );
    if ( ulOffset == DIRECTORY_ERROR || ulOffset == DIRECTORY_ENTRY_OFFSET_ERR)
        return( DIRECTORY_ERROR );

    if (ReadGeneric(pInputBufferInfo, (uint8 *) pDirectory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesRead) != NO_ERROR)
        return ( DIRECTORY_ERROR );
    return( ulOffset );

} /* ReadTTDirectory() */


/* ---------------------------------------------------------------------- */
uint32 TTTableLength(
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char *  szTagName )
{
DIRECTORY Directory;

    if ( GetTTDirectory( pInputBufferInfo, szTagName, &Directory ) != DIRECTORY_ERROR)
        return( Directory.length );
    return( DIRECTORY_ERROR );
}


/* ---------------------------------------------------------------------- */
uint32 TTTableOffset( 
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char * szTagName )
{
    DIRECTORY Directory;

    if ( GetTTDirectory( pInputBufferInfo, szTagName, &Directory ) != DIRECTORY_ERROR)
        return( Directory.offset );

    return( DIRECTORY_ERROR );
}
/* this function calculates the checksum of a table already written to the buffer */
/* ---------------------------------------------------------------------- */
uint32 TTTableChecksum(
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char * szTagName,
    uint32 * pulChecksum )
{
uint32 ul;
uint32 ulWord;
uint32 ulOffset;
uint32 ulLength;

    *pulChecksum = 0;
    if ((ulOffset = TTTableOffset( pInputBufferInfo, szTagName ))== DIRECTORY_ERROR)
        return DIRECTORY_ERROR;
    if ((ulLength = TTTableLength( pInputBufferInfo, szTagName ))== DIRECTORY_ERROR)
        return DIRECTORY_ERROR;

    for ( ul = 0; ul < (ulLength+3) / 4; ul++ )
    {
        if ( ReadLong( pInputBufferInfo, &ulWord, ulOffset + ul * sizeof(uint32)) != 0 )
            break;
        *pulChecksum = *pulChecksum + ulWord;
    }
    return ulOffset; /* any non zero number will do */
}


/* ---------------------------------------------------------------------- */
/* calcs the new checksum */
/* ---------------------------------------------------------------------- */
int16 UpdateChecksum( 
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char* szDirTag)
{
DIRECTORY Directory;
uint32 ulOffset;
uint16 usBytesMoved;
int16 errCode;

    /* read existing directory entry */

    ulOffset = GetTTDirectory( pInputBufferInfo, szDirTag, &Directory );
    if ( ulOffset == DIRECTORY_ERROR)
        return NO_ERROR;

    if ((errCode = CalcChecksum( pInputBufferInfo, Directory.offset, Directory.length, &Directory.checkSum )) != NO_ERROR)
        return errCode;

    /* write new directory entry with new checksum */

    if ((errCode = WriteGeneric( pInputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesMoved )) != NO_ERROR)
        return errCode;
    return NO_ERROR;
}

/* ---------------------------------------------------------------------- */
/* sets the new length, calcs the new checksum, makes sure offset on long word boundary */
/* ---------------------------------------------------------------------- */
int16 UpdateDirEntry(
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char *  szDirTag,
    uint32   ulNewLength )
{
DIRECTORY Directory;
uint32 ulOffset;
uint16 usBytesMoved;
int16 errCode;

    /* read existing directory entry */

    ulOffset = GetTTDirectory( pInputBufferInfo, szDirTag, &Directory );
    if ( ulOffset == DIRECTORY_ERROR)
        return NO_ERROR;
    /* set new length and recalc checksum */

    Directory.length = ulNewLength;
    if ((errCode = ZeroLongWordGap( pInputBufferInfo, Directory.offset, Directory.length, NULL)) != NO_ERROR)
        return errCode;

    if ((errCode = CalcChecksum( pInputBufferInfo, Directory.offset, Directory.length, &Directory.checkSum )) != NO_ERROR)
        return errCode;

    /* write new directory entry with new checksum */

    if ((errCode = WriteGeneric( pInputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesMoved )) != NO_ERROR)
        return errCode;
        
    return NO_ERROR;
}

/* ---------------------------------------------------------------------- */
/* sets the new length, calcs the new checksum, makes sure offset on long word boundary */
/* ---------------------------------------------------------------------- */
int16 UpdateDirEntryAll(
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char *  szDirTag,
    uint32 ulNewLength,
    uint32 ulNewOffset
)
{
DIRECTORY Directory;
uint32 ulOffset;
uint16 usBytesMoved;
int16 errCode;

    /* read existing directory entry */

    ulOffset = GetTTDirectory( pInputBufferInfo, szDirTag, &Directory );
    /* set new length and recalc checksum */

    Directory.length = ulNewLength;

    if ((errCode = ZeroLongWordAlign( pInputBufferInfo, ulNewOffset, &(Directory.offset))) != NO_ERROR)
    {
        return errCode;
    }

    if ((errCode = CalcChecksum( pInputBufferInfo, Directory.offset, Directory.length, &Directory.checkSum )) != NO_ERROR)
        return errCode;

    /* write new directory entry with new values */
    if ((errCode = WriteGeneric( pInputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOffset, &usBytesMoved )) != NO_ERROR)
        return errCode;
    return NO_ERROR;
}


/* ---------------------------------------------------------------------- */

PRIVATE uint32 GetGeneric( TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint8 * puchBuffer, uint16 usTagIndex)
{
uint32 ulOffset;
uint16 usBytesRead;

    if (usTagIndex >= TAG_INDEX_COUNT)
        return 0L;
    if ((ulOffset = TTTableOffset( pInputBufferInfo, Control_Table[usTagIndex].Tag ))== DIRECTORY_ERROR)
        return 0L;
    if (ReadGeneric(pInputBufferInfo, (uint8 *) puchBuffer, Control_Table[usTagIndex].StructSize, Control_Table[usTagIndex].Control, ulOffset, &usBytesRead) != NO_ERROR)
        return 0L;

    return ulOffset;
}

/* ---------------------------------------------------------------------- */
uint32 GetHHea( TTFACC_FILEBUFFERINFO * pInputBufferInfo, HHEA *  pHorizHead )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pHorizHead, HHEA_INDEX));
}

/* ---------------------------------------------------------------------- */
uint32 GetVHea( TTFACC_FILEBUFFERINFO * pInputBufferInfo, VHEA * pVertHead )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pVertHead, VHEA_INDEX));
}

/* ---------------------------------------------------------------------- */
uint32 GetHead( TTFACC_FILEBUFFERINFO * pInputBufferInfo, HEAD *  pHead )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pHead, HEAD_INDEX));
}

/* ---------------------------------------------------------------------- */
uint32 GetOS2( TTFACC_FILEBUFFERINFO * pInputBufferInfo, OS2 *pOs2 )      
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pOs2, OS2_INDEX));
}

/* ---------------------------------------------------------------------- */
uint32 GetNEWOS2( TTFACC_FILEBUFFERINFO * pInputBufferInfo, NEWOS2 *pNewOs2 )    
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pNewOs2, NEWOS2_INDEX));
}

/* ---------------------------------------------------------------------- */
uint32 GetVERSION2OS2( TTFACC_FILEBUFFERINFO * pInputBufferInfo, VERSION2OS2 *pVersion2Os2 )     
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pVersion2Os2, VERSION2OS2_INDEX));
}

/* ---------------------------------------------------------------------- */
uint32 GetSmartOS2(TTFACC_FILEBUFFERINFO * pInputBufferInfo, NEWOS2 *pOs2, BOOL *pbNewOS2)
{
uint32 ulOffset = 0L;
uint32 ulLength = 0L;

    ulLength = TTTableLength( pInputBufferInfo, OS2_TAG);
    if (ulLength > 0L)
    {
        if(ulLength == GetGenericSize(OS2_CONTROL)) /* we read all the bytes available */
        {
            *pbNewOS2 = FALSE;
            ulOffset = GetOS2(pInputBufferInfo,(OS2 *)pOs2);
        }
        else if (ulLength >= GetGenericSize(NEWOS2_CONTROL)) /* make sure there's enough to read */
        {
            *pbNewOS2 = TRUE;
            ulOffset = GetNEWOS2(pInputBufferInfo,pOs2);
        }
    }
    return ulOffset;    
}

/* ---------------------------------------------------------------------- */
uint32 GetSmarterOS2(TTFACC_FILEBUFFERINFO * pInputBufferInfo, MAINOS2 *pOs2)
{
uint32 ulOffset = 0L;
uint32 ulLength = 0L;

    ulLength = TTTableLength( pInputBufferInfo, OS2_TAG);
    if (ulLength > 0L)
    {
        if(ulLength == GetGenericSize(OS2_CONTROL)) /* we read all the bytes available */
            ulOffset = GetOS2(pInputBufferInfo,(OS2 *)pOs2);
        else if (ulLength == GetGenericSize(NEWOS2_CONTROL)) /* make sure there's enough to read */
            ulOffset = GetNEWOS2(pInputBufferInfo,(NEWOS2 *)pOs2);
        else if (ulLength >= GetGenericSize(VERSION2OS2_CONTROL))
            ulOffset = GetVERSION2OS2(pInputBufferInfo,pOs2);       
    }
    return ulOffset;
}

/* ---------------------------------------------------------------------- */
uint32 GetMaxp( TTFACC_FILEBUFFERINFO * pInputBufferInfo, MAXP *  pMaxp )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) pMaxp, MAXP_INDEX));
}

/* ---------------------------------------------------------------------- */

uint32 GetPost( TTFACC_FILEBUFFERINFO * pInputBufferInfo, POST *  Post )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) Post, POST_INDEX));
}
        
/* ---------------------------------------------------------------------- */
uint32 GetHdmx( TTFACC_FILEBUFFERINFO * pInputBufferInfo, HDMX *  Hdmx )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) Hdmx, HDMX_INDEX));
}
        
/* ---------------------------------------------------------------------- */
uint32 GetLTSH( TTFACC_FILEBUFFERINFO * pInputBufferInfo, LTSH *  Ltsh )
{
    return(GetGeneric(pInputBufferInfo, (uint8 *) Ltsh, LTSH_INDEX));
}
/* ---------------------------------------------------------------------- */
/* ---------------------------------------------------------------------- */
uint16 GetUnitsPerEm( TTFACC_FILEBUFFERINFO * pInputBufferInfo )
{
/* get true type scaling factor */

HEAD Head = {0};

   if (! GetHead(pInputBufferInfo,  &Head ) )
      return( 0 );
   return(Head.unitsPerEm);
}

/* ---------------------------------------------------------------------- */
uint16 GetNumGlyphs( TTFACC_FILEBUFFERINFO * pInputBufferInfo )
{
MAXP MaxP = {0};

    if (!GetMaxp(pInputBufferInfo, &MaxP))
        return (0);
    return(MaxP.numGlyphs);

} /* GetNumGlyphs() */

/* ---------------------------------------------------------------------- */
/* determine checksum, then calc checkSumAdjustment and write 
the whole thing out again.  This routine assumes that 
the checkSumAdjustment field was set to 0 and the 'head' 
table checksum was computed while that was so.  */
/* ---------------------------------------------------------------------- */
void SetFileChecksum( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint32 ulLength )
{
uint32        ulCheckSum;
HEAD         Head;
uint32        ulHeadOffset;
uint16 usBytesMoved;


    ulHeadOffset =  TTTableOffset( pOutputBufferInfo, HEAD_TAG );
    if ( ulHeadOffset == 0L )
        return;
    if (ReadGeneric(pOutputBufferInfo, (uint8 *) &Head, SIZEOF_HEAD, HEAD_CONTROL, ulHeadOffset, &usBytesMoved) != NO_ERROR)
        return;

    Head.checkSumAdjustment = 0L;
    if (WriteGeneric(pOutputBufferInfo, (uint8 *) &Head, SIZEOF_HEAD, HEAD_CONTROL, ulHeadOffset, &usBytesMoved) != NO_ERROR)
        return;
    if (CalcFileChecksum(pOutputBufferInfo, ulLength, &ulCheckSum) != NO_ERROR)
    {
        return;
    }
    
    Head.checkSumAdjustment = (0xb1b0afbaL - ulCheckSum );

    if (WriteGeneric(pOutputBufferInfo, (uint8 *) &Head, SIZEOF_HEAD, HEAD_CONTROL, ulHeadOffset, &usBytesMoved) != NO_ERROR)
    {
        return;
    };
}

/* ---------------------------------------------------------------------- */
int16 CopyBlock( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                uint32 ulTarget,
                uint32 ulSource,
                uint32 ulSize )
{
int16 errCode;

/* this routine copies a block of TrueType file data, taking into
account possible overlap between source and target */

/* ignore request for "null" copy */

    if ( ulTarget == ulSource || ulSize == 0L )
        return (NO_ERROR);

    /* now check ranges so that we can use memmove to do the copying.*/

    if ((errCode=CheckInOffset(pInputBufferInfo,ulSource,ulSize)) != NO_ERROR)
    {
        return errCode;
    }

    if ((errCode=CheckOutOffset(pInputBufferInfo,ulTarget,ulSize)) != NO_ERROR)
    {
        return errCode;
    }

   /* copy correctly regardless of whether the regions overlap */
#ifdef _MAC
    BlockMove((char *)(pInputBufferInfo->puchBuffer + ulSource),(char *)(pInputBufferInfo->puchBuffer + ulTarget), ulSize); 
#else
    memmove((char *)(pInputBufferInfo->puchBuffer + ulTarget),(char *)(pInputBufferInfo->puchBuffer + ulSource), ulSize); 
#endif // _MAC
    return(NO_ERROR);
}

/* ---------------------------------------------------------------------- */
int16 CopyBlockOver( TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
                     CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                     uint32 ulTarget,
                     uint32 ulSource,
                     uint32 ulSize )
{
int16 errCode;
/* this routine copies a block of data, taking into
account possible overlap between source and target */

/* ignore request for "null" copy */

    if ( (pOutputBufferInfo->puchBuffer + ulTarget == pInputBufferInfo->puchBuffer + ulSource) 
         || ulSize == 0L )
        return (NO_ERROR);

    /* now check ranges so that we can use memmove to do the copying.*/

    if ((errCode=CheckInOffset((TTFACC_FILEBUFFERINFO *)pInputBufferInfo,ulSource,ulSize)) != NO_ERROR)
    {
        return errCode;
    }

    if ((errCode=CheckOutOffset(pOutputBufferInfo,ulTarget,ulSize)) != NO_ERROR)
    {
        return errCode;
    }
    
   /* copy correctly regardless of whether the regions overlap */
#ifdef _MAC
    BlockMove((char *)(pInputBufferInfo->puchBuffer + ulSource), (char *)(pOutputBufferInfo->puchBuffer + ulTarget), ulSize); 
#else
    memmove((char *)(pOutputBufferInfo->puchBuffer + ulTarget),(char *)(pInputBufferInfo->puchBuffer + ulSource), ulSize); 
#endif // _MAC
    return(NO_ERROR);
}
/* ---------------------------------------------------------------------- */
/* copy a table from the input buffer to the output buffer to location *pulNewOutOffset */
/* table should not already exist in the output buffer, it will get written elsewhere */ 
/* ---------------------------------------------------------------------- */
int16 CopyTableOver(TTFACC_FILEBUFFERINFO *pOutputBufferInfo,
                    CONST_TTFACC_FILEBUFFERINFO *pInputBufferInfo,
                    __in_bcount(4) const char * Tag,
                    uint32 *pulNewOutOffset)
{
uint32 ulOffset;
uint32 ulLength;
uint32 ulDestOffset;
int16 errCode=NO_ERROR;
uint32 ulOutDirectoryOffset;
DIRECTORY Directory;
uint16 usBytesWritten;

    ulOutDirectoryOffset = GetTTDirectory( pOutputBufferInfo, Tag, &Directory); 
    /* make sure there is a directory entry */
    if (ulOutDirectoryOffset == DIRECTORY_ERROR) /* this should have been setup */
        return ERR_FORMAT;
    
    ulOffset = TTTableOffset((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, Tag );
    ulLength = TTTableLength((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, Tag);

    if (ulOffset == DIRECTORY_ERROR)
        return ERR_FORMAT; /* couldn't find it in input! */

    if ((errCode = ZeroLongWordAlign(pOutputBufferInfo, *pulNewOutOffset, &ulDestOffset)) != NO_ERROR)
    {
        return errCode;
    }

    Directory.offset = ulDestOffset;
    Directory.length = ulLength;

    if (ulLength > 0)
    {
        if ((errCode=CheckInOffset((TTFACC_FILEBUFFERINFO *)pInputBufferInfo,ulOffset,ulLength)) != NO_ERROR)
        {
            return errCode;
        }

        if ((errCode=CheckOutOffset(pOutputBufferInfo,ulDestOffset,ulLength)) != NO_ERROR)
        {
            return errCode;
        }
        
        errCode = ReadBytes((TTFACC_FILEBUFFERINFO *)pInputBufferInfo, pOutputBufferInfo->puchBuffer + ulDestOffset, ulOffset, ulLength);  /* read those bytes */
    }
    if (errCode == NO_ERROR)
    {
        if ((errCode = WriteGeneric( pOutputBufferInfo, (uint8 *) &Directory, SIZEOF_DIRECTORY, DIRECTORY_CONTROL, ulOutDirectoryOffset, &usBytesWritten )) == NO_ERROR)
            *pulNewOutOffset = ulDestOffset + ulLength;
    }

    return errCode;
}
/* ---------------------------------------------------------------------- */
uint32 RoundToLongWord( uint32  ulLength )
{
    ulLength = (ulLength + 3) & ~3;
    return( ulLength );
}


/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR)
uint16 ZeroLongWordGap( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                    uint32  ulOffset,
                    uint32  ulUnalignedLength,
                    __out_opt uint32 *pulNewOffset)
{
uint16 i;
uint16 errCode;
uint32 usPaddingBytes;

    /* zero out any pad bytes */

    if (pulNewOffset)
    {
        *pulNewOffset  = ulOffset + RoundToLongWord(ulUnalignedLength);
    }
    
    usPaddingBytes = RoundToLongWord(ulUnalignedLength) - ulUnalignedLength;
    
    for ( i = 0; i < usPaddingBytes; i++ )
    {
        if ((errCode = WriteByte( pInputBufferInfo, (uint8) 0, ulOffset + ulUnalignedLength + i))!= NO_ERROR)
        {
            return errCode;
        }
    }
    
    return NO_ERROR;
}

/* ---------------------------------------------------------------------- */
__checkReturn __success(return==NO_ERROR) 
uint16 ZeroLongWordAlign( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                    uint32  ulOffset, 
                    uint32 *pulNewOffset)
{
uint16 i;
uint16 errCode;
uint32 usPaddingBytes;

   /* zero out any pad bytes */

    *pulNewOffset  = RoundToLongWord(ulOffset);
    usPaddingBytes = *pulNewOffset - ulOffset;
    
    for ( i = 0; i < usPaddingBytes; i++ )
    {
        if ((errCode = WriteByte( pInputBufferInfo, (uint8) 0, ulOffset + i))!= NO_ERROR)
        {
            return errCode;
        }
    }

    return NO_ERROR;
}
/* ---------------------------------------------------------------------- */
