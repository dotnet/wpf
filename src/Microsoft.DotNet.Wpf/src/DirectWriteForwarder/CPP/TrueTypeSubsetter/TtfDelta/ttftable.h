// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFTABLE.H
 *
 *
 * Function prototypes for TTFTABLE.C for TTFSub.lib
 *
 **************************************************************************/

#ifndef TTFTABLE_DOT_H_DEFINED
#define TTFTABLE_DOT_H_DEFINED        
/* preprocessor macros -------------------------------------------------- */
#define INVALID_GLYPH_INDEX 0xFFFF
#define INVALID_GLYPH_INDEX_LONG 0xFFFFFFFF
#define INVALID_CHAR_CODE 0xFFFF
#define DELETETABLETAG 0x01010101L
#define INVALID_NAME_STRING_LENGTH 0   /* must be 0 */

/* used by ReadAllocNameRecords etc. */
typedef struct namerecord *PNAMERECORD;
typedef struct namerecord NAMERECORD;
struct namerecord /* MUST be same as NAME_RECORD from ttff.h for the first 6 elements */
{   
    uint16  platformID;
    uint16  encodingID;
    uint16  languageID;
    uint16  nameID;
    uint16  stringLength; /* value of 0 means invalid string - don't write */
    uint16    stringOffset; /* offset into string pool */
    uint16    bStringWritten; /* set to FALSE if not written yet */
    char *  pNameString;  /* note: extra element. Alloced in ReadAllocNameRecords */
    char *  pNewNameString; /* If a different string should be written out, it is set here */
                            /* allocation of this string occurs outside of entry points. */
                            /* deallocation must occur before FreeNameRecords */
    BOOL bDeleteString; /* set if string is to be deleted */
};

/* exported functions --------------------------------------------------- */

void MarkTableForDeletion( 
    TTFACC_FILEBUFFERINFO * pInputBufferInfo,
    __in_bcount(4) const char *  szDirTag );     /* pointer to null terminated string with tag name */

uint32 FindCmapSubtable( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            uint16 usDesiredPlatform,
            uint16 usDesiredEncodingID,
            uint16 *pusFoundEncoding);
int16 ReadCmapLength(
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            CMAP_SUBHEADER_GEN * pCmapSubHeader,
            uint32    ulStartOffset,
            uint16 * pusBytesRead);
void FreeCmapFormat4Ids( GLYPH_ID * GlyphId );
void FreeCmapFormat4Segs( FORMAT4_SEGMENTS * Format4Segments);
void FreeCmapFormat4( 
            FORMAT4_SEGMENTS * Format4Segments,
            GLYPH_ID * GlyphId );
int16 ReadAllocCmapFormat4Ids( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint16 usSegCount,
            FORMAT4_SEGMENTS * Format4Segments,
            GLYPH_ID ** ppGlyphId,
            uint16 * pusnIds,
            uint32 ulOffset,
            uint32 *pulBytesRead );
int16 ReadAllocCmapFormat4Segs( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo, uint16 usSegCount,
            FORMAT4_SEGMENTS ** Format4Segments, 
            uint32 ulOffset,
            uint32 *pulBytesRead);
int16 ReadAllocCmapFormat4( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            CONST uint16 usPlatform,
            CONST uint16 usEncoding,
            uint16 *pusFoundEncoding,
            CMAP_FORMAT4 * CmapFormat4,
            FORMAT4_SEGMENTS ** Format4Segments,
            GLYPH_ID ** GlyphId,
            uint16 * pusnIds            
            );
void FreeCmapFormat6( uint16 *  glyphIndexArray);
int16 ReadAllocCmapFormat6( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
            CONST uint16 usPlatform,
            CONST uint16 usEncoding,
            uint16 *pusFoundEncoding,
            CMAP_FORMAT6 * pCmap,
            uint16 **  glyphIndexArray);
int16 ReadCmapFormat0( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            CONST uint16 usPlatform,
            CONST uint16 usEncoding,
            uint16 *pusFoundEncoding,
            CMAP_FORMAT0 * CmapFormat0);
int16 ReadAllocCmapFormat12(
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            uint32 ulSubOffset,
            CMAP_FORMAT12 * pCmapFormat12,
            FORMAT12_GROUPS **  ppFormat12Groups);
void FreeCmapFormat12Groups(FORMAT12_GROUPS * pFormat12Groups);
uint16 GetGlyphIdx( 
            uint16 CharCode,
            FORMAT4_SEGMENTS * Format4Segments,
            uint16 usnSegments,
            GLYPH_ID * GlyphId,
            uint16 usnGlyphs);
uint32 GetGlyphIdx12(
            uint32 ulCharCode,
            FORMAT12_GROUPS * pFormat12Groups,
            uint32 ulnGroups );
int16 GetGlyphHeader( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            uint16 GlyfIdx,
            uint16 usIdxToLocFmt,
            uint32 ulLocaOffset,
            uint32 ulGlyfOffset,    
            GLYF_HEADER * GlyfHeader,
            uint32 * pulOffset,
            uint16 * pusLength );
uint32 GetLoca( TTFACC_FILEBUFFERINFO               *pInputBufferInfo, 
                     __out_ecount(ulAllocedCount) uint32 *pulLoca, 
                     __range(1, USHORT_MAX + 1)   uint32  ulAllocedCount
                  );
int16 GetComponentGlyphList( 
            TTFACC_FILEBUFFERINFO * pInputBufferInfo,
            uint16 usCompositeGlyphIdx,
            uint16 * pusnGlyphs,
            uint16 * ausGlyphIdxs,
            uint16 cMaxGlyphs,
            uint16 *pusnComponentDepth,
            uint16 usLevelValue, 
            uint16 usIdxToLocFmt,
            uint32 ulLocaOffset,
            uint32 ulGlyfOffset);
int16 ReadAllocNameRecords(
            TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
            PNAMERECORD *ppNameRecordArray, 
            uint16 *pNameRecordCount, 
            CFP_ALLOCPROC lfpnAllocate, 
            CFP_FREEPROC lfpnFree);
uint32 CalcMaxNameTableLength(
            PNAMERECORD pNameRecordArray, 
            uint16 NameRecordCount);
int16 WriteNameRecords(
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
            PNAMERECORD pNameRecordArray, 
            uint16 NameRecordCount, 
            BOOL bDeleteStrings, 
            BOOL bOptimize, 
            uint32 *pulBytesWritten);
void FreeNameRecords(
            PNAMERECORD pNameRecordArray, 
            uint16 NameRecordCount, 
            CFP_FREEPROC lfpnFree);

int16 InsertTable(
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
            __in_bcount(4) const char * szTag, 
            uint8 * puchTableBuffer, 
            uint32 ulTableBufferLength);
int16 WriteNameTable(
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
            PNAMERECORD pNameRecordArray,     /* internal representation of NameRecord - from ttftable.h */
            uint16 NameRecordCount,
            BOOL bOptimize); /* lcp 4/8/97, if set to TRUE, optimize Name string storage for size */
int16 WriteSmartOS2Table(
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
            MAINOS2 * pOS2);
void SortByTag( 
            DIRECTORY * aDirectory, 
            uint16 usnDirs);
void SortByOffset( 
            DIRECTORY * aDirectory, 
            uint16 usnDirs);
int16 CompressTables( 
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo, 
            uint32 * pulBytesWritten);

typedef struct Char_Glyph_Map_List *PCHAR_GLYPH_MAP_LIST;
typedef struct Char_Glyph_Map_List {
    uint16 usCharCode;
    uint16 usGlyphIndex;
} CHARGLYPHMAPLIST;

typedef struct Char_Glyph_Map_List_Ex {
    uint32 ulCharCode;
    uint32 ulGlyphIndex;
} *PCHAR_GLYPH_MAP_LIST_EX;


void FreeFormat4CharCodes(PCHAR_GLYPH_MAP_LIST pusCharCodeList);
int16 ReadAllocFormat4CharGlyphMapList(
            TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
            CONST uint16 usPlatform,
            CONST uint16 usEncoding,
            __in_ecount(usGlyphCount) uint8 *puchKeepGlyphList, /* glyphs to keep - boolean */
            uint16 usGlyphCount,  /* count of puchKeepGlyphList */
            PCHAR_GLYPH_MAP_LIST *ppCharGlyphMapList,
            uint16 *pusnCharGlyphMapListCount);
void FreeFormat12CharCodes(PCHAR_GLYPH_MAP_LIST_EX pulCharCodeList);
int16 ReadAllocFormat12CharGlyphMapList(
            TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
            uint32 ulOffset,
            uint8 *puchKeepGlyphList, /* glyphs to keep - boolean */
            uint16 usGlyphCount,  /* count of puchKeepGlyphList */
            PCHAR_GLYPH_MAP_LIST_EX *ppCharGlyphMapList,
            uint32 *pulnCharGlyphMapListCount);

void ComputeFormat4CmapData( 
            CMAP_FORMAT4 * pCmapFormat4, /* to be set by this routine */
            FORMAT4_SEGMENTS * NewFormat4Segments, /* to be set by this routine */
            uint16 * pusnSegment, /* count of NewFormat4Segments - returned */
            GLYPH_ID * NewFormat4GlyphIdArray, /* to be set by this routine */
            uint16 * psnFormat4GlyphIdArray, /* count of NewFormat4GlyphIdArray - returned */
            PCHAR_GLYPH_MAP_LIST pCharGlyphMapList, /* input - map of CharCode to GlyphIndex */
            uint16 usnCharGlyphMapListCount);     /* input */

int16 WriteOutFormat4CmapData( 
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo,          
            CMAP_FORMAT4 *pCmapFormat4,    /* created by ComputeNewFormat4Data */
            FORMAT4_SEGMENTS * NewFormat4Segments, /* created by ComputeNewFormat4Data */
            GLYPH_ID * NewFormat4GlyphIdArray, /* created by ComputeNewFormat4Data */
            uint16 usnSegment, /* number of NewFormat4Segments elements */ 
            uint16 snFormat4GlyphIdArray, /* number of NewFormat4GlyphIdArray elements */
            uint32 ulNewOffset,  /* where to write the table */
            uint32 *pulBytesWritten);  /* number of bytes written to table */

void ComputeFormat12CmapData(
            CMAP_FORMAT12 * pCmapFormat12, /* to be set by this routine */
            FORMAT12_GROUPS * NewFormat12Groups, /* to be set by this routine */
            uint32 * pulnGroups,            /* count of NewFormat12Groups - returned */
            PCHAR_GLYPH_MAP_LIST_EX pCharGlyphMapList, /* input - map of CharCode to GlyphIndex */
            uint32 ulnCharGlyphMapListCount);     /* input */
int16 WriteOutFormat12CmapData( 
            TTFACC_FILEBUFFERINFO * pOutputBufferInfo,          
            CMAP_FORMAT12 *pCmapFormat12,        /* created by ComputeNewFormat12Data */
            FORMAT12_GROUPS * NewFormat12Groups, /* created by ComputeNewFormat12Data */
            uint32 ulnGroups,                    /* number of NewFormat12Groups elements */ 
            uint32 ulNewOffset,                    /* where to write the table */
            uint32 *pulBytesWritten);            /* number of bytes written to table */

#endif TTFTABLE_DOT_H_DEFINED
