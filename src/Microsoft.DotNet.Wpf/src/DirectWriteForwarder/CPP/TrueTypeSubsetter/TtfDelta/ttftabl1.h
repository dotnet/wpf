// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************
 * module: TTFTABL1.H
 *
 *
 * Function prototypes for TTFTABL1.C for TTFacc.lib
 *        Lower level functions extracted from ttftable.c
 *
 **************************************************************************/
 /* NOTE: must include TYPEDEFS.H, TTFF.H and TTFACC.H before this file */

#ifndef TTFTABL1_DOT_H_DEFINED
#define TTFTABL1_DOT_H_DEFINED        
/* preprocessor macros -------------------------------------------------- */
#define DIRECTORY_ENTRY_OFFSET_ERR 0xFFFFFFFF
#define DIRECTORY_ERROR 0L

/* structure definitions -------------------------------------------------- */

/* exported functions --------------------------------------------------- */

void ConvertLongTagToString(uint32 ulTag, __in_bcount(5) char *szTag);      /* convert a tag, as it has been read from the font, to a string */
void ConvertStringTagToLong(__in_bcount(4) const char *szTag, uint32 *pulTag);

uint32 TTDirectoryEntryOffset( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char * szTagName );
uint32 GetTTDirectory( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char * szTagName, DIRECTORY * pDirectory );
uint32 TTTableLength( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char * szTagName );

uint32 TTTableOffset( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char * szTagName );
uint32 TTTableChecksum( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char * szTagName, uint32 * pulChecksum );

int16 UpdateChecksum( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char *  szDirTag );

int16 UpdateDirEntry( TTFACC_FILEBUFFERINFO * pInputBufferInfo, __in_bcount(4) const char *  szDirTag, uint32   ulNewLength );

int16 UpdateDirEntryAll( 
    TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
    __in_bcount(4) const char *  szDirTag, 
    uint32   ulNewLength,
    uint32   ulNewOffset);

uint32 GetHHea( TTFACC_FILEBUFFERINFO * pInputBufferInfo, HHEA * HorizHead );
uint32 GetVHea( TTFACC_FILEBUFFERINFO * pInputBufferInfo, VHEA * VertHead );
uint32 GetHead( TTFACC_FILEBUFFERINFO * pInputBufferInfo, HEAD * Head );
uint32 GetOS2( TTFACC_FILEBUFFERINFO * pInputBufferInfo, OS2 *Os2 );
uint32 GetNEWOS2( TTFACC_FILEBUFFERINFO * pInputBufferInfo, NEWOS2 *NEWOs2 );    
uint32 GetVERSION2OS2( TTFACC_FILEBUFFERINFO * pInputBufferInfo, VERSION2OS2 *pVersion2Os2 );
uint32 GetSmartOS2(TTFACC_FILEBUFFERINFO * pInputBufferInfo, NEWOS2 *pOs2, BOOL *pbNewOS2);
uint32 GetSmarterOS2(TTFACC_FILEBUFFERINFO * pInputBufferInfo, MAINOS2 *pOs2);
uint32 GetMaxp( TTFACC_FILEBUFFERINFO * pInputBufferInfo, MAXP *  pMaxp );
uint32 GetPost( TTFACC_FILEBUFFERINFO * pInputBufferInfo, POST *  Post );
uint32 GetHdmx( TTFACC_FILEBUFFERINFO * pInputBufferInfo, HDMX *  Hdmx );
uint32 GetLTSH( TTFACC_FILEBUFFERINFO * pInputBufferInfo, LTSH *  Ltsh );
uint16 GetUnitsPerEm( TTFACC_FILEBUFFERINFO * pInputBufferInfo );
uint16 GetNumGlyphs( TTFACC_FILEBUFFERINFO * pInputBufferInfo );

void SetFileChecksum( TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint32 ulLength );
int16 CopyBlock( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                uint32 ulTarget,
                uint32 ulSource,
                uint32 ulSize )    ;

int16 CopyBlockOver( TTFACC_FILEBUFFERINFO * pOutputBufferInfo,
                     CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                     uint32 ulTarget,
                     uint32 ulSource,
                     uint32 ulSize );
int16 CopyTableOver(TTFACC_FILEBUFFERINFO *pOutputBufferInfo,
                    CONST_TTFACC_FILEBUFFERINFO *pInputBufferInfo,
                    __in_bcount(4) const char * Tag,
                    uint32 *pulNewOutOffset);
uint32 RoundToLongWord( uint32  ulLength ) ;

__checkReturn __success(return==NO_ERROR) uint16 ZeroLongWordGap( TTFACC_FILEBUFFERINFO * pInputBufferInfo,
                                                                    uint32  ulOffset,
                                                                    uint32  ulUnalignedLength,
                                                                    __out_opt uint32 *pulNewOffset);
                    
__checkReturn __success(return==NO_ERROR) uint16 ZeroLongWordAlign( TTFACC_FILEBUFFERINFO * pInputBufferInfo, 
                                                                    uint32  ulOffset, 
                                                                    uint32 *pulNewOffset);
#endif TTFTABL1_DOT_H_DEFINED
