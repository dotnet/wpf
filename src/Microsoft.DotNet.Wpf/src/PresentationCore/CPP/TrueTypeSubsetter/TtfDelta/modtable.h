// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*
  * ModTable.h: Interface file for ModTable.c - Written by Louise Pathe
  *
  *
  * 
  */
  
#ifndef MODTABLE_DOT_H_DEFINED
#define MODTABLE_DOT_H_DEFINED        

int16 ModXmtxXhea( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint8 *puchKeepGlyphList, CONST uint16 usGlyphListCount, CONST uint16 usGlyphIndexCount, CONST uint16 usMaxGlyphIndexUsed, BOOL isHmtx, uint32 *pulBytesWritten);
int16 ModLTSH( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint8 *puchKeepGlyphList, CONST uint16 usGlyphListCount, CONST uint16 usGlyphIndexCount, uint32 *pulBytesWritten);
int16 ModHdmx( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint8 *puchKeepGlyphList, CONST uint16 usGlyphListCount, CONST uint16 usGlyphIndexCount, uint32 *pulBytesWritten);
int16 ModHead( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint16 usGlyphListCount, uint32 *pCheckSumAdjustment, uint32 *pulBytesWritten );
int16 ModKern( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint8 *puchKeepGlyphList, CONST uint16 usGlyphListCount, CONST uint16 usFormat  , uint32 *pulBytesWritten);
int16 ModMaxP( CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint32 *pulBytesWritten);
int16 ModName(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint16 usLanguage, CONST uint16 usFormat, uint32 *pulBytesWritten );
int16 ModOS2(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, uint16 usMinChr, uint16 usMaxChr, CONST uint16 usFormat, uint32 *pulBytesWritten );
int16 ModPost(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo , CONST uint16 usFormat, uint32 *pulBytesWritten );
int16 ModVDMX(CONST_TTFACC_FILEBUFFERINFO * pInputBufferInfo, TTFACC_FILEBUFFERINFO * pOutputBufferInfo, CONST uint16 usFormat, uint32 *pulBytesWritten );

#endif /* MODTABLE_DOT_H_DEFINED */
